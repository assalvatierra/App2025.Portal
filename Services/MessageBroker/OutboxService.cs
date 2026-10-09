using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Portal.Services.MessageBroker
{
    public class OutboxService : IOutboxService
    {
        private readonly ApplicationDbContext _context;

        public OutboxService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(BrokerMessage message, CancellationToken cancellationToken = default)
        {
            var outbox = new OutboxMessage
            {
                Id = message.Id,
                MessageType = message.MessageType,
                Payload = message.Payload,
                CorrelationId = message.CorrelationId,
                PartitionKey = message.PartitionKey,
                Headers = message.Headers is null ? null : JsonSerializer.Serialize(message.Headers),
                CreatedAt = message.Timestamp
            };

            await _context.OutboxMessage.AddAsync(outbox, cancellationToken);
        }

        public Task<List<OutboxMessage>> GetPendingAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            return _context.OutboxMessage
                .Where(m => m.ProcessedAt == null && m.RetryCount < maxRetries)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.OutboxMessage
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.ProcessedAt, DateTime.UtcNow)
                    .SetProperty(m => m.Exception, (string?)null), cancellationToken);
        }

        public async Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            await _context.OutboxMessage
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.RetryCount, m => m.RetryCount + 1)
                    .SetProperty(m => m.Exception, error), cancellationToken);
        }
    }
}
