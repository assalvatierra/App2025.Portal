using Erp.Domain.Models;

namespace Portal.DBLayer
{
    public interface IPortalConfigurationDbLayer
    {
        public Task<List<PortalConfiguration>> GetPortalConfigurationByNameAndCodeAsync(string name, string code);
    }
}
