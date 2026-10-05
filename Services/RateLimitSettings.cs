namespace Portal.Services
{
    /// <summary>
    /// Configuration settings for rate limiting.
    /// Can be bound from appsettings.json under "RateLimit" section.
    /// </summary>
    public class RateLimitSettings
    {
        /// <summary>
        /// Whether rate limiting is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Default maximum number of requests per time window.
        /// </summary>
        public int DefaultRequestsPerWindow { get; set; } = 10;

        /// <summary>
        /// Default time window in seconds.
        /// </summary>
        public int DefaultWindowSeconds { get; set; } = 60;

        /// <summary>
        /// Endpoint-specific rate limit overrides.
        /// Key format: "ControllerName:ActionName", e.g., "Articles:Articles"
        /// </summary>
        public Dictionary<string, EndpointRateLimit> EndpointOverrides { get; set; } = new();
    }

    /// <summary>
    /// Rate limit configuration for a specific endpoint.
    /// </summary>
    public class EndpointRateLimit
    {
        /// <summary>
        /// Maximum number of requests allowed per time window for this endpoint.
        /// </summary>
        public int RequestsPerWindow { get; set; }

        /// <summary>
        /// Time window in seconds for this endpoint.
        /// </summary>
        public int WindowSeconds { get; set; }
    }
}
