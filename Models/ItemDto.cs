using Erp.Domain.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Portal.Models
{
    public class ItemDto
    {
        public PortalItem? PortalItem { get; set; }
        public string? Title { get; set; } = null;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageAlt { get; set; }
        public string? PageUrl { get; set; }

    }

    public static class PortalItemExtensions
    {
        public static ItemDto MapToDto(this PortalItem item)
        {
            JsonDto jObject = JsonSerializer.Deserialize<JsonDto>(item.JsonData ?? "{}") ?? new JsonDto ();
            
            return new ItemDto
            {
                PortalItem = item,
                Title = jObject.Title,
                Description = jObject.Description,
                ImageUrl = jObject.ImageUrl,
                ImageAlt = jObject.ImageAlt,
                PageUrl = jObject.PageUrl
            };

        }
    }

}
