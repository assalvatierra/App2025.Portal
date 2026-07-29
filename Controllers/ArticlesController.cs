using Microsoft.AspNetCore.Mvc;
using Portal.DBServices;

namespace Portal.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ICtaBoxService _ctaBoxService;

        public ArticlesController(ICtaBoxService ctaBoxService)
        {
            _ctaBoxService = ctaBoxService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return BadRequest("View name is required.");
            }

            await SetCtaBoxViewBag();
            return View(viewName);
        }

        [HttpGet]
        [Route("Articles/{viewName}")]
        public async Task<IActionResult> Articles(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return BadRequest("View name is required.");
            }

            await SetCtaBoxViewBag();
            return View(viewName);
        }

        [HttpGet]
        [Route("Services/{viewName}")]
        public async Task<IActionResult> Services(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return BadRequest("View name is required.");
            }

            await SetCtaBoxViewBag();
            return View("Services/" + viewName);
        }

        private async Task SetCtaBoxViewBag()
        {
            var ctaBoxInfo = await _ctaBoxService.GetCtaBoxInfoAsync();
            ViewBag.CtaBox = ctaBoxInfo;
        }
    }
}
