namespace Portal.Services.MessageBroker
{
    public class IdempotencySettings
    {
        public bool EnableIdempotency { get; set; } = true;
        public int RetentionDays { get; set; } = 90;
    }
}
