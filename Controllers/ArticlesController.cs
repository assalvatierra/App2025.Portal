using Microsoft.AspNetCore.Mvc;

namespace Portal.Controllers
{
    public class ArticlesController : Controller
    {
        [HttpGet]
        public IActionResult Index(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return BadRequest("View name is required.");
            }

            return View(viewName);
        }

        [HttpGet]
        [Route("Articles/{viewName}")]
        public IActionResult Articles(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return BadRequest("View name is required.");
            }

            return View(viewName);
        }

        [HttpGet]
        [Route("Services/{viewName}")]
        public IActionResult Services(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return BadRequest("View name is required.");
            }

            return View(viewName);
        }

    }
}
