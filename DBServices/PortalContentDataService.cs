using Erp.Domain.Models;
using Portal.DBLayer;
using Portal.Services;
using System.Text.Json;

namespace Portal.DBServices
{
    public class PortalContentDataService : IPortalContentDataService
    {
        private class JsonContentData
        {
            public int? ContentDataID { get; set; }
        }

        private readonly IPortalContentDataDbLayer _portalContentDataDbLayer;
        private readonly IPortalContentDbLayer _portalContentDbLayer;
        private readonly IPortalItemDbLayer _portalItemDbLayer;
        private readonly ICache _cache;

        public PortalContentDataService(
            IPortalContentDataDbLayer portalContentDataDbLayer,
            IPortalContentDbLayer portalContentDbLayer,
            IPortalItemDbLayer portalItemDbLayer,
            ICache cache)
        {
            _portalContentDataDbLayer = portalContentDataDbLayer;
            _portalContentDbLayer = portalContentDbLayer;
            _portalItemDbLayer = portalItemDbLayer;
            _cache = cache;
        }

        public async Task<List<PortalContentData>> GetAllAsync()
        {
            return await _portalContentDataDbLayer.GetAllAsync();
        }

        public async Task<List<PortalContentData>> GetByTypeAsync(string dataType)
        {
            return await _portalContentDataDbLayer.GetByTypeAsync(dataType);
        }

        public async Task<PortalContentData?> GetByIdAsync(int id)
        {
            return await _portalContentDataDbLayer.GetByIdAsync(id);
        }

        public async Task<(PortalContentData?, PortalContent?)> GetByContentNameAsync(string contentName)
        {
            // Create a cache key for this content
            var cacheKey = $"contentdata_content_{contentName}";

            // Try to get from cache first
            var cachedResult = await _cache.GetAsync<(PortalContentData?, PortalContent?)>(cacheKey);
            if (cachedResult != default)
            {
                return cachedResult;
            }

            // Get the PortalContent by name
            var portalContent = await _portalContentDbLayer.GetByNameAsync(contentName);

            if (portalContent == null || string.IsNullOrEmpty(portalContent.JsonData))
            {
                return (null, null);
            }

            // Extract ContentDataID from JsonData
            var jsonData = JsonSerializer.Deserialize<JsonContentData>(portalContent.JsonData);

            if (jsonData?.ContentDataID == null)
            {
                return (null, portalContent);
            }

            // Get and return the PortalContentData using the extracted ID
            var contentData = await _portalContentDataDbLayer.GetByIdAsync(jsonData.ContentDataID.Value);
            var result = (contentData, portalContent);

            // Cache the result for 1 hour
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        public async Task<(PortalContentData?, PortalItem?)> GetByItemNameAsync(string itemName)
        {
            // Create a cache key for this item
            var cacheKey = $"contentdata_item_{itemName}";

            // Try to get from cache first
            var cachedResult = await _cache.GetAsync<(PortalContentData?, PortalItem?)>(cacheKey);
            if (cachedResult != default)
            {
                return cachedResult;
            }

            // Get the PortalItem by name
            var portalItem = await _portalItemDbLayer.GetByNameAsync(itemName);

            if (portalItem == null || string.IsNullOrEmpty(portalItem.JsonData))
            {
                return (null, null);
            }

            // Extract ContentDataID from JsonData
            var jsonData = JsonSerializer.Deserialize<JsonContentData>(portalItem.JsonData);

            if (jsonData?.ContentDataID == null)
            {
                return (null, portalItem);
            }

            // Get and return the PortalContentData and PortalItem
            var contentData = await _portalContentDataDbLayer.GetByIdAsync(jsonData.ContentDataID.Value);
            var result = (contentData, portalItem);

            // Cache the result for 1 hour
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        public async Task UpdateAsync(PortalContentData portalContentData)
        {
            await _portalContentDataDbLayer.UpdateAsync(portalContentData);
        }

        public async Task<PortalContentData> AddAsync(PortalContentData portalContentData)
        {
            return await _portalContentDataDbLayer.AddAsync(portalContentData);
        }

        public async Task DeleteAsync(PortalContentData portalContentData)
        {
            await _portalContentDataDbLayer.DeleteAsync(portalContentData);
        }
    }
}
