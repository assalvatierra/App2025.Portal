using Erp.Domain.Models;
using Portal.Models;

namespace Portal.DBServices
{
    public interface IPortalContentService
    {
        Task<List<ContentDto>> GetContentsByCategoryAsync(List<string> category, string? type);
        Task<List<ContentDto>> GetAllActiveContentsAsync();
        Task<PortalContent?> GetByIdAsync(int id);

    }
}
