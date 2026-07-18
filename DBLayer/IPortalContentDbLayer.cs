using Erp.Domain.Models;

namespace Portal.DBLayer
{
    public interface IPortalContentDbLayer
    {
        Task<List<PortalContent>> GetAllAsync();
        Task<List<PortalContent>> GetContentsByCategoryAsync(string category, string? type);
        Task<PortalContent?> GetByIdAsync(int id);
        Task UpdateAsync(PortalContent portalContent);
        Task<PortalContent> AddAsync(PortalContent portalContent);
        Task DeleteAsync(PortalContent portalContent);
        bool Exists(int id);
    }
}
