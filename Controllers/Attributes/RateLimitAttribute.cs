using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Services;

namespace Portal.Controllers.Attributes
{
    /// <summary>
    /// Rate limiting attribute that restricts requests per IP address per action method.
    /// Uses the ICache service to track request counts across time windows.
    /// Supports configuration-based limits via appsettings.json RateLimit section.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RateLimitAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int? _requestsPerWindow;
        private readonly int? _windowSeconds;
        private readonly string? _endpointName;

        /// <summary>
        /// Initializes a new instance of the RateLimitAttribute.
        /// </summary>
        /// <param name="requestsPerWindow">Maximum number of requests allowed per time window (overrides config if specified)</param>
        /// <param name="windowSeconds">Time window in seconds (overrides config if specified)</param>
        /// <param name="endpointName">Optional endpoint name for config lookup (e.g., "Articles:Articles")</param>
        public RateLimitAttribute(int requestsPerWindow = 0, int windowSeconds = 0, string? endpointName = null)
        {
            _requestsPerWindow = requestsPerWindow > 0 ? requestsPerWindow : null;
            _windowSeconds = windowSeconds > 0 ? windowSeconds : null;
            _endpointName = endpointName;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                // Get ICache from dependency injection
                var cache = context.HttpContext.RequestServices.GetService(typeof(ICache)) as ICache;
                if (cache == null)
                {
                    // If cache is not available, skip rate limiting and proceed
                    await next();
                    return;
                }

                // Get RateLimitSettings from dependency injection
                var rateLimitSettings = context.HttpContext.RequestServices.GetService(typeof(IOptions<RateLimitSettings>)) as IOptions<RateLimitSettings>;
                if (rateLimitSettings?.Value == null || !rateLimitSettings.Value.Enabled)
                {
                    // If rate limiting is not enabled, proceed
                    await next();
                    return;
                }

                // Extract client IP address
                var clientIp = GetClientIpAddress(context.HttpContext);
                if (string.IsNullOrEmpty(clientIp))
                {
                    // If IP cannot be determined, skip rate limiting
                    await next();
                    return;
                }

                // Build cache key: rate-limit:{ip}:{controller}:{action}
                var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "unknown";
                var actionName = context.RouteData.Values["action"]?.ToString() ?? "unknown";
                var cacheKey = $"rate-limit:{clientIp}:{controllerName}:{actionName}";

                var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RateLimitAttribute>)) as ILogger<RateLimitAttribute>;

                logger?.LogInformation("========== RATE LIMIT CHECK START ==========");
                logger?.LogInformation("CacheKey: {CacheKey}", cacheKey);
                logger?.LogInformation("ClientIP: {ClientIp}", clientIp);
                logger?.LogInformation("Endpoint: {Endpoint}", $"{controllerName}:{actionName}");

                // Determine rate limit parameters from config or attribute
                var endpointKey = _endpointName ?? $"{controllerName}:{actionName}";
                var (requestsPerWindow, windowSeconds) = GetLimitParameters(rateLimitSettings.Value, endpointKey);

                logger?.LogInformation("Configured Limit: {RequestsPerWindow} requests per {WindowSeconds}s", requestsPerWindow, windowSeconds);

                // Get current request count from cache
                var currentCount = await cache.GetAsync<int?>(cacheKey);
                var requestCount = currentCount ?? 0;

                logger?.LogInformation("Current cache value: {CurrentCount}", currentCount);
                logger?.LogInformation("About to increment from {CurrentCount} to {NextCount}", requestCount, requestCount + 1);

                // Increment first, THEN check the limit
                var newCount = requestCount + 1;
                await cache.SetAsync(
                    cacheKey,
                    newCount,
                    TimeSpan.FromSeconds(windowSeconds)
                );

                logger?.LogInformation("Cache updated. New value: {NewCount}", newCount);

                // Now check if we've exceeded the limit
                logger?.LogInformation("Checking: {NewCount} > {RequestsPerWindow}?", newCount, requestsPerWindow);

                if (newCount > requestsPerWindow)
                {
                    // Log the rate limit violation
                    logger?.LogWarning(
                        "❌ RATE LIMIT EXCEEDED - IP: {ClientIp}, Endpoint: {Endpoint}, Requests: {RequestCount}, Limit: {RequestsPerWindow}",
                        clientIp, endpointKey, newCount, requestsPerWindow);

                    logger?.LogInformation("========== RATE LIMIT CHECK END - REDIRECTING ==========");

                    // Redirect to rate limit reached page using RedirectToActionResult
                    var currentUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                    context.Result = new RedirectToActionResult(
                        "RateLimitReached",
                        "Error",null
                        //new
                        //{
                        //    returnUrl = currentUrl.ToString(),
                        //    requestsPerWindow = requestsPerWindow,
                        //    windowSeconds = windowSeconds
                        //}
                        );
                    return;
                }

                // Log successful request
                logger?.LogInformation(
                    "✅ RATE LIMIT OK - IP: {ClientIp}, Endpoint: {Endpoint}, Requests: {NewCount}/{Limit}",
                    clientIp, endpointKey, newCount, requestsPerWindow);

                logger?.LogInformation("========== RATE LIMIT CHECK END - PASSING ==========");

                // Continue to the action
                await next();
            }
            catch (Exception ex)
            {
                // Log the exception and continue - don't break the request due to rate limiter error
                var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RateLimitAttribute>)) as ILogger<RateLimitAttribute>;
                logger?.LogError(ex, "❌ CRITICAL ERROR in RateLimitAttribute - request may bypass rate limiting");
                await next();
            }
        }

        /// <summary>
        /// Resolves rate limit parameters from configuration or attribute defaults.
        /// Priority: Attribute parameters > Endpoint overrides in config > Default config values
        /// </summary>
        private (int requestsPerWindow, int windowSeconds) GetLimitParameters(RateLimitSettings settings, string endpointKey)
        {
            int requestsPerWindow = settings.DefaultRequestsPerWindow;
            int windowSeconds = settings.DefaultWindowSeconds;

            // Check for endpoint-specific overrides in configuration
            if (settings.EndpointOverrides.TryGetValue(endpointKey, out var endpointLimit))
            {
                requestsPerWindow = endpointLimit.RequestsPerWindow;
                windowSeconds = endpointLimit.WindowSeconds;
            }

            // Attribute parameters override config if they were explicitly set
            if (_requestsPerWindow.HasValue)
            {
                requestsPerWindow = _requestsPerWindow.Value;
            }
            if (_windowSeconds.HasValue)
            {
                windowSeconds = _windowSeconds.Value;
            }

            return (requestsPerWindow, windowSeconds);
        }

        /// <summary>
        /// Extracts the client's IP address from the HTTP context.
        /// Checks X-Forwarded-For header first (for proxy scenarios), then RemoteIpAddress.
        /// </summary>
        private string GetClientIpAddress(HttpContext httpContext)
        {
            try
            {
                // Check for X-Forwarded-For header (common in proxy/load balancer scenarios)
                if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
                {
                    var ips = xff.ToString().Split(',');
                    if (ips.Length > 0 && !string.IsNullOrWhiteSpace(ips[0]))
                    {
                        return ips[0].Trim();
                    }
                }

                // Fall back to RemoteIpAddress
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                return remoteIp ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
