using System.Text.Json;
using Portal.Data;

namespace Portal.Services.MessageBroker
{
    public class OutboxPublisher : IOutboxPublisher
    {
        private readonly IOutboxService _outboxService;
        private readonly ApplicationDbContext _context;

        public OutboxPublisher(IOutboxService outboxService, ApplicationDbContext context)
        {
            _outboxService = outboxService;
            _context = context;
        }

        public async Task PublishAsync(BrokerMessage message, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            await _outboxService.AddAsync(message, cancellationToken);

            if (saveChanges)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public Task PublishAsync<T>(T payload, string? messageType = null, Guid? correlationId = null, string? partitionKey = null,
            bool saveChanges = false, CancellationToken cancellationToken = default) where T : class
        {
            var message = new BrokerMessage(messageType ?? typeof(T).Name, JsonSerializer.Serialize(payload))
            {
                CorrelationId = correlationId,
                PartitionKey = partitionKey
            };

            return PublishAsync(message, saveChanges, cancellationToken);
        }
    }
}
