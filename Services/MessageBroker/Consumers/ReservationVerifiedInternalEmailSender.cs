using Portal.Services.MessageBroker.Events;
using Erp.Domain.Models;

namespace Portal.Services.MessageBroker.Consumers
{
    /// <summary>
    /// Handles <see cref="ReservationVerified"/> events delivered through the outbox and MassTransit.
    /// Sends internal email notification to configured internal recipients.
    /// </summary>
    public class ReservationVerifiedInternalEmailSender : MessageConsumerBase<ReservationVerified>
    {
        private readonly ILogger<ReservationVerifiedInternalEmailSender> _logger;
        private readonly IReservationService _reservationService;

        public ReservationVerifiedInternalEmailSender(
            ILogger<ReservationVerifiedInternalEmailSender> logger,
            IReservationService reservationService)
        {
            _logger = logger;
            _reservationService = reservationService;
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

            // Send internal notification with single reservation wrapped in a list
            var reservations = new List<PortalReservation> { reservation };
            await this._reservationService.SendInternalReservationNotification(reservations);

            _logger.LogInformation(
                "Internal email notification sent for Reservation {ReservationId} ({TransactionType}) for {CustomerName}. MessageId {MessageId}, CorrelationId {CorrelationId}",
                payload.ReservationId, payload.TransactionType, payload.CustomerName, message.Id, message.CorrelationId);
        }
    }
}
