using Erp.Domain.Models;
using Portal.DBLayer;

namespace Portal.DBServices
{
    public class PortalConfigurationService: IPortalConfigurationService
    {
        //portal configuration system code (field:SysCode). Can vary by business type or by client.
        const string PORTAL_CODE = "PORTAL"; //default code during development

        //configuration name for global portal settings
        const string PORTAL_MAIN_CONFIG = "Portal";

        // admin email from portal global configuration
        const string ADMIN_EMAIL = "AdministratorEmail";

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

        public Task<string> GetPortalAdminEmail()
        {
            var config = this.GetPortalConfigurationByNameAsync(PORTAL_MAIN_CONFIG).Result;
            if (config != null && config.Count > 0)
            {
                string jsonsetting = config.First().Settings;
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonsetting);
                if (settings != null && settings.ContainsKey(ADMIN_EMAIL))
                {
                    return Task.FromResult(settings[ADMIN_EMAIL]);
                }
            }
            return Task.FromResult(string.Empty);
        }
    }
}
