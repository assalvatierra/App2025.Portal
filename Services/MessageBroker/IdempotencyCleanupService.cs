using Microsoft.EntityFrameworkCore;

namespace Portal.Services.MessageBroker
{
    public class IdempotencyCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IdempotencyCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public IdempotencyCleanupService(IServiceProvider serviceProvider, ILogger<IdempotencyCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Idempotency cleanup service started");

            // Initial delay to avoid running immediately on startup
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Idempotency cleanup service is stopping");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<Portal.Data.ApplicationDbContext>();

                    var now = DateTime.UtcNow;
                    const int batchSize = 1000;
                    int totalDeleted = 0;

                    // Delete in batches to avoid locking the table for too long
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var rowsDeleted = await db.ProcessedMessage
                            .Where(p => p.ExpiresAt != null && p.ExpiresAt <= now)
                            .Take(batchSize)
                            .ExecuteDeleteAsync(stoppingToken);

                        if (rowsDeleted == 0)
                        {
                            break;
                        }

                        totalDeleted += rowsDeleted;
                        _logger.LogDebug("Deleted {RowCount} expired idempotency records", rowsDeleted);
                    }

                    if (totalDeleted > 0)
                    {
                        _logger.LogInformation("Idempotency cleanup completed: deleted {TotalCount} expired record(s)", totalDeleted);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Idempotency cleanup service is stopping");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during idempotency cleanup");
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
