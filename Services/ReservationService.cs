using Erp.Domain.Models;
using Portal.DBServices;
using System.Net.Mail;

namespace Portal.Services
{
    public class ReservationService:IReservationService
    {
        private readonly IPortalConfigurationService _configuration;
        private readonly IPortalReservationService _reservationDbService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReservationService> _logger;
        public ReservationService(
            IPortalConfigurationService configuration, 
            IPortalReservationService reservationDbService, 
            IEmailService emailService,
            ILogger<ReservationService> logger
            )
        {
            _configuration = configuration;
            _reservationDbService = reservationDbService;
            _emailService = emailService;
            _logger = logger;
        }
        public async Task ProcessPendingReservations()
        {
            _logger.LogInformation("ProcessPendingReservations called at {Time}", DateTime.Now);

            var pendingReservations = _reservationDbService.GetByStatusAsync("New").Result;

            if(pendingReservations != null && pendingReservations.Any())
            {
                _logger.LogInformation("Found {Count} pending reservations", pendingReservations.Count);
                await this.SendInternalReservationNotification(pendingReservations);
            }
            else
            {
                _logger.LogInformation("No pending reservations found");
            }

            //foreach (var reservation in pendingReservations)
            //{
            //    // Send email notification
            //    this.SendInternalReservationNotification(reservation);

            //    // Update reservation status to "Processed"

            //    //reservation.Status = "Processed";
            //    //_reservationDbService.UpdateReservation(reservation);
            //}
        }

        private async Task SendInternalReservationNotification(List<PortalReservation> reservations)
        {
            string[] EmailRecipient = Array.Empty<string>();
            string emailSubject = string.Empty;
            string emailTitle = string.Empty;
            string emailMessage = string.Empty;

            //get configuration setting value
            var config = _configuration.GetPortalConfigurationByNameAsync("Reservation").Result;
            if(config != null)
            {
                string jsonsetting = config.First().Settings;
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonsetting);

                string notificationEmail = settings.ContainsKey("InternalNotificationEmail") ? settings["InternalNotificationEmail"] : string.Empty;
                EmailRecipient = new[] { notificationEmail };

                emailSubject = settings.ContainsKey("InternalNotificationEmailSubject") ? settings["InternalNotificationEmailSubject"] : "New Reservation";
                emailTitle = settings.ContainsKey("InternalNotificationEmailTitle") ? settings["InternalNotificationEmailTitle"] : "Reservation";
                emailMessage = settings.ContainsKey("InternalNotificationEmailMessage") ? settings["InternalNotificationEmailMessage"] : "A new reservation has been made.";
            }

            // make list of reservation details
            string reservationDetails = string.Join("<br/>", 
                reservations.Select(r => 
                    $"Reservation ID: {r.Id}, Customer: {r.CustomerName}, Email: {r.ContactEmail}, Date Received: {r.DateReceived}"));

            if(EmailRecipient.Length > 0 && !string.IsNullOrEmpty(EmailRecipient[0]))
            {
              await _emailService.SendEmailAsync(
                EmailRecipient,
                Array.Empty<string>(),
                Array.Empty<string>(),
                emailSubject,
                $"{emailTitle}\n<br>" +
                $"{emailMessage}\n<br>" +
                $"{reservationDetails}\n<br>" 
                );
            }
            else {
                // Log or handle the case where no email recipient is configured
                //send email notification to administrator email
                string adminEmail = _configuration.GetPortalAdminEmail().Result;
                await _emailService.SendEmailAsync(
                    new[] { adminEmail },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    "Reservation Notification Error",
                    $"No internal notification email configured. Please check the portal configuration settings."
                );

            }

        }
    }
}
