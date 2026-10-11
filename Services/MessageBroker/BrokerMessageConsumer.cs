using MassTransit;

namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// MassTransit consumer that dispatches each <see cref="BrokerMessage"/> to the
    /// <see cref="IMessageConsumer"/> implementations registered for its MessageType.
    /// </summary>
    public class BrokerMessageConsumer : IConsumer<BrokerMessage>
    {
        private readonly IEnumerable<IMessageConsumer> _consumers;
        private readonly ILogger<BrokerMessageConsumer> _logger;
        private readonly IIdempotencyService _idempotencyService;

        public BrokerMessageConsumer(IEnumerable<IMessageConsumer> consumers, ILogger<BrokerMessageConsumer> logger, IIdempotencyService idempotencyService)
        {
            _consumers = consumers;
            _logger = logger;
            _idempotencyService = idempotencyService;
        }

        public async Task Consume(ConsumeContext<BrokerMessage> context)
        {
            var message = context.Message;
            var handlers = _consumers
                .Where(c => string.Equals(c.MessageType, message.MessageType, StringComparison.Ordinal))
                .ToList();

            if (handlers.Count == 0)
            {
                _logger.LogWarning("No consumer registered for message type {MessageType} (MessageId {MessageId})",
                    message.MessageType, message.Id);
                return;
            }

            foreach (var handler in handlers)
            {
                var consumerType = handler.GetType().FullName ?? handler.GetType().Name;

                // Attempt to reserve the message for this consumer to avoid duplicate processing
                var reserved = await _idempotencyService.TryReserveAsync(message.Id, consumerType, context.CancellationToken);
                if (!reserved)
                {
                    _logger.LogWarning("Skipping duplicate message {MessageId} for consumer {Consumer}", message.Id, consumerType);
                    continue;
                }

                try
                {
                    // ConsumeAsync returns true if processed, false if skipped (e.g., partition key mismatch)
                    var wasProcessed = await handler.ConsumeAsync(message, context.CancellationToken);

                    if (wasProcessed)
                    {
                        // Only mark as processed if the message was actually handled
                        await _idempotencyService.MarkProcessedAsync(message.Id, consumerType, context.CancellationToken);
                    }
                    else
                    {
                        // Message was skipped (e.g., partition key mismatch), release reservation
                        // so other consumers can process it
                        System.Diagnostics.Debug.WriteLine($"[BrokerMessageConsumer] Message {message.Id}: Released reservation for {consumerType} (skipped due to partition key)");
                        await _idempotencyService.ReleaseReservationAsync(message.Id, consumerType, context.CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // Release reservation so the message can be retried later
                    _logger.LogError(ex, "Error processing message {MessageId} for consumer {Consumer}", message.Id, consumerType);
                    await _idempotencyService.ReleaseReservationAsync(message.Id, consumerType, context.CancellationToken);
                    throw;
                }
            }
        }
    }
}
