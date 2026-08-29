using Erp.Domain.Models;

namespace Portal.DBLayer
{
    public interface IPortalContentDataDbLayer
    {
        Task<List<PortalContentData>> GetAllAsync();
        Task<List<PortalContentData>> GetByTypeAsync(string dataType);
        Task<PortalContentData?> GetByIdAsync(int id);
        Task UpdateAsync(PortalContentData portalContentData);
        Task<PortalContentData> AddAsync(PortalContentData portalContentData);
        Task DeleteAsync(PortalContentData portalContentData);
        bool Exists(int id);
    }
}
