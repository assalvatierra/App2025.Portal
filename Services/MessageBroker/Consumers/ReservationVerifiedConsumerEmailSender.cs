using Portal.Services.MessageBroker.Events;
using Erp.Domain.Models;

namespace Portal.Services.MessageBroker.Consumers
{
    /// <summary>
    /// Handles <see cref="ReservationVerified"/> events delivered through the outbox and MassTransit.
    /// </summary>
    public class ReservationVerifiedConsumerEmailSender : MessageConsumerBase<ReservationVerified>
    {
        private readonly ILogger<ReservationVerifiedConsumerEmailSender> _logger;
        private readonly IReservationService _reservationService;

        public ReservationVerifiedConsumerEmailSender(
            ILogger<ReservationVerifiedConsumerEmailSender> logger,
            IReservationService reservationService,
            IConfiguration configuration)
            : base(ParseAssignedKeys(configuration, nameof(ReservationVerifiedConsumerEmailSender)))
        {
            _logger = logger;
            _reservationService = reservationService;
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

        protected override async Task HandleAsync(ReservationVerified payload, BrokerMessage message, CancellationToken cancellationToken)
        {
            // Map ReservationVerified event to PortalReservation
            var reservation = new PortalReservation
            {
                Id = payload.ReservationId,
                TransactionType = payload.TransactionType ?? string.Empty,
                PortalItemId = payload.PortalItem,
                CustomerName = payload.CustomerName ?? string.Empty,
                ContactNo = payload.ContactNo,
                ContactEmail = payload.ContactEmail,
                DateReceived = payload.DateReceived ?? DateTime.Now,
                JsonData = payload.jsonData ?? string.Empty,
                Status = payload.Status ?? string.Empty
            };

            await this._reservationService.SendCustomerNotification(reservation);

            _logger.LogInformation(
                "Reservation {ReservationId} verified ({TransactionType}) for {CustomerName}. MessageId {MessageId}, CorrelationId {CorrelationId}",
                payload.ReservationId, payload.TransactionType, payload.CustomerName, message.Id, message.CorrelationId);
        }
    }
}
