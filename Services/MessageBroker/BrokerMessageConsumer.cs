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

        public BrokerMessageConsumer(IEnumerable<IMessageConsumer> consumers, ILogger<BrokerMessageConsumer> logger)
        {
            _consumers = consumers;
            _logger = logger;
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
                await handler.ConsumeAsync(message, context.CancellationToken);
            }
        }
    }
}
