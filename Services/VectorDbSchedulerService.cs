using Portal.Models;
using Portal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Portal.Services
{
    /// <summary>
    /// Background service that monitors VectorDB schedule settings and triggers updates
    /// when the scheduled time is reached.
    /// </summary>
    public class VectorDbSchedulerService : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
        private const string VectorDbCacheKey = "VectorDbScheduleSettings";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VectorDbSchedulerService> _logger;

        public VectorDbSchedulerService(IServiceScopeFactory scopeFactory, ILogger<VectorDbSchedulerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VectorDB Scheduler service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndTriggerScheduledUpdatesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while checking VectorDB schedule");
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

            _logger.LogInformation("VectorDB Scheduler service stopped");
        }

        private async Task CheckAndTriggerScheduledUpdatesAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

                        // Retrieve schedule settings from distributed cache
                        var cachedBytes = cache.Get(VectorDbCacheKey);
                        if (cachedBytes == null)
                        {
                            _logger.LogDebug("No VectorDB schedule settings found in cache at {Timestamp}. Creating initial schedule.", DateTime.UtcNow);

                            // Create initial schedule: 60 seconds from now, with 1-day interval
                            var currentTime = DateTime.UtcNow;
                            var initialSettings = new VectorDbScheduleSettings
                            {
                                NextSchedule = currentTime.AddSeconds(60),
                                IntervalValue = 1,
                                IntervalUnit = "Days",
                                LastModified = currentTime,
                                LastUpdateTriggered = null
                            };

                            // Save to cache
                            var cacheOptions = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
                            };
                            using (var ms = new System.IO.MemoryStream())
                            {
                                using (var writer = new System.IO.StreamWriter(ms))
                                {
                                    JsonSerializer.Serialize(writer.BaseStream, initialSettings);
                                    writer.Flush();
                                    cache.Set(VectorDbCacheKey, ms.ToArray(), cacheOptions);
                                }
                            }

                            _logger.LogInformation(
                                "Initial VectorDB schedule created at {Timestamp}. Next update scheduled for {NextSchedule} UTC with 1-day interval.",
                                currentTime, initialSettings.NextSchedule);

                            return;
                        }

                        VectorDbScheduleSettings? settings;
                        try
                        {
                            using (var ms = new System.IO.MemoryStream(cachedBytes))
                            {
                                settings = JsonSerializer.Deserialize<VectorDbScheduleSettings>(ms);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deserializing VectorDB schedule settings from cache");
                            return;
                        }

                        if (settings == null)
                        {
                            _logger.LogWarning("Deserialized VectorDB schedule settings is null");
                            return;
                        }

                        // Check if it's time to trigger the update
                        var now = DateTime.UtcNow;
                        if (now >= settings.NextSchedule)
                        {
                            _logger.LogInformation(
                                "VectorDB scheduled update triggered at {Timestamp}. Last scheduled: {LastSchedule}. Interval: {Interval} {IntervalUnit}",
                                now, settings.NextSchedule, settings.IntervalValue, settings.IntervalUnit);

                            // Update the last triggered time
                            settings.LastUpdateTriggered = now;

                            // Calculate next schedule based on interval
                            settings.NextSchedule = settings.IntervalUnit.ToLower() switch
                            {
                                "minutes" => now.AddMinutes(settings.IntervalValue),
                                "hours" => now.AddHours(settings.IntervalValue),
                                "days" => now.AddDays(settings.IntervalValue),
                                _ => now.AddHours(1)
                            };

                            // Save updated settings back to cache
                            var cacheOptions = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
                            };
                            using (var ms = new System.IO.MemoryStream())
                            {
                                using (var writer = new System.IO.StreamWriter(ms))
                                {
                                    JsonSerializer.Serialize(writer.BaseStream, settings);
                                    writer.Flush();
                                    cache.Set(VectorDbCacheKey, ms.ToArray(), cacheOptions);
                                }
                            }

                            _logger.LogInformation(
                                "VectorDB schedule updated. Next scheduled update: {NextSchedule} UTC",
                                settings.NextSchedule);

                            // TODO: Call the actual vector database update service here
                            // Example: await vectorDbService.RefreshCollectionsAndIndexesAsync(cancellationToken);
                        }
                        else
                        {
                            var timeUntilNext = settings.NextSchedule - now;
                            _logger.LogDebug(
                                "VectorDB update not yet due. Next scheduled: {NextSchedule} UTC (in {TimeRemaining})",
                                settings.NextSchedule, timeUntilNext);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in CheckAndTriggerScheduledUpdatesAsync");
                }
            }, cancellationToken);
        }
    }
}
