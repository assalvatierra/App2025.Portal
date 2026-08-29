using Erp.Domain.Models;

namespace Portal.DBServices
{
    public interface IPortalContentDataService
    {
        Task<List<PortalContentData>> GetAllAsync();
        Task<List<PortalContentData>> GetByTypeAsync(string dataType);
        Task<PortalContentData?> GetByIdAsync(int id);
        Task UpdateAsync(PortalContentData portalContentData);
        Task<PortalContentData> AddAsync(PortalContentData portalContentData);
        Task DeleteAsync(PortalContentData portalContentData);
    }
}
