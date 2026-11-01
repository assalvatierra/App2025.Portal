using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StackExchange.Redis;

namespace Portal.Services.Caching;

/// <summary>
/// Extension methods for configuring caching services.
/// Handles Redis and in-memory cache registration based on configuration.
/// </summary>
public static class CachingServiceCollectionExtension
{
    /// <summary>
    /// Adds caching services to the dependency injection container.
    /// Configures Redis if enabled, otherwise falls back to in-memory cache.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        /*
         * REDIS AND CACHING CONFIGURATION  
         */
        // Register CacheService with StackExchange.Redis
        // Read Redis configuration from appsettings
        var redisEnabled = configuration.GetValue<bool>("Caching:Redis:enabled");

        if (redisEnabled)
        {
            var redisHostRaw = configuration.GetValue<string>("Caching:Redis:https") 
                ?? throw new InvalidOperationException("Redis host not configured in Caching:Redis:https");
            var redisPort = configuration.GetValue<string>("Caching:Redis:port") ?? "6379";
            var redisToken = configuration.GetValue<string>("Caching:Redis:token") 
                ?? throw new InvalidOperationException("Redis token not configured in Caching:Redis:token");

            // Normalize host: remove scheme if the value includes https:// or http://
            var redisHost = redisHostRaw;
            if (redisHostRaw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    redisHost = new Uri(redisHostRaw).Host;
                }
                catch
                {
                    // If parsing fails, keep the raw value and let the connection attempt fail gracefully
                    redisHost = redisHostRaw;
                }
            }

            // Read optional connection options from config (fallback to safe defaults)
            var redisOptions = configuration.GetValue<string>("Caching:Redis:options")
                ?? "abortConnect=false,connectTimeout=5000,syncTimeout=5000";

            // Build Redis connection string with authentication and resilience options
            var redisConnectionString = $"{redisHost}:{redisPort},password={redisToken},ssl=true,{redisOptions}";

            Log.Information("Redis configuration loaded: Host={Host}, Port={Port}, Options={Options}", 
                redisHost, redisPort, redisOptions);

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                try
                {
                    Log.Information("Attempting Redis connection to {Host}:{Port}", redisHost, redisPort);
                    var mux = ConnectionMultiplexer.Connect(redisConnectionString);

                    if (mux.IsConnected)
                    {
                        Log.Information("Redis connected successfully to {Host}:{Port}", redisHost, redisPort);
                    }
                    else
                    {
                        Log.Warning("Redis multiplexer created but not yet connected to {Host}:{Port}. " +
                            "Connection will retry in background (abortConnect=false)", redisHost, redisPort);
                    }

                    return mux;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to create Redis connection multiplexer for {Host}:{Port}. " +
                        "Application will continue but caching will be unavailable until Redis connects.", 
                        redisHost, redisPort);
                    throw;
                }
            });
            services.AddScoped<ICache, RedisCache>();
        }
        else
        {
            // Fallback to in-memory cache if Redis is disabled
            services.AddSingleton<ICache, InMemoryCache>();
        }

        return services;
    }
}
