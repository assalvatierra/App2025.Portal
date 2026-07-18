using Erp.Domain.Models;
using Portal.DBLayer;
using Portal.Models;
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
        }


        private readonly IPortalContentDbLayer _portalContentDbLayer;
        public PortalContentService(IPortalContentDbLayer portalContentDbLayer)
        {
            _portalContentDbLayer = portalContentDbLayer;
        }
        public async Task<List<ContentDto>> GetContentsByCategoryAsync(string category, string? type)
        {
            var content = await _portalContentDbLayer.GetContentsByCategoryAsync(category, type);
            return content.Select(c =>
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
        public async Task<PortalContent?> GetByIdAsync(int id)
        {
            return await _portalContentDbLayer.GetByIdAsync(id);
        }
    }
}
