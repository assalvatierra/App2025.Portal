namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Lifecycle status for outbox messages.
    /// </summary>
    public enum OutboxMessageStatus
    {
        Pending = 0,
        Failed = 1,
        Processed = 2,
        DeadLetter = 3
    }
}
