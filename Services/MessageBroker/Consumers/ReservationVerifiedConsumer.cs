using Portal.Services.MessageBroker.Events;

namespace Portal.Services.MessageBroker.Consumers
{
    /// <summary>
    /// Handles <see cref="ReservationVerified"/> events delivered through the outbox and MassTransit.
    /// </summary>
    public class ReservationVerifiedConsumer : MessageConsumerBase<ReservationVerified>
    {
        private readonly ILogger<ReservationVerifiedConsumer> _logger;

        public ReservationVerifiedConsumer(ILogger<ReservationVerifiedConsumer> logger)
        {
            _logger = logger;
        }

        protected override Task HandleAsync(ReservationVerified payload, BrokerMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Reservation {ReservationId} verified ({TransactionType}) for {CustomerName}. MessageId {MessageId}, CorrelationId {CorrelationId}",
                payload.ReservationId, payload.TransactionType, payload.CustomerName, message.Id, message.CorrelationId);

            return Task.CompletedTask;
        }
    }
}
