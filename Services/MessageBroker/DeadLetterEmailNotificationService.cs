using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Dead-letter notification service that sends email alerts to configured recipients
    /// and logs the event. Requires EmailSettings and MessageBroker:DeadLetterNotification
    /// configuration sections.
    /// </summary>
    public class DeadLetterEmailNotificationService : IDeadLetterNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeadLetterEmailNotificationService> _logger;

        public DeadLetterEmailNotificationService(
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<DeadLetterEmailNotificationService> logger)
        {
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifyAsync(OutboxMessage message, string? exception, CancellationToken cancellationToken = default)
        {
            try
            {
                // Log the critical event first
                _logger.LogCritical(
                    "Outbox message {MessageId} moved to dead-letter. MessageType={MessageType}, CorrelationId={CorrelationId}, RetryCount={RetryCount}, Exception={Exception}",
                    message.Id,
                    message.MessageType,
                    message.CorrelationId,
                    message.RetryCount,
                    exception);

                // Get email recipients from configuration
                var recipientsConfig = _configuration["MessageBroker:DeadLetterNotification:EmailRecipients"];
                if (string.IsNullOrWhiteSpace(recipientsConfig))
                {
                    _logger.LogWarning(
                        "Dead-letter email notification skipped for message {MessageId}: no recipients configured in MessageBroker:DeadLetterNotification:EmailRecipients",
                        message.Id);
                    return;
                }

                var recipients = recipientsConfig.Split(new[] { ';', ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToArray();

                if (recipients.Length == 0)
                {
                    _logger.LogWarning(
                        "Dead-letter email notification skipped for message {MessageId}: no valid email addresses in configuration",
                        message.Id);
                    return;
                }

                // Build email body
                var body = BuildEmailBody(message, exception);

                // Send email
                await _emailService.SendEmailAsync(
                    to: recipients,
                    cc: Array.Empty<string>(),
                    bcc: Array.Empty<string>(),
                    subject: $"🚨 Dead Letter Alert: Message {message.Id}",
                    body: body);

                _logger.LogInformation(
                    "Dead-letter email notification sent for message {MessageId} to {RecipientCount} recipients",
                    message.Id,
                    recipients.Length);
            }
            catch (Exception ex)
            {
                // Log email send failure but don't throw; the message is already dead-lettered
                _logger.LogError(
                    ex,
                    "Failed to send dead-letter email notification for message {MessageId}",
                    message.Id);
            }
        }

        private string BuildEmailBody(OutboxMessage message, string? exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; background-color: #f5f5f5; }");
            sb.AppendLine(".container { max-width: 600px; margin: 20px auto; background-color: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine(".header { background-color: #d32f2f; color: #fff; padding: 15px; border-radius: 4px; margin-bottom: 20px; }");
            sb.AppendLine(".header h1 { margin: 0; font-size: 20px; }");
            sb.AppendLine(".section { margin-bottom: 20px; }");
            sb.AppendLine(".label { font-weight: bold; color: #333; }");
            sb.AppendLine(".value { color: #666; word-break: break-all; }");
            sb.AppendLine(".exception { background-color: #ffe6e6; border-left: 4px solid #d32f2f; padding: 10px; border-radius: 4px; font-family: monospace; font-size: 12px; }");
            sb.AppendLine(".footer { font-size: 12px; color: #999; border-top: 1px solid #ddd; padding-top: 10px; margin-top: 20px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"container\">");

            // Header
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<h1>🚨 Outbox Message Dead Letter Alert</h1>");
            sb.AppendLine("</div>");

            // Message Details
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine($"<div><span class=\"label\">Message ID:</span> <span class=\"value\">{message.Id}</span></div>");
            sb.AppendLine($"<div><span class=\"label\">Message Type:</span> <span class=\"value\">{message.MessageType}</span></div>");
            sb.AppendLine($"<div><span class=\"label\">Correlation ID:</span> <span class=\"value\">{message.CorrelationId?.ToString() ?? "N/A"}</span></div>");
            sb.AppendLine($"<div><span class=\"label\">Created At:</span> <span class=\"value\">{message.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</span></div>");
            sb.AppendLine($"<div><span class=\"label\">Retry Count:</span> <span class=\"value\">{message.RetryCount}</span></div>");
            sb.AppendLine($"<div><span class=\"label\">Status:</span> <span class=\"value\">DeadLetter</span></div>");
            sb.AppendLine("</div>");

            // Exception Details
            if (!string.IsNullOrWhiteSpace(exception))
            {
                sb.AppendLine("<div class=\"section\">");
                sb.AppendLine("<div><span class=\"label\">Last Error:</span></div>");
                sb.AppendLine($"<div class=\"exception\">{System.Net.WebUtility.HtmlEncode(exception)}</div>");
                sb.AppendLine("</div>");
            }

            // Message Content Preview
            if (!string.IsNullOrWhiteSpace(message.Payload))
            {
                sb.AppendLine("<div class=\"section\">");
                sb.AppendLine("<div><span class=\"label\">Payload (first 500 chars):</span></div>");
                var payloadPreview = message.Payload.Length > 500
                    ? message.Payload[..500] + "..."
                    : message.Payload;
                sb.AppendLine($"<div class=\"exception\">{System.Net.WebUtility.HtmlEncode(payloadPreview)}</div>");
                sb.AppendLine("</div>");
            }

            // Action items
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine("<div><span class=\"label\">Action Required:</span></div>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Review the outbox table for message details and error history</li>");
            sb.AppendLine("<li>Investigate the root cause of the failure</li>");
            sb.AppendLine("<li>Determine if the message should be manually republished or the system failure should be fixed</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class=\"footer\">");
            sb.AppendLine("This is an automated notification from the Portal application's message broker.");
            sb.AppendLine("<br/>Do not reply to this email.");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
