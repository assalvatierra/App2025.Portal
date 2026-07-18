using Erp.Domain.Models;
using Portal.Models;

namespace Portal.DBLayer
{
    public interface IPortalItemSpecDbLayer
    {
        Task<List<PortalItemSpec>> GetAllAsync();
        Task<List<PortalItemSpec>> GetItemISpecsCriteriaAsync(SearchDto search);

        Task<List<PortalItemSpec>> GetByPortalItemIdAsync(int portalItemId);
        Task<PortalItemSpec?> GetByIdAsync(int id);
        Task UpdateAsync(PortalItemSpec portalItemSpec);
        Task<PortalItemSpec> AddAsync(PortalItemSpec portalItemSpec);
        Task DeleteAsync(PortalItemSpec portalItemSpec);
        bool Exists(int id);
    }
}
