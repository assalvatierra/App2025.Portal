using Erp.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Portal.DBServices;

namespace Portal.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ICtaBoxService _ctaBoxService;
        private readonly IPortalContentDataService _portalContentDataService;

        public ArticlesController(ICtaBoxService ctaBoxService, 
            IPortalContentDataService portalContentDataService)
        {
            _ctaBoxService = ctaBoxService;
            _portalContentDataService = portalContentDataService;
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

        [HttpGet]
        [Route("Contents/{contentName}")]
        public async Task<IActionResult> Contents(string contentName)
        {
            string viewName = "HtmlContent";
            if (string.IsNullOrWhiteSpace(contentName))
            {
                return BadRequest("Content name is required.");
            }

            PortalContentData contentData = await _portalContentDataService.GetByContentNameAsync(contentName);
            if (contentData != null)
            {
                ViewBag.ContentData = contentData.DataValue;
            }

            await SetCtaBoxViewBag();
            return View("Contents/" + viewName);
        }


        [HttpGet]
        [Route("Items/{itemName}")]
        public async Task<IActionResult> Items(string itemName)
        {
            string viewName = "HtmlContent";
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return BadRequest("Item name is required.");
            }

            var result = await _portalContentDataService.GetByItemNameAsync(itemName);
            var contentData = result.Item1;
            var item = result.Item2;

            if (contentData != null)
            {
                ViewBag.ContentData = contentData.DataValue;
            }
            if(item != null)
            {
                ViewBag.ItemId = item.Id;
            }

            await SetCtaBoxViewBag();
            return View("Contents/" + viewName);
        }


        private async Task SetCtaBoxViewBag()
        {
            var ctaBoxInfo = await _ctaBoxService.GetCtaBoxInfoAsync();
            ViewBag.CtaBox = ctaBoxInfo;
        }
    }
}
