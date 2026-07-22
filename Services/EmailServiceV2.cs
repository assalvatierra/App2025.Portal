using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Portal.Services
{
    public class EmailServiceV2 : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailServiceV2(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string[] to, string[] cc, string[] bcc, string subject, string body)
        {
            // email settings
            // https://myaccount.google.com/apppasswords

            var smtpServer = _configuration["EmailSettings:Host"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);
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

            // Create MimeMessage
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(displayName ?? string.Empty, fromAddress));
            message.Subject = subject;
            message.ReplyTo.Add(new MailboxAddress(displayName ?? string.Empty, fromAddress));

            // Add recipients
            foreach (var recipient in to ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    message.To.Add(MailboxAddress.Parse(recipient));
                }
            }

            foreach (var recipient in cc ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    message.Cc.Add(MailboxAddress.Parse(recipient));
                }
            }

            foreach (var recipient in bcc ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    message.Bcc.Add(MailboxAddress.Parse(recipient));
                }
            }

            // Validate that at least one recipient exists
            if (message.To.Count == 0 && message.Cc.Count == 0 && message.Bcc.Count == 0)
            {
                throw new InvalidOperationException("At least one recipient (To, CC, or BCC) is required.");
            }

            // Set HTML body
            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = builder.ToMessageBody();

            // Set message priority
            message.Priority = MessagePriority.Normal;

            // Send email using MailKit SmtpClient
            using var client = new SmtpClient();
            try
            {
                // Set timeout
                client.Timeout = 30000; // 30 seconds timeout

                // Determine the appropriate SecureSocketOptions based on port and EnableSsl setting
                SecureSocketOptions secureSocketOptions;

                if (!enableSsl)
                {
                    // No SSL/TLS
                    secureSocketOptions = SecureSocketOptions.None;
                }
                else if (smtpPort == 465)
                {
                    // Port 465 typically uses implicit SSL (SslOnConnect)
                    secureSocketOptions = SecureSocketOptions.SslOnConnect;
                }
                else if (smtpPort == 587)
                {
                    // Port 587 typically uses explicit SSL (StartTls)
                    secureSocketOptions = SecureSocketOptions.StartTls;
                }
                else
                {
                    // For other ports, let MailKit auto-negotiate
                    secureSocketOptions = SecureSocketOptions.Auto;
                }

                // Connect to SMTP server
                await client.ConnectAsync(smtpServer, smtpPort, secureSocketOptions);

                // Authenticate
                await client.AuthenticateAsync(username, password);

                // Send the message
                await client.SendAsync(message);

                // Disconnect
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log detailed SMTP error for debugging
                throw new InvalidOperationException($"Failed to send email via SMTP. Message: {ex.Message}", ex);
            }
        }

        public Task<string> ReadEmailAsync(string emailId)
        {
            // Implement email reading logic here using MailKit's ImapClient or Pop3Client
            return Task.FromResult(string.Empty);
        }

        public Task<string> ReadAllEmailsAsync()
        {
            // Implement logic to read all emails here using MailKit's ImapClient or Pop3Client
            return Task.FromResult(string.Empty);
        }
    }
}
