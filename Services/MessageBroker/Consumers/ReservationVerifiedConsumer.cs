using Portal.Services.MessageBroker.Events;

namespace Portal.Services.MessageBroker.Consumers
{
    /// <summary>
    /// Handles <see cref="ReservationVerified"/> events delivered through the outbox and MassTransit.
    /// </summary>
    public class ReservationVerifiedConsumer : MessageConsumerBase<ReservationVerified>
    {
        private readonly ILogger<ReservationVerifiedConsumer> _logger;

        public ReservationVerifiedConsumer(ILogger<ReservationVerifiedConsumer> logger, IConfiguration configuration)
            : base(ParseAssignedKeys(configuration, nameof(ReservationVerifiedConsumer)))
        {
            _logger = logger;
        }

        private static IEnumerable<string>? ParseAssignedKeys(IConfiguration configuration, string consumerName)
        {
            var section = configuration.GetSection("PartitionKeyAssignments");
            var value = section[consumerName];

            // Debug: Log configuration reading
            System.Diagnostics.Debug.WriteLine($"[PartitionKeyFilter] {consumerName}: section.Path='{section.Path}', value='{value}'");

            if (string.IsNullOrWhiteSpace(value)) 
            {
                System.Diagnostics.Debug.WriteLine($"[PartitionKeyFilter] {consumerName}: No partition key assigned (config value is null/empty)");
                return null;
            }

            var keys = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            System.Diagnostics.Debug.WriteLine($"[PartitionKeyFilter] {consumerName}: Assigned keys = {string.Join(",", keys)}");
            return keys;
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
