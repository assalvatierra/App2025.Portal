namespace Portal.Services.MessageBroker
{
    public class OutboxCleanupSettings
    {
        public bool EnableCleanup { get; set; } = true;

        public int RetentionDays { get; set; } = 30;
    }
}
