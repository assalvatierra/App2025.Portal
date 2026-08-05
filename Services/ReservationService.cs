using Erp.Domain.Models;
using Microsoft.IdentityModel.Tokens;
using Portal.DBServices;
using Portal.Models;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Text.Json;

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

                foreach (var reservation in pendingReservations)
                {
                    await this.SendCustomerNotification(reservation);
                }
            }
            else
            {
                _logger.LogInformation("No pending reservations found");
            }
        }

        public async Task SendCustomerNotification(PortalReservation reservation)
        {
            if (reservation == null)
            {
                _logger.LogWarning("SendCustomerNotification called with null reservation");
                return;
            }

            if (string.IsNullOrWhiteSpace(reservation.ContactEmail))
            {
                _logger.LogWarning("Reservation {ReservationId} has no contact email; skipping customer notification", reservation.Id);
                return;
            }

            try
            {
                string subject = "Your reservation is in progress";
                string body = $"Dear {reservation.CustomerName},<br/><br/>" +
                              $"We have received your reservation (ID: {reservation.Id}) on {reservation.DateReceived:G}. " +
                              "It is now in progress and our team will contact you if any additional information is required.<br/><br/>" +
                              "Thank you for choosing us.<br/><br/>" +
                              "Regards,<br/>Portal Team";

                await _emailService.SendEmailAsync(
                    new[] { reservation.ContactEmail },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    subject,
                    body
                );

                //reservation.Status = "In Progress";
                //await _reservationDbService.UpdateAsync(reservation);
                _logger.LogInformation("Sent customer notification for reservation {ReservationId} and updated status to In Progress", reservation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send customer notification for reservation {ReservationId}", reservation.Id);
            }
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
                var settings = JsonSerializer.Deserialize<InternalEmailNotificationJsonModel>(jsonsetting);

                string notificationEmail = !settings.InternalNotificationEmails.IsNullOrEmpty() ? 
                                    Regex.Replace(settings.InternalNotificationEmails, @"[\r\n\t]", string.Empty)
                                    .Replace("[", string.Empty).Replace("]", string.Empty).Replace("\"", "") : string.Empty;

                EmailRecipient =  notificationEmail.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(e => e.Trim()).ToArray();

                emailSubject = !settings.InternalNotificationEmailSubject.IsNullOrEmpty() ? settings.InternalNotificationEmailSubject : "New Reservation";
                emailTitle = !settings.InternalNotificationEmailTitle.IsNullOrEmpty() ? settings.InternalNotificationEmailTitle : "Reservation";
                emailMessage = !settings.InternalNotificationEmailMessage.IsNullOrEmpty() ? settings.InternalNotificationEmailMessage : "A new reservation has been made.";
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

        public async Task<string> GenerateOTP()
        {
            var random = new Random();
            return await Task.FromResult(random.Next(100000, 999999).ToString()); // Generates a 6-digit OTP
        }

        public async Task SendCustomerOTP(PortalReservation reservation, string otp)
        {
            try
            {
                string subject = "Your reservation One Time Password";
                string body = $"Dear {reservation.CustomerName},<br/><br/>" +
                              $"We have received your reservation (ID: {reservation.Id}) on {reservation.DateReceived:G}. " +
                              $"Your One Time Password is: {otp} <br/><br/>" +
                              "Thank you for choosing us.<br/><br/>" +
                              "Regards,<br/>Portal Team";

                await _emailService.SendEmailAsync(
                    new[] { reservation.ContactEmail },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    subject,
                    body
                );


                _logger.LogInformation("Sent customer notification for OTP for reservation {ReservationId} ", reservation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send customer notification for OTP for reservation {ReservationId}", reservation.Id);
            }
        }

    }
}
