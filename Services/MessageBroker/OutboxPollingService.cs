using System.Text.Json;
using MassTransit;

namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Background worker that publishes pending outbox messages to the MassTransit bus.
    /// </summary>
    public class OutboxPollingService : BackgroundService
    {
        private const int BatchSize = 50;
        private const int MaxRetries = 5;
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPollingService> _logger;

        public OutboxPollingService(IServiceScopeFactory scopeFactory, ILogger<OutboxPollingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox polling service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while polling outbox");
                }

                try
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ProcessPendingAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxService>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var deadLetterNotifier = scope.ServiceProvider.GetService<IDeadLetterNotificationService>();

            var pending = await outbox.GetPendingAsync(BatchSize, MaxRetries, cancellationToken);

            foreach (var item in pending)
            {
                try
                {
                    var message = new BrokerMessage
                    {
                        Id = item.Id,
                        MessageType = item.MessageType,
                        Payload = item.Payload,
                        Timestamp = item.CreatedAt,
                        CorrelationId = item.CorrelationId,
                        PartitionKey = item.PartitionKey,
                        Headers = string.IsNullOrEmpty(item.Headers)
                            ? null
                            : JsonSerializer.Deserialize<Dictionary<string, string>>(item.Headers)
                    };

                    await publishEndpoint.Publish(message, ctx =>
                    {
                        ctx.MessageId = message.Id;
                        ctx.CorrelationId = message.CorrelationId;
                    }, cancellationToken);

                    await outbox.MarkAsProcessedAsync(item.Id, cancellationToken);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}", item.Id);
                    var newRetry = await outbox.MarkAsFailedAsync(item.Id, ex.ToString(), cancellationToken);

                    // If we've reached or exceeded the max retries, move to dead-letter and notify
                    if (newRetry >= MaxRetries)
                    {
                        await outbox.MarkAsDeadLetterAsync(item.Id, ex.ToString(), cancellationToken);

                        if (deadLetterNotifier is not null)
                        {
                            // update in-memory item for notification context
                            item.RetryCount = newRetry;
                            item.Status = OutboxMessageStatus.DeadLetter;
                            await deadLetterNotifier.NotifyAsync(item, ex.ToString(), cancellationToken);
                        }
                        else
                        {
                            _logger.LogCritical("Outbox message {MessageId} reached max retries and was dead-lettered but no IDeadLetterNotificationService is registered.", item.Id);
                        }
                    }
                }
            }
        }
    }
}
