using System.Text.Json;

namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Base class that deserializes <see cref="BrokerMessage.Payload"/> into <typeparamref name="TPayload"/>.
    /// MessageType defaults to the payload type name, matching <see cref="IOutboxPublisher"/>.
    /// </summary>
    public abstract class MessageConsumerBase<TPayload> : IMessageConsumer where TPayload : class
    {
        public virtual string MessageType => typeof(TPayload).Name;

        public async Task ConsumeAsync(BrokerMessage message, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Deserialize<TPayload>(message.Payload)
                ?? throw new InvalidOperationException(
                    $"Payload of message {message.Id} could not be deserialized to {typeof(TPayload).Name}.");

            await HandleAsync(payload, message, cancellationToken);
        }

        protected abstract Task HandleAsync(TPayload payload, BrokerMessage message, CancellationToken cancellationToken);
    }
}
