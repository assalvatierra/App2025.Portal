using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.DBLayer;
using Portal.DBServices;
using MassTransit;
using Portal.Services;
using Portal.Services.MessageBroker;
using Portal.Services.Observability;
using Serilog;
using StackExchange.Redis;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Portal")
    .CreateLogger();

try
{
    Log.Information("Application starting up");
    var builder = WebApplication.CreateBuilder(args);

    // Add OpenTelemetry - must be called before building the app
    builder.AddPortalOpenTelemetry();

    // Add services to the container.
    builder.Host.UseSerilog();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();


// Register CacheService with StackExchange.Redis
// Read Redis configuration from appsettings
var redisEnabled = builder.Configuration.GetValue<bool>("Caching:Redis:enabled");

if (redisEnabled)
{
    var redisHostRaw = builder.Configuration.GetValue<string>("Caching:Redis:https") 
        ?? throw new InvalidOperationException("Redis host not configured in Caching:Redis:https");
    var redisPort = builder.Configuration.GetValue<string>("Caching:Redis:port") ?? "6379";
    var redisToken = builder.Configuration.GetValue<string>("Caching:Redis:token") 
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
    var redisOptions = builder.Configuration.GetValue<string>("Caching:Redis:options")
        ?? "abortConnect=false,connectTimeout=5000,syncTimeout=5000";

    // Build Redis connection string with authentication and resilience options
    var redisConnectionString = $"{redisHost}:{redisPort},password={redisToken},ssl=true,{redisOptions}";

    Log.Information("Redis configuration loaded: Host={Host}, Port={Port}, Options={Options}", 
        redisHost, redisPort, redisOptions);

    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
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
    builder.Services.AddScoped<ICache, RedisCache>();
}
else
{
    // Fallback to in-memory cache if Redis is disabled
    builder.Services.AddSingleton<ICache, InMemoryCache>();
}

// Register RateLimitSettings configuration
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));

// Register 3-layer architecture dependencies
builder.Services.AddScoped<IPortalConfigurationDbLayer, PortalConfigurationDbLayer>();
builder.Services.AddScoped<IPortalConfigurationService, PortalConfigurationService>();
builder.Services.AddScoped<IPortalItemDbLayer, PortalItemDbLayer>();
builder.Services.AddScoped<IPortalItemService, PortalItemService>();
builder.Services.AddScoped<IPortalItemSpecDbLayer, PortalItemSpecDbLayer>();
builder.Services.AddScoped<IPortalItemSpecService, PortalItemSpecService>();
builder.Services.AddScoped<IPortalReservationDbLayer, PortalReservationDbLayer>();
builder.Services.AddScoped<IPortalReservationService, PortalReservationService>();
builder.Services.AddScoped<IPortalCategoryDbLayer, PortalCategoryDbLayer>();
builder.Services.AddScoped<IPortalCategoryServices, PortalCategoryServices>();
builder.Services.AddScoped<IPortalContentDbLayer, PortalContentDbLayer>();
builder.Services.AddScoped<IPortalContentService, PortalContentService>();
builder.Services.AddScoped<IPortalContentDataDbLayer, PortalContentDataDbLayer>();
builder.Services.AddScoped<IPortalContentDataService, PortalContentDataService>();
builder.Services.AddScoped<IEmailService, EmailServiceV2>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<ICtaBoxService, CtaBoxService>();
builder.Services.AddScoped<ISemanticKernelService, SemanticKernelServiceOpenAI>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<Portal.Services.VectorSearch.IVectorSearchService, Portal.Services.VectorSearch.QdrantVectorSearchService>();

// MessageBroker: MassTransit with RabbitMQ, Azure Service Bus, and In-Memory transport options
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BrokerMessageConsumer>();

    // === RABBITMQ CONFIGURATION (ACTIVE) ===
    //var rabbitMqHost = builder.Configuration["MessageBroker:RabbitMQ:Host"]
    //    ?? throw new InvalidOperationException("RabbitMQ host not configured. Set MessageBroker:RabbitMQ:Host in appsettings.");
    //var rabbitMqPort = builder.Configuration.GetValue<int>("MessageBroker:RabbitMQ:Port", 5672);
    //var rabbitMqUsername = builder.Configuration["MessageBroker:RabbitMQ:Username"]
    //    ?? throw new InvalidOperationException("RabbitMQ username not configured. Set MessageBroker:RabbitMQ:Username in appsettings.");
    //var rabbitMqPassword = builder.Configuration["MessageBroker:RabbitMQ:Password"]
    //    ?? throw new InvalidOperationException("RabbitMQ password not configured. Set MessageBroker:RabbitMQ:Password in appsettings.");
    //var rabbitMqVirtualHost = builder.Configuration["MessageBroker:RabbitMQ:VirtualHost"]
    //    ?? throw new InvalidOperationException("RabbitMQ virtual host not configured. Set MessageBroker:RabbitMQ:VirtualHost in appsettings.");
    //var useSsl = builder.Configuration.GetValue<bool>("MessageBroker:RabbitMQ:UseSsl", true);

    //x.UsingRabbitMq((context, cfg) =>
    //{
    //    cfg.Host(new Uri($"amqps://{rabbitMqUsername}:{rabbitMqPassword}@{rabbitMqHost}:{rabbitMqPort}/{rabbitMqVirtualHost}"));

    //    cfg.ConfigureEndpoints(context);
    //});

    // === AZURE SERVICE BUS CONFIGURATION (COMMENTED OUT) ===
    //var serviceBusConnectionString = builder.Configuration.GetConnectionString("AzureServiceBus")
    //    ?? builder.Configuration["MessageBroker:AzureServiceBus:ConnectionString"]
    //    ?? throw new InvalidOperationException("Azure Service Bus connection string not configured. Set MessageBroker:AzureServiceBus:ConnectionString in appsettings.");

    //x.UsingAzureServiceBus((context, cfg) =>
    //{
    //    cfg.Host(serviceBusConnectionString);

    //    // Configure for Azure Service Bus Basic tier (queues only, no topics)
    //    // This disables topic-based messaging and uses queue-based endpoints instead
    //    cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter("Portal", false));
    //});

    // === IN-MEMORY CONFIGURATION (COMMENTED OUT) ===
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});
builder.Services.AddMessageConsumers();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IOutboxPublisher, OutboxPublisher>();
builder.Services.AddHostedService<OutboxPollingService>();

// Dead-letter notification service (sends email alerts to configured recipients)
// To use the email notification service, replace with: DeadLetterEmailNotificationService
// And configure MessageBroker:DeadLetterNotification:EmailRecipients in appsettings.json
builder.Services.AddScoped<Portal.Services.MessageBroker.IDeadLetterNotificationService, Portal.Services.MessageBroker.DeadLetterEmailNotificationService>();

// Idempotency for message consumption
builder.Services.Configure<Portal.Services.MessageBroker.IdempotencySettings>(builder.Configuration.GetSection("MessageBroker:Idempotency"));
builder.Services.AddScoped<Portal.Services.MessageBroker.IIdempotencyService, Portal.Services.MessageBroker.IdempotencyService>();
builder.Services.AddHostedService<Portal.Services.MessageBroker.IdempotencyCleanupService>();

// Outbox cleanup for processed messages
builder.Services.Configure<Portal.Services.MessageBroker.OutboxCleanupSettings>(builder.Configuration.GetSection("MessageBroker:OutboxCleanup"));
builder.Services.AddHostedService<Portal.Services.MessageBroker.OutboxCleanupService>();

// VectorDB Scheduler for monitoring and triggering search database refreshes
builder.Services.AddHostedService<Portal.Services.VectorDbSchedulerService>();

    builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable session middleware
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

    app.Run();
    Log.Information("Application terminated normally");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
