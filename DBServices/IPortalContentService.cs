using Erp.Domain.Models;
using Portal.Models;

namespace Portal.DBServices
{
    public interface IPortalContentService
    {
        Task<List<ContentDto>> GetContentsByCategoryAsync(string category, string? type);
        Task<PortalContent?> GetByIdAsync(int id);

    }
}
