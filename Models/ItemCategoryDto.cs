using Erp.Domain.Models;
using System.Text.Json;

namespace Portal.Models
{
    public class ItemCategoryDTO
    {
        public PortalCategory? PortalCategory { get; set; }
        public string? Title { get; set; } = null;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? PageUrl { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
    }

    public static class PortalItemCategoryExtension
    {
        public static ItemCategoryDTO MapToDto(this PortalCategory category)
        {
            JsonDto jObject = JsonSerializer.Deserialize<JsonDto>(category.JsonData ?? "{}") ?? new JsonDto();
            return new ItemCategoryDTO
            {
                PortalCategory = category,
                Title = jObject.Title,
                Description = jObject.Description,
                ImageUrl = jObject.ImageUrl,
                PageUrl = jObject.PageUrl,
                SeoTitle = jObject.SeoTitle,
                SeoDescription = jObject.SeoDescription
            };
        }

    }
}
