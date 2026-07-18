using Erp.Domain.Models;
using Portal.DBLayer;
using Portal.Models;
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
        public PortalCategoryServices(IPortalCategoryDbLayer dbLayer)
        {
            _dbLayer = dbLayer;
        }
        public async Task<List<ItemCategoryDTO>> GetAllByStatusAsync(string? status)
        {
            var categories = await _dbLayer.GetAllByStatusAsync(status);
            return categories.Select(c =>
            {
                JObject jObject = JsonSerializer.Deserialize<JObject>(c.JsonData ?? "{}") ?? new JObject();
                return new ItemCategoryDTO
                {
                    PortalCategory = c,
                    Title = jObject.Title,
                    Description = jObject.Description,
                    ImageUrl = jObject.ImageUrl,
                    PageUrl = jObject.PageUrl
                };
            }).ToList();


        }
        public async Task<PortalCategory?> GetByIdAsync(int id)
        {
            return await _dbLayer.GetByIdAsync(id);
        }
    }
}
