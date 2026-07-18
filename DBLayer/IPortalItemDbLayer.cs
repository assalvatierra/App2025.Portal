using Erp.Domain.Models;

namespace Portal.DBLayer
{
    public interface IPortalItemDbLayer
    {
        Task<List<PortalItem>> GetAllAsync();
        Task<List<PortalItem>> SearchItemsAsync(string searchTerm, List<int> itemIds);
        Task<List<PortalItem>> GetItemsByCategoryAsync(string category, string? type);
        Task<List<PortalItem>> GetByIdListAsync(List<int> IdList);
        Task<PortalItem?> GetByIdAsync(int id);
        Task UpdateAsync(PortalItem portalItem);
        Task<PortalItem> AddAsync(PortalItem portalItem);
        Task DeleteAsync(PortalItem portalItem);
        bool Exists(int id);
    }
}
