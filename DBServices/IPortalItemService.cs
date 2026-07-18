using Erp.Domain.Models;
using Portal.Models;

namespace Portal.DBServices
{
    public interface IPortalItemService
    {
        Task<List<PortalItem>> GetAllAsync();
        Task<List<ItemDto>> SearchItemsAsync(SearchDto search);
        Task<List<ItemDto>> GetItemsByCategory(string category, string? type);
        Task<List<ItemDto>> GetByIdListAsync(List<int> IdList);
        Task<PortalItem?> GetByIdAsync(int id);
        Task UpdateAsync(PortalItem portalItem);
        Task<PortalItem> AddAsync(PortalItem portalItem);
        Task DeleteAsync(PortalItem portalItem);
        bool Exists(int id);
    }
}
