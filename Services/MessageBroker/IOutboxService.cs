namespace Portal.Services.MessageBroker
{
    public interface IOutboxService
    {
        /// <summary>
        /// Adds a message to the outbox. Does not save; the caller's SaveChanges commits it
        /// together with the business changes (same transaction).
        /// </summary>
        Task AddAsync(BrokerMessage message, CancellationToken cancellationToken = default);

        Task<List<OutboxMessage>> GetPendingAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default);

        Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increment retry count and persist the error. Returns the updated RetryCount.
        /// </summary>
        Task<int> MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark the outbox message as dead-lettered (final state).
        /// </summary>
        Task MarkAsDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default);
    }
}
