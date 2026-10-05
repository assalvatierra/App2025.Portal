namespace Portal.Models
{
    public class RateLimitViewModel
    {
        public string Message { get; set; } = "You have exceeded the rate limit for this resource.";
        public string? ReturnUrl { get; set; }
        public int RequestsPerWindow { get; set; } = 10;
        public int WindowSeconds { get; set; } = 60;
        public DateTime ResetTime { get; set; }
    }
}
