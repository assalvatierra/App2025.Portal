using Microsoft.AspNetCore.Mvc;
using Portal.Services;

namespace Portal.ViewComponents;

public class NavigationViewComponent : ViewComponent
{
    private readonly INavigationService _navigationService;

    public NavigationViewComponent(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var navItems = await _navigationService.GetNavItemsAsync();
        return View(navItems);
    }
}
