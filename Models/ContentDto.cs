using Erp.Domain.Models;

namespace Portal.Models
{
    public class ContentDto
    {
        public PortalContent Content{ get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; } = null;
        public string? PageUrl { get; set; } = null;
    }
}
