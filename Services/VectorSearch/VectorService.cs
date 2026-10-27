using Microsoft.Extensions.Caching.Distributed;
using Portal.Services.VectorSearch.Models;

namespace Portal.Services.VectorSearch
{
    public class VectorService: IVectorService
    {
        private const string VectorDbCacheKey = "VectorDbScheduleSettings";
        private readonly IDistributedCache _cache;
        private readonly ILogger<VectorService> _logger;

        public VectorService(
                        IDistributedCache cache,
                        ILogger<VectorService> logger
            )
        {
            _cache = cache;
            _logger = logger;
        }

        public VectorDbScheduleSettings? GetScheduleSetting()
        {
            try
            {
                var cachedBytes = _cache.Get(VectorDbCacheKey);
                if (cachedBytes == null)
                {
                    return null;
                }

                using (var ms = new System.IO.MemoryStream(cachedBytes))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<VectorDbScheduleSettings>(ms);
                }
            }
            catch (Exception ex)
            {
                // Log error and return null if cache retrieval fails
                _logger.LogError(ex, "Failed to retrieve VectorDbScheduleSettings from cache.");
                return null;
            }
        }

        public int SetScheduleSetting(VectorDbScheduleSettings setting)
        {
            try
            {
                var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(setting);
                _cache.Set(VectorDbCacheKey, bytes);

                return 1; // Indicate success
            }
            catch (Exception ex)
            {
                // Log error and return 0 if cache update fails
                _logger.LogError(ex, "Failed to set VectorDbScheduleSettings in cache.");
                return 0;
            }
        }
    }
}
