using Erp.Domain.Models;

namespace Portal.DBLayer
{
    public interface IPortalCategoryDbLayer
    {
        Task<List<PortalCategory>> GetAllAsync();
        Task<List<PortalCategory>> GetAllByStatusAsync(string? status);

        Task<PortalCategory?> GetByIdAsync(int id);
        Task UpdateAsync(PortalCategory portalCategory);
        Task<PortalCategory> AddAsync(PortalCategory portalCategory);
        Task DeleteAsync(PortalCategory portalCategory);
        bool Exists(int id);
    }
}
