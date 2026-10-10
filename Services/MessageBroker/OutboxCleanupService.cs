using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portal.Data;

namespace Portal.Services.MessageBroker
{
    public class OutboxCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);
        private readonly OutboxCleanupSettings _settings;

        public OutboxCleanupService(
            IServiceProvider serviceProvider,
            ILogger<OutboxCleanupService> logger,
            IOptions<OutboxCleanupSettings> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.EnableCleanup)
            {
                _logger.LogInformation("Outbox cleanup service is disabled");
                return;
            }

            _logger.LogInformation("Outbox cleanup service started with {RetentionDays} day(s) retention", _settings.RetentionDays);

            // Initial delay to avoid running immediately on startup
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);
                    const int batchSize = 1000;
                    int totalDeleted = 0;

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var rowsDeleted = await db.OutboxMessage
                            .Where(m => m.Status == OutboxMessageStatus.Processed 
                                && m.ProcessedAt != null 
                                && m.ProcessedAt < cutoffDate)
                            .Take(batchSize)
                            .ExecuteDeleteAsync(stoppingToken);

                        if (rowsDeleted == 0)
                        {
                            break;
                        }

                        totalDeleted += rowsDeleted;
                        _logger.LogDebug("Deleted {RowCount} processed outbox messages", rowsDeleted);
                    }

                    if (totalDeleted > 0)
                    {
                        _logger.LogInformation("Outbox cleanup completed: deleted {TotalCount} processed message(s)", totalDeleted);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Outbox cleanup service is stopping");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during outbox cleanup");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when service is stopping
                }
            }
        }
    }
}
