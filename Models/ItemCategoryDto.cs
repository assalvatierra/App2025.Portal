using Erp.Domain.Models;

namespace Portal.Models
{
    public class ItemCategoryDTO
    {
        public PortalCategory? PortalCategory { get; set; }
        public string? Title { get; set; } = null;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? PageUrl { get; set; }
    }
}
