using Microsoft.Extensions.Caching.Distributed;
using Portal.Services.VectorSearch.Models;

namespace Portal.Services.VectorSearch
{
    public class VectorService: IVectorService
    {
        private const string VectorDbCacheKey = "VectorDbScheduleSettings";
        private readonly IDistributedCache _cache;

        public VectorService(
                        IDistributedCache cache
            )
        {
            _cache = cache;
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
                return 0;
            }
        }
    }
}
