using Erp.Domain.Models;
using Portal.Models;

namespace Portal.DBServices
{
    public interface IPortalItemSpecService
    {
        Task<List<PortalItemSpec>> GetAllAsync();
        //Task<List<int>> GetItemIdsBySpecsCriteriaAsync(SearchDto search);
        Task<List<PortalItemSpec>> GetByPortalItemIdAsync(int portalItemId);
        Task<PortalItemSpec?> GetByIdAsync(int id);
        Task UpdateAsync(PortalItemSpec portalItemSpec);
        Task<PortalItemSpec> AddAsync(PortalItemSpec portalItemSpec);
        Task DeleteAsync(PortalItemSpec portalItemSpec);
        bool Exists(int id);
    }
}
