using Erp.Domain.Models;
using Portal.DBLayer;

namespace Portal.DBServices
{
    public class PortalConfigurationService: IPortalConfigurationService
    {
        const string PORTAL_CODE = "PORTAL";
        private readonly IPortalConfigurationDbLayer _portalConfigurationDbLayer;
        public PortalConfigurationService(IPortalConfigurationDbLayer portalConfigurationDbLayer)
        {
            _portalConfigurationDbLayer = portalConfigurationDbLayer;
        }
        public async Task<List<PortalConfiguration>> GetPortalConfigurationByNameAsync(string name)
        {
            // Call the database layer to get portal configurations by name
            return await _portalConfigurationDbLayer.GetPortalConfigurationByNameAndCodeAsync(name, PORTAL_CODE);
        }
    }
}
