using Erp.Domain.Models;
using Portal.DBServices;
using System.Net.Mail;

namespace Portal.Services
{
    public class ReservationService:IReservationService
    {
        private readonly IPortalReservationService _reservationDbService;
        private readonly IEmailService _emailService;
        public ReservationService(IPortalReservationService reservationDbService, IEmailService emailService)
        {
            _reservationDbService = reservationDbService;
            _emailService = emailService;
        }
        public void ProcessPendingReservations()
        {
            var pendingReservations = _reservationDbService.GetByStatusAsync("New").Result;
            foreach (var reservation in pendingReservations)
            {
                // Send email notification
                this.SendInternalReservationNotification(reservation);

                // Update reservation status to "Processed"

                //reservation.Status = "Processed";
                //_reservationDbService.UpdateReservation(reservation);
            }
        }

        private async void SendInternalReservationNotification(PortalReservation reservation)
        {
            string[] EmailRecipient = Array.Empty<string>();
            string emailMessage = string.Empty;

            // TODO: Get the email and message from configuration
            EmailRecipient = new[] { "assalvatierra@yahoo.com" };
            emailMessage = "New Reservation";


            //if (!string.IsNullOrEmpty(paymentExternal.JsonInfo))
            //{
            //    var jsonInfo = JsonSerializer.Deserialize<PaymentExternalJsonInfo>(
            //        paymentExternal.JsonInfo,
            //        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            //    );
            //    if (jsonInfo != null)
            //    {
            //        customerEmail = new[] { jsonInfo.ReceiptEmail ?? string.Empty };
            //        emailMessage = jsonInfo.EmailMessage ?? string.Empty;
            //    }
            //}

            await _emailService.SendEmailAsync(
                EmailRecipient,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Realbreeze Travel & Tours ",
                $"New Reservation Request\n<br>" +
                $"Customer: {reservation.CustomerName}\n<br>" +
                $"{emailMessage}\n<br>" 
                );

        }
    }
}
