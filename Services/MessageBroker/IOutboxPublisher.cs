namespace Portal.Services.MessageBroker
{
    public interface IOutboxPublisher
    {
        /// <summary>
        /// Stores the message in the outbox. Call SaveChangesAsync on the DbContext (or use
        /// <paramref name="saveChanges"/>) to commit it with the business transaction.
        /// </summary>
        Task PublishAsync(BrokerMessage message, bool saveChanges = false, CancellationToken cancellationToken = default);

        Task PublishAsync<T>(T payload, string? messageType = null, Guid? correlationId = null, string? partitionKey = null,
            bool saveChanges = false, CancellationToken cancellationToken = default) where T : class;
    }
}
