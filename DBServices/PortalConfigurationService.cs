using Erp.Domain.Models;
using Portal.DBLayer;
using Microsoft.Extensions.Configuration;

namespace Portal.DBServices
{
    public class PortalConfigurationService: IPortalConfigurationService
    {
        // appsettings attribute name for syscode
        const string SYS_CODE_NAME = "Syscode";
        const string SYS_CODE_DEFAULT = "PORTAL";
        //configuration name for global portal settings
        const string PORTAL_MAIN_CONFIG = "Portal";

        // admin email from portal global configuration
        const string ADMIN_EMAIL = "AdministratorEmail";

        private readonly IPortalConfigurationDbLayer _portalConfigurationDbLayer;
        private readonly string _portalCode;

        public PortalConfigurationService(IPortalConfigurationDbLayer portalConfigurationDbLayer, IConfiguration configuration)
        {
            _portalConfigurationDbLayer = portalConfigurationDbLayer;
            _portalCode = configuration.GetValue<string>(SYS_CODE_NAME) ?? SYS_CODE_DEFAULT; //default code during development
        }
        public async Task<List<PortalConfiguration>?> GetPortalConfigurationByNameAsync(string name)
        {
            // Call the database layer to get portal configurations by name
            return await _portalConfigurationDbLayer.GetPortalConfigurationByNameAndCodeAsync(name, _portalCode);
        }

        public Task<string> GetPortalAdminEmail()
        {
            var config = this.GetPortalConfigurationByNameAsync(PORTAL_MAIN_CONFIG).Result;
            if (config != null && config.Count > 0)
            {
                string? jsonsetting = config.First().Settings;
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonsetting ?? "");
                if (settings != null && settings.ContainsKey(ADMIN_EMAIL))
                {
                    return Task.FromResult(settings[ADMIN_EMAIL]);
                }
            }
            return Task.FromResult(string.Empty);
        }
    }
}
