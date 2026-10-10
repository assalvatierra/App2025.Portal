using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Portal.Services.MessageBroker
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly ApplicationDbContext _db;
        private readonly IdempotencySettings _settings;

        public IdempotencyService(ApplicationDbContext db, Microsoft.Extensions.Options.IOptions<IdempotencySettings> settings)
        {
            _db = db;
            _settings = settings.Value;
        }

        public async Task<bool> TryReserveAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default)
        {
            if (!_settings.EnableIdempotency)
                return true;

            var now = DateTime.UtcNow;

            var entry = new ProcessedMessage
            {
                MessageId = messageId,
                ConsumerType = consumerType,
                ProcessingStartedAt = now,
                ExpiresAt = now.AddDays(_settings.RetentionDays)
            };

            try
            {
                _db.ProcessedMessage.Add(entry);
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                // Unique constraint violation or similar - treat as already reserved/processed
                return false;
            }
        }

        public async Task MarkProcessedAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default)
        {
            if (!_settings.EnableIdempotency)
                return;

            var record = await _db.ProcessedMessage
                .FirstOrDefaultAsync(p => p.MessageId == messageId && p.ConsumerType == consumerType, cancellationToken);

            if (record == null)
            {
                // If the reservation wasn't present, attempt to insert a completed record
                var now = DateTime.UtcNow;
                _db.ProcessedMessage.Add(new ProcessedMessage
                {
                    MessageId = messageId,
                    ConsumerType = consumerType,
                    ProcessingStartedAt = now,
                    ProcessedAt = now,
                    ExpiresAt = now.AddDays(_settings.RetentionDays)
                });
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            record.ProcessedAt = DateTime.UtcNow;
            record.ExpiresAt = DateTime.UtcNow.AddDays(_settings.RetentionDays);
            _db.ProcessedMessage.Update(record);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ReleaseReservationAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default)
        {
            if (!_settings.EnableIdempotency)
                return;

            var record = await _db.ProcessedMessage
                .FirstOrDefaultAsync(p => p.MessageId == messageId && p.ConsumerType == consumerType, cancellationToken);

            if (record == null)
                return;

            // If it has not been processed yet, remove so it can be retried later
            if (record.ProcessedAt == null)
            {
                _db.ProcessedMessage.Remove(record);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> CheckIfProcessedAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default)
        {
            if (!_settings.EnableIdempotency)
                return false;

            return await _db.ProcessedMessage
                .AnyAsync(p => p.MessageId == messageId && p.ConsumerType == consumerType && p.ProcessedAt != null, cancellationToken);
        }
    }
}
