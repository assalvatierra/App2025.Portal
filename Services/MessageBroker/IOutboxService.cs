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

        Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
    }
}
