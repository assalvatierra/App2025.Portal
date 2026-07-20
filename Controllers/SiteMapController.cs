using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Globalization;
using System.Text;
using System.Xml;

namespace Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteMapController : ControllerBase
    {
        public class SitemapNode
        {
            public SitemapFrequency? Frequency { get; set; }
            public DateTime? LastModified { get; set; }
            public double? Priority { get; set; }
            public string Url { get; set; }
        }

        public enum SitemapFrequency
        {
            Never,
            Yearly,
            Monthly,
            Weekly,
            Daily,
            Hourly,
            Always
        }

        [HttpGet("/sitemap.xml")]
        public IActionResult GetSitemap()
        {
            // Get the base url of your website
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // using var dbContext = new BlogContext();
            // Load pages to include in the sitemap from your database or model
            // var pages = dbContext.Posts.OrderByDescending(x => x.DateCreated).ToList();

            IReadOnlyCollection<SitemapNode> pages = new List<SitemapNode>();

            // Manually create the  root and  entries using XmlWriter
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };

            using var stream = new MemoryStream();
            using var writer = XmlWriter.Create(stream, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // First we add the website home page as a manual entry, you can repeat this
            // to add other static pages. If you have a last updated date you can set this here

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", baseUrl);
            writer.WriteElementString("lastmod", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            writer.WriteElementString("changefreq", SitemapFrequency.Weekly.ToString().ToLower());
            writer.WriteElementString("priority", "1");
            writer.WriteEndElement(); // 


            pages = this.GetSitemapNodes(baseUrl);

            // Add each page or post to the list
            foreach (var page in pages)
            {
                // format the date
                var date = DateTime.Parse(page.LastModified.GetValueOrDefault().ToString(CultureInfo.CurrentCulture)).ToString("yyyy-MM-ddTHH:mm:sszzz");
                writer.WriteStartElement("url");
                // write the url, you can change this to match your website url structure
                writer.WriteElementString("loc", page.Url);
                writer.WriteElementString("lastmod", page.LastModified.GetValueOrDefault().ToString());
                writer.WriteElementString("changefreq", page.Frequency.GetValueOrDefault().ToString().ToLower());
                writer.WriteElementString("priority", page.Priority.GetValueOrDefault().ToString());

                writer.WriteEndElement(); // 
            }

            writer.WriteEndElement(); // 
            writer.WriteEndDocument();
            writer.Flush();

            stream.Position = 0;
            var xml = Encoding.UTF8.GetString(stream.ToArray());

            return Content(xml, "application/xml", Encoding.UTF8);
        }


        public List<string> GetItemPages()
        {
            List<string> items = new List<string>();
            items.Add("/shop/");
            items.Add("/about-car-rental-davao/");
            items.Add("/contact-us/");

            items.Add("/Home/OurProducts");
            items.Add("/Home/OurServices");
            items.Add("/Home/Articles");


            return items;
        }

        public IReadOnlyCollection<SitemapNode> GetSitemapNodes(string _website)
        {
            List<SitemapNode> nodes = new List<SitemapNode>();

            //root items
            List<string> itemroot = this.GetItemPages();
            foreach (var item in itemroot)
            {
                nodes.Add(
                    new SitemapNode()
                    {
                        Url = string.Format(_website + "{0}", item),
                        LastModified = System.DateTime.Now,
                        Frequency = SitemapFrequency.Weekly,
                        Priority = 1
                    });
            }
            return nodes;
        }


        }
    }
