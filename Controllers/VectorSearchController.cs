using Microsoft.AspNetCore.Mvc;

namespace Portal.Controllers
{
    public class VectorSearchController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
