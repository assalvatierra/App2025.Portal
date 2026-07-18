using System.Net;
using System.Net.Mail;

namespace Portal.Services
{
    public class EmailService: IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task SendEmailAsync(string[] to, string[] cc, string[] bcc, string subject, string body)
        {
            // email settings
            // https://myaccount.google.com/apppasswords

            var fromAddress = _configuration["EmailSettings:FromAddress"];
            var displayName = _configuration["EmailSettings:FromName"];
            var username = _configuration["EmailSettings:UserName"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new InvalidOperationException("EmailSettings:FromAddress is required.");
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("EmailSettings:UserName is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("EmailSettings:Password is required.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, displayName ?? string.Empty),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var recipient in to ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    message.To.Add(recipient);
                }
            }

            foreach (var recipient in cc ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    message.CC.Add(recipient);
                }
            }

            foreach (var recipient in bcc ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    message.Bcc.Add(recipient);
                }
            }

            // Helps receiving servers classify the message correctly
            message.Headers.Add("X-Mailer", "AngularApp1 Mailer");
            message.Headers.Add("Precedence", "bulk");
            message.Priority = MailPriority.Normal;
            message.ReplyToList.Add(new MailAddress(fromAddress, displayName ?? string.Empty));

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }

        public Task<string> ReadEmailAsync(string emailId)
        {
            // Implement email reading logic here
            return Task.FromResult(string.Empty);
        }

        public Task<string> ReadAllEmailsAsync()
        {
            // Implement logic to read all emails here
            return Task.FromResult(string.Empty);
        }
    }
}
