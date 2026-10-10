namespace Portal.Services.MessageBroker
{
    public interface IIdempotencyService
    {
        /// <summary>
        /// Attempts to reserve the message for processing by the specified consumer.
        /// Returns true if the reservation succeeded (caller should proceed with processing).
        /// Returns false if another consumer/instance already reserved or processed this message.
        /// </summary>
        Task<bool> TryReserveAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a previously reserved message as processed successfully.
        /// </summary>
        Task MarkProcessedAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a reservation if processing failed so the message can be retried later.
        /// </summary>
        Task ReleaseReservationAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a message was already processed for the given consumer.
        /// </summary>
        Task<bool> CheckIfProcessedAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);
    }
}
