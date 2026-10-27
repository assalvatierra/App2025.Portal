using App2025.Portal.Models;
using Erp.Domain.Models;
using Portal.DBLayer;

namespace Portal.DBServices
{
    public class CtaBoxService : ICtaBoxService
    {
        // Portal system code (field:SysCode)
        private const string PORTAL_CODE = "PORTAL";

        // Configuration name for CTA Box settings
        private const string CTA_BOX_CONFIG_NAME = "CtaBox";

        private readonly IPortalConfigurationDbLayer _portalConfigurationDbLayer;
        private readonly ILogger<CtaBoxService> _logger;

        public CtaBoxService(IPortalConfigurationDbLayer portalConfigurationDbLayer, ILogger<CtaBoxService> logger)
        {
            _portalConfigurationDbLayer = portalConfigurationDbLayer;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves CTA Box configuration from the database and deserializes it
        /// </summary>
        public async Task<CtaBoxViewModel> GetCtaBoxInfoAsync()
        {
            try
            {
                // Retrieve portal configuration by name and system code
                var configs = await _portalConfigurationDbLayer.GetPortalConfigurationByNameAndCodeAsync(
                    CTA_BOX_CONFIG_NAME,
                    PORTAL_CODE
                );

                if (configs == null || configs.Count == 0)
                {
                    return new CtaBoxViewModel();
                }

                // Get the first configuration with matching name
                var config = configs.FirstOrDefault();
                if (config == null || string.IsNullOrEmpty(config.Settings))
                {
                    return new CtaBoxViewModel();
                }

                // Deserialize JSON string to CtaBoxViewModel
                var ctaBoxModel = CtaBoxJsonHelper.FromJson(config.Settings);
                return ctaBoxModel;
            }
            catch (Exception ex)
            {
                // Log exception if needed
                _logger.LogError($"Error retrieving CTA Box configuration: {ex.Message}");
                return new CtaBoxViewModel();
            }
        }
    }
}
