using Erp.Domain.Models;
using Portal.DBLayer;

namespace Portal.DBServices
{
    public class PortalContentDataService : IPortalContentDataService
    {
        private readonly IPortalContentDataDbLayer _portalContentDataDbLayer;

        public PortalContentDataService(IPortalContentDataDbLayer portalContentDataDbLayer)
        {
            _portalContentDataDbLayer = portalContentDataDbLayer;
        }

        public async Task<List<PortalContentData>> GetAllAsync()
        {
            return await _portalContentDataDbLayer.GetAllAsync();
        }

        public async Task<List<PortalContentData>> GetByTypeAsync(string dataType)
        {
            return await _portalContentDataDbLayer.GetByTypeAsync(dataType);
        }

        public async Task<PortalContentData?> GetByIdAsync(int id)
        {
            return await _portalContentDataDbLayer.GetByIdAsync(id);
        }

        public async Task UpdateAsync(PortalContentData portalContentData)
        {
            await _portalContentDataDbLayer.UpdateAsync(portalContentData);
        }

        public async Task<PortalContentData> AddAsync(PortalContentData portalContentData)
        {
            return await _portalContentDataDbLayer.AddAsync(portalContentData);
        }

        public async Task DeleteAsync(PortalContentData portalContentData)
        {
            await _portalContentDataDbLayer.DeleteAsync(portalContentData);
        }
    }
}
