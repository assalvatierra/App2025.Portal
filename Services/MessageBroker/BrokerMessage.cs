namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Represents a message contract for the message broker system.
    /// Used by both publishers and consumers to serialize/deserialize messages.
    /// </summary>
    public class BrokerMessage
    {
        /// <summary>
        /// Unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The type or category of the message (e.g., "OrderCreated", "InventoryUpdated").
        /// Used for routing and message type-based processing.
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was created (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The serialized message payload (typically JSON).
        /// Contains the actual business data being transmitted.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for tracking related messages across the system.
        /// Used to link a chain of messages that belong to the same business transaction or workflow.
        /// </summary>
        public Guid? CorrelationId { get; set; }

        /// <summary>
        /// Optional metadata headers for the message.
        /// Can contain routing information, custom properties, or trace identifiers.
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Optional partition key for message queue partitioning (e.g., Redis streams, Kafka).
        /// Used to ensure ordering of related messages within a partition.
        /// </summary>
        public string? PartitionKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the BrokerMessage class.
        /// </summary>
        public BrokerMessage()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the BrokerMessage class with specified message type and payload.
        /// </summary>
        /// <param name="messageType">The type/category of the message.</param>
        /// <param name="payload">The serialized message payload.</param>
        public BrokerMessage(string messageType, string payload)
            : this()
        {
            MessageType = messageType;
            Payload = payload;
        }
    }
}
