using Erp.Domain.Models;
using Portal.DBLayer;
using Portal.Models;
using Portal.Services;
using System.Text.Json;

namespace Portal.DBServices
{
    public class PortalCategoryServices: IPortalCategoryServices
    {
        private class JObject
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? ImageUrl { get; set; }
            public string? PageUrl { get; set; }
        }



        private readonly IPortalCategoryDbLayer _dbLayer;
        private readonly ICache _cache;

        public PortalCategoryServices(IPortalCategoryDbLayer dbLayer, ICache cache)
        {
            _dbLayer = dbLayer;
            _cache = cache;
        }
        public async Task<List<ItemCategoryDTO>> GetAllByStatusAsync(string? status)
        {
            var cacheKey = $"categories_status_{status ?? "all"}";

            var cached = await _cache.GetAsync<List<ItemCategoryDTO>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var categories = await _dbLayer.GetAllByStatusAsync(status);
            //return
            //{
            //    JObject jObject = JsonSerializer.Deserialize<JObject>(c.JsonData ?? "{}") ?? new JObject();
            //    return new ItemCategoryDTO
            //    {
            //        PortalCategory = c,
            //        Title = jObject.Title,
            //        Description = jObject.Description,
            //        ImageUrl = jObject.ImageUrl,
            //        PageUrl = jObject.PageUrl,
            //        SeoTitle = jObject.SeoTitle,
            //        SeoDescription = jObject.SeoDescription
            //    };
            //}).ToList();

            var result = categories.Select(c => c.MapToDto()).ToList();

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }
        public async Task<PortalCategory?> GetByIdAsync(int id)
        {
            return await _dbLayer.GetByIdAsync(id);
        }

        public async Task<ItemCategoryDTO?> GetByName(string name)
        {
            var categories = await _dbLayer.GetAllAsync();
            return categories.FirstOrDefault(c => c.Name == name)?.MapToDto();
        }

        public async Task<List<ItemCategoryDTO>> GetCategoriesByTypeAsync(string categoryType)
        {
            var cacheKey = $"categories_type_{categoryType}";

            var cached = await _cache.GetAsync<List<ItemCategoryDTO>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var categories = await _dbLayer.GetByTypeAsync(categoryType);
            var result = categories.Select(c => c.MapToDto()).ToList();

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }
    }
}
