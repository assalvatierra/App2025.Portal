using System.Diagnostics;
using System.Text.Json;
using Portal.Services.Observability;

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
            // Create an Activity for this message consumption
            using var activity = PortalActivitySource.Instance.StartActivity(
                PortalActivitySource.ActivityNames.MessageConsume,
                ActivityKind.Consumer);

            if (activity is not null)
            {
                activity.SetTag(PortalActivitySource.AttributeNames.MessageId, message.Id);
                activity.SetTag(PortalActivitySource.AttributeNames.MessageType, MessageType);
                if (!string.IsNullOrEmpty(message.PartitionKey))
                {
                    activity.SetTag(PortalActivitySource.AttributeNames.PartitionKey, message.PartitionKey);
                }
            }

            try
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
                        activity?.SetTag("messaging.skipped_reason", "partition_key_mismatch");
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
                activity?.SetTag("messaging.message_processed", true);
                return true;  // Return true to indicate message was processed
            }
            catch (Exception ex)
            {
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection(
                        new[] { new KeyValuePair<string, object?>("exception.type", ex.GetType().Name),
                                new KeyValuePair<string, object?>("exception.message", ex.Message) })));
                }
                throw;
            }
        }

        protected abstract Task HandleAsync(TPayload payload, BrokerMessage message, CancellationToken cancellationToken);
    }
}
