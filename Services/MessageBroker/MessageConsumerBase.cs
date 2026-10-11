using System.Text.Json;

namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Base class that deserializes <see cref="BrokerMessage.Payload"/> into <typeparamref name="TPayload"/>.
    /// MessageType defaults to the payload type name, matching <see cref="IOutboxPublisher"/>.
    /// </summary>
    public abstract class MessageConsumerBase<TPayload> : IMessageConsumer where TPayload : class
    {
        private readonly HashSet<string>? _assignedPartitionKeys;

        protected MessageConsumerBase(IEnumerable<string>? assignedPartitionKeys = null)
        {
            _assignedPartitionKeys = assignedPartitionKeys is null
                ? null
                : new HashSet<string>(assignedPartitionKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()), StringComparer.Ordinal);
        }

        public virtual string MessageType => typeof(TPayload).Name;

        public async Task<bool> ConsumeAsync(BrokerMessage message, CancellationToken cancellationToken = default)
        {
            // If this consumer has assigned partition keys, skip messages that don't match.
            if (_assignedPartitionKeys is not null && _assignedPartitionKeys.Count > 0)
            {
                var messagePartitionKey = message.PartitionKey;
                var isMatch = !string.IsNullOrEmpty(messagePartitionKey) && _assignedPartitionKeys.Contains(messagePartitionKey);

                System.Diagnostics.Debug.WriteLine($"[PartitionKeyFilter] Message ID {message.Id}: partitionKey='{messagePartitionKey}', assignedKeys=[{string.Join(",", _assignedPartitionKeys)}], match={isMatch}");

                if (!isMatch)
                {
                    // Intentionally skip processing for partition key mismatch. Assume another handler will process it.
                    System.Diagnostics.Debug.WriteLine($"[PartitionKeyFilter] Message ID {message.Id}: SKIPPED - partition key mismatch");
                    return false;  // Return false to indicate message was skipped
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PartitionKeyFilter] Consumer has no partition key filtering enabled (will process all messages)");
            }

            var payload = JsonSerializer.Deserialize<TPayload>(message.Payload)
                ?? throw new InvalidOperationException(
                    $"Payload of message {message.Id} could not be deserialized to {typeof(TPayload).Name}.");

            await HandleAsync(payload, message, cancellationToken);
            return true;  // Return true to indicate message was processed
        }

        protected abstract Task HandleAsync(TPayload payload, BrokerMessage message, CancellationToken cancellationToken);
    }
}
