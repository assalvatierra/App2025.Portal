using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Services.MessageBroker
{
    [Table("ProcessedMessage")]
    public class ProcessedMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid MessageId { get; set; }

        [Required]
        [MaxLength(512)]
        public string ConsumerType { get; set; } = string.Empty;

        /// <summary>
        /// When processing started (reserved). Nullable until reserved.
        /// </summary>
        public DateTime? ProcessingStartedAt { get; set; }

        /// <summary>
        /// When processed successfully. Null while processing or if processing failed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Optional expiry used by cleanup job.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
