using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Default dead-letter notification implementation. Currently logs a critical message with context.
    /// Can be extended to send emails, webhooks, or integrate with monitoring systems.
    /// </summary>
    public class DeadLetterNotificationService : IDeadLetterNotificationService
    {
        private readonly ILogger<DeadLetterNotificationService> _logger;

        public DeadLetterNotificationService(ILogger<DeadLetterNotificationService> logger)
        {
            _logger = logger;
        }

        public Task NotifyAsync(OutboxMessage message, string? exception, CancellationToken cancellationToken = default)
        {
            _logger.LogCritical(
                "Outbox message {MessageId} moved to dead-letter. MessageType={MessageType}, CorrelationId={CorrelationId}, RetryCount={RetryCount}, Exception={Exception}",
                message.Id,
                message.MessageType,
                message.CorrelationId,
                message.RetryCount,
                exception);

            // Keep this async-friendly for future integrations (email/webhook)
            return Task.CompletedTask;
        }
    }
}
