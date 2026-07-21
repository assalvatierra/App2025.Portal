using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Portal.DBServices;
using Portal.Models;
using Portal.Services;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteMapController : ControllerBase
    {

        private readonly ISitemapService _sitemapservice;

        public SiteMapController(ISitemapService sitemapservice)
        {
            _sitemapservice = sitemapservice;
        }

        [HttpGet("/sitemap.xml")]
        public IActionResult GetSitemap()
        {
            var xml = _sitemapservice.GetSitemapXml();

            return Content(xml, "application/xml", Encoding.UTF8);
        }


    }
}
