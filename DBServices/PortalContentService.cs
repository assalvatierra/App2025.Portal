using Erp.Domain.Models;
using Portal.DBLayer;
using Portal.Models;
using Portal.Services;
using System.Text.Json;

namespace Portal.DBServices
{
    public class PortalContentService: IPortalContentService
    {
        private class JObject
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? ImageUrl { get; set; }
            public string? PageUrl { get; set; }
            public int? ContentDataID { get; set; }
        }

        private readonly IConfiguration _configuration;
        private readonly IPortalContentDbLayer _portalContentDbLayer;
        private readonly ICache _cache;

        public PortalContentService(
            IConfiguration configuration,
            IPortalContentDbLayer portalContentDbLayer,
            ICache cache)
        {
            _portalContentDbLayer = portalContentDbLayer;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<List<ContentDto>> GetAllActiveContentsAsync()
        {
            var contents = await _portalContentDbLayer.GetContentsByStatusAsync("Active");
            return contents.Select(c =>
            {
                JObject jObject = JsonSerializer.Deserialize<JObject>(c.JsonData ?? "{}") ?? new JObject();
                return new ContentDto
                {
                    Content = c,
                    Title = jObject.Title,
                    Description = jObject.Description,
                    ImageUrl = jObject.ImageUrl,
                    PageUrl = jObject.PageUrl
                };
            }).ToList();
        }

        public async Task<List<ContentDto>> GetContentsByCategoryAsync(List<string> category, string? type)
        {
            // Create a cache key based on categories and type
            var categoryKey = string.Join("_", category.OrderBy(c => c));
            var cacheKey = $"content_category_{categoryKey}_{type ?? "all"}";

            // Try to get from cache first
            var cachedContents = await _cache.GetAsync<List<ContentDto>>(cacheKey);
            if (cachedContents != null)
            {
                return cachedContents;
            }

            var TemporaryContents = _configuration["TemporaryContents:Enabled"];
            if (bool.Parse(TemporaryContents))
            {
                var TemporaryServices = _configuration["TemporaryContents:" + type];
                category = category.Concat(new[] { TemporaryServices }).ToList();
            }

            var content = await _portalContentDbLayer.GetContentsByCategoryAsync(category, type);
            var result = content.Select(c =>
                {
                    JObject jObject = JsonSerializer.Deserialize<JObject>(c.JsonData ?? "{}") ?? new JObject();
                    return new ContentDto
                    {
                        Content = c,
                        Title = jObject.Title,
                        Description = jObject.Description,
                        ImageUrl = jObject.ImageUrl,
                        PageUrl = jObject.PageUrl,
                        ContentDataID = jObject.ContentDataID
                    };
                }).ToList();

            // Cache the result for 1 hour
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }
        public async Task<PortalContent?> GetByIdAsync(int id)
        {
            return await _portalContentDbLayer.GetByIdAsync(id);
        }
    }
}
