using System.Threading;
using System.Threading.Tasks;

namespace Portal.Services.MessageBroker
{
    public interface IDeadLetterNotificationService
    {
        /// <summary>
        /// Notify that a message has been moved to the dead-letter state.
        /// </summary>
        Task NotifyAsync(OutboxMessage message, string? exception, CancellationToken cancellationToken = default);
    }
}
