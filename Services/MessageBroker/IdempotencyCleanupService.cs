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

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<Portal.Data.ApplicationDbContext>();

                    var now = DateTime.UtcNow;
                    var expired = await db.ProcessedMessage
                        .Where(p => p.ExpiresAt != null && p.ExpiresAt <= now)
                        .ToListAsync(stoppingToken);

                    if (expired.Count > 0)
                    {
                        db.ProcessedMessage.RemoveRange(expired);
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Deleted {Count} expired processed message records", expired.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during idempotency cleanup");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
