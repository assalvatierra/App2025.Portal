using System.Text.Json;
using Portal.DBServices;

namespace Portal.Services;

public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private readonly IPortalConfigurationService _portalConfigurationService;
    private const string NAVIGATION_CONFIG_NAME = "Navigation";

    public NavigationService(ILogger<NavigationService> logger, IPortalConfigurationService portalConfigurationService)
    {
        _logger = logger;
        _portalConfigurationService = portalConfigurationService;
    }

    public async Task<List<NavigationItem>> GetNavItemsAsync()
    {
        try
        {
            // Get navigation configuration from database
            var configs = await _portalConfigurationService.GetPortalConfigurationByNameAsync(NAVIGATION_CONFIG_NAME);

            if (configs != null && configs.Count > 0)
            {
                var navConfig = configs.First();

                if (!string.IsNullOrEmpty(navConfig.Settings))
                {
                    // Deserialize JSON string to List<NavigationItem>
                    // Use PropertyNameCaseInsensitive to match lowercase JSON keys with PascalCase C# properties
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var navItems = JsonSerializer.Deserialize<List<NavigationItem>>(navConfig.Settings, options);
                    if (navItems != null && navItems.Any())
                    {
                        // Filter out disabled items (IsEnabled != 1)
                        var enabledItems = navItems.Where(item => item.IsEnabled == true).ToList();
                        if (enabledItems.Any())
                        {
                            _logger.LogInformation("Navigation items loaded successfully from database. Total: {Total}, Enabled: {Enabled}", navItems.Count, enabledItems.Count);
                            return enabledItems;
                        }
                    }
                }
            }

            _logger.LogWarning("No navigation configuration found in database with name '{ConfigName}'", NAVIGATION_CONFIG_NAME);
            return GetDefaultNavItems();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching navigation items from database");
            return GetDefaultNavItems();
        }
    }

    /// <summary>
    /// Returns default navigation items as fallback if database configuration is not available
    /// </summary>
    private List<NavigationItem> GetDefaultNavItems()
    {
        _logger.LogInformation("Using default navigation items as fallback");
        return new List<NavigationItem>
        {
            new NavigationItem { Label = "Home", ActionName = "Index", ControllerName = "Home" },
            //new NavigationItem { Label = "Our Fleet", ActionName = "OurProducts", ControllerName = "Home" },
            //new NavigationItem { Label = "Car Rental Services", ActionName = "OurServices", ControllerName = "Home" },
            //new NavigationItem { Label = "Car Rental Blogs", ActionName = "Articles", ControllerName = "Home" },
            //new NavigationItem { Label = "FAQs", ActionName = "FaqList", ControllerName = "Home" },
            new NavigationItem { Label = "Privacy", ActionName = "Privacy", ControllerName = "Home" },
            //new NavigationItem { Label = "Ask AI", ActionName = "AskAI", ControllerName = "Home" },
            //new NavigationItem { Label = "VectorDB", ActionName = "VectorDB", ControllerName = "Home" }
            new NavigationItem { Label = "Configuration", ActionName = "Privacy", ControllerName = "Home" }
        };
    }
}
