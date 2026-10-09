using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Entity persisted in the outbox table. Published asynchronously by <see cref="OutboxPollingService"/>.
    /// </summary>
    [Table("OutboxMessage")]
    public class OutboxMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(256)]
        public string MessageType { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        public Guid? CorrelationId { get; set; }

        [MaxLength(256)]
        public string? PartitionKey { get; set; }

        /// <summary>
        /// JSON-serialized message headers.
        /// </summary>
        public string? Headers { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public int RetryCount { get; set; }

        public string? Exception { get; set; }
    }
}
