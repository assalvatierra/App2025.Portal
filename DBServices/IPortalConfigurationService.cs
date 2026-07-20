using Erp.Domain.Models;

namespace Portal.DBServices
{
    public interface IPortalConfigurationService
    {
        public Task<List<PortalConfiguration>> GetPortalConfigurationByNameAsync(string name);
        public Task<string> GetPortalAdminEmail();

    }
}
