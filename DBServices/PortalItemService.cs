using Erp.Domain.Models;
using Portal.DBLayer;
using Portal.Models;
using Portal.Services;

namespace Portal.DBServices
{
    public class PortalItemService : IPortalItemService
    {
        private readonly IPortalItemDbLayer _db;
        private readonly IPortalItemSpecDbLayer _dbItemSpecs;
        private readonly ICache _cache;

        public PortalItemService(IPortalItemDbLayer db, IPortalItemSpecDbLayer db2, ICache cache)
        {
            _db = db;
            _dbItemSpecs = db2;
            _cache = cache;
        }

        public async Task<List<PortalItem>> GetAllAsync()
        {
            return await _db.GetAllAsync();
        }

        public async Task<List<ItemDto>> SearchItemsAsync(SearchDto search)
        {
            var specs = await _dbItemSpecs.GetItemISpecsCriteriaAsync(search);
            var itemIds = specs.Select(s => (int)s.PortalItemId).Distinct().ToList();
            //return itemIds;
            //List<int> itemIds = await _dbItemSpecs.GetItemIdsBySpecsCriteriaAsync(search);
            var items = await _db.SearchItemsAsync(search.searchTerm, itemIds);
            return items.Select(item => 
            { 
                var dto = item.MapToDto();
                //dto.PortalItem.PortalItemSpecs = specs.Where(s => s.PortalItemId == item.Id).ToList();
                return dto;
            }).ToList();
        }

        public async Task<List<ItemDto>> GetItemsByCategory(string category, string? type)
        {
            // Create a cache key based on category and type
            var cacheKey = $"items_category_{category}_{type ?? "all"}";

            // Try to get from cache first
            var cachedItems = await _cache.GetAsync<List<ItemDto>>(cacheKey);
            if (cachedItems != null)
            {
                return cachedItems;
            }

            // If not in cache, get from database
            var items = await _db.GetItemsByCategoryAsync(category, type);

            var result = items.Select(item =>
            {
                ItemDto dto = item.MapToDto();
                return dto;
            }).ToList();

            // Cache the result for 1 hour
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        public async Task<List<ItemDto>> GetByIdListAsync(List<int> IdList)
        {
            var items = await _db.GetByIdListAsync(IdList);
            return items.Select(item => item.MapToDto()).ToList();
        }
        public async Task<PortalItem?> GetByIdAsync(int id)
        {
            return await _db.GetByIdAsync(id);
        }

        public async Task UpdateAsync(PortalItem portalItem)
        {
            portalItem.LastEditOn = DateTime.UtcNow;
            await _db.UpdateAsync(portalItem);
        }

        public async Task<PortalItem> AddAsync(PortalItem portalItem)
        {
            portalItem.CreatedOn = DateTime.UtcNow;
            portalItem.LastEditOn = DateTime.UtcNow;
            portalItem.RecordGuid = Guid.NewGuid();
            return await _db.AddAsync(portalItem);
        }

        public async Task DeleteAsync(PortalItem portalItem)
        {
            await _db.DeleteAsync(portalItem);
        }

        public bool Exists(int id)
        {
            return _db.Exists(id);
        }
    }
}
