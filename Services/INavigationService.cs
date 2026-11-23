namespace Portal.Services;

public class NavigationItem
{
    public string Label { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public interface INavigationService
{
    Task<List<NavigationItem>> GetNavItemsAsync();
}
