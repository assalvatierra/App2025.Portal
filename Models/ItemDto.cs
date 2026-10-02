using Erp.Domain.Models;
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
        public int? ContentDataID { get; set; }

    }
    
    //public class PortalItemDto
    //{
    //    public int Id { get; set; }
    //    public string? Name { get; set; }
    //    public string? JsonData { get; set; }
    //    public string? Category { get; set; }
    //    public string? Type { get; set; }
    //    public DateTime CreatedAt { get; set; }
    //    public DateTime UpdatedAt { get; set; }
    //}

    public static class PortalItemExtensions
    {
        public static ItemDto MapToDto(this PortalItem item)
        {
            JsonDto jObject = JsonSerializer.Deserialize<JsonDto>(item.JsonData ?? "{}") ?? new JsonDto ();

            //PortalItemDto portalitem = new PortalItemDto
            //{
            //    Id = item.Id,
            //    Name = item.Name
            //};

            return new ItemDto
            {
                PortalItem = item,
                Title = jObject.Title,
                Description = jObject.Description,
                ImageUrl = jObject.ImageUrl,
                ImageAlt = jObject.ImageAlt,
                PageUrl = jObject.PageUrl,
                ContentDataID = jObject.ContentDataID
            };

        }
    }

}
