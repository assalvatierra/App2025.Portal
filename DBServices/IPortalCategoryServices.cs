using Erp.Domain.Models;
using Portal.Models;

namespace Portal.DBServices
{
    public interface IPortalCategoryServices
    {
        Task<List<ItemCategoryDTO>> GetAllByStatusAsync(string? status);
        Task<PortalCategory?> GetByIdAsync(int id);
    }
}
