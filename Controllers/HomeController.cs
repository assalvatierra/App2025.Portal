using Microsoft.AspNetCore.Mvc;
using Portal.DBServices;
using Portal.Models;
using Portal.Helpers;
using System.Diagnostics;

namespace Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPortalItemService _portalItemService;
        private readonly IPortalCategoryServices _portalCategoryService;
        private readonly IPortalContentService _portalContentService;

        public HomeController(
            IPortalItemService portalItemService, 
            IPortalCategoryServices portalCategoryService,
            IPortalContentService portalContentService)
        {
            _portalItemService = portalItemService;
            _portalCategoryService = portalCategoryService;
            _portalContentService = portalContentService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _portalCategoryService.GetAllByStatusAsync("Active");

            // fetch categories for Product
            ViewBag.ProductCategories = categories
                .Where(c => c.PortalCategory.CategoryType == "Product");

            ViewBag.ServiceCategories = await _portalContentService.GetContentsByCategoryAsync("Services", null);
            ViewBag.WhyUs = await _portalContentService.GetContentsByCategoryAsync("WhyUs", null);
            ViewBag.Faq = await _portalContentService.GetContentsByCategoryAsync("Faq", null);
            ViewBag.FeaturedArticles = await _portalContentService.GetContentsByCategoryAsync("FeaturedBlog", null);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchItems([FromQuery] SearchDto search)
        {
            var results = await _portalItemService.SearchItemsAsync(search);
            ViewBag.SearchTerm = search.searchTerm;
            ViewBag.PageTitle = "Car Rental Search Results";
            ViewBag.PageMessage = $"Search results for: {search.searchTerm}";

            // Pass compare list to view
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
            ViewBag.CompareList = compareList;
            ViewBag.cardMode = "List";

            return View("ItemList", results);
        }

        [HttpGet]
        public async Task<IActionResult> ItemsByCategory(string category)
        {
            var results = await _portalItemService.GetItemsByCategory(category,"Product");
            ViewBag.Category = category;
            ViewBag.PageTitle = "Car Rental Items by Category";
            ViewBag.PageMessage = $"Items in category: {category}";

            // Pass compare list to view
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
            ViewBag.CompareList = compareList;
            ViewBag.cardMode = "List";

            return View("ItemList", results);
        }

        [HttpGet]
        public async Task<IActionResult> OurProducts()
        {
            var results = await _portalItemService.GetItemsByCategory("", "Product");
            //ViewBag.Category = "List";
            ViewBag.PageTitle = "Car Rental Fleet";
            //ViewBag.PageMessage = $"Items in category: Product";

            // Pass compare list to view
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
            ViewBag.CompareList = compareList;
            ViewBag.cardMode = "List";

            return View("ItemList", results);
        }

        [HttpGet]
        public async Task<IActionResult> BrowseProducts(string? transactionType)
        {
            var results = await _portalItemService.GetItemsByCategory("", "Product");
            ViewBag.PageTitle = "Browse Car Rental Fleet";
            ViewBag.cardMode = "Browse";
            ViewBag.TransactionType = transactionType ?? "Reservation";

            return View("ItemList", results);
        }

        [HttpGet]
        public async Task<IActionResult> OurServices()
        {
            ViewBag.PageTitle = "Car Rental Services";
            var results = await _portalContentService.GetContentsByCategoryAsync("Services", null);

            return View("ContentList", results);
        }

        [HttpGet]
        public async Task<IActionResult> Articles()
        {
            ViewBag.PageTitle = "Car Rental Articles";
            var results = await _portalContentService.GetContentsByCategoryAsync("Articles", null);

            return View("ContentList", results);
        }

        [HttpGet]
        public async Task<IActionResult> FeaturedBlog()
        {
            ViewBag.PageTitle = "Car Rental Featured";
            var results = await _portalContentService.GetContentsByCategoryAsync("FeaturedBlog", "");

            return View("ContentList", results);
        }

        [HttpGet]
        public async Task<IActionResult> FaqList()
        {
            ViewBag.PageTitle = "Car Rental FAQs";
            var results = await _portalContentService.GetContentsByCategoryAsync("Faq", null);

            return View("FaqList", results);
        }

        [HttpGet]
        public async Task<IActionResult> ItemsToCompare()
        {
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
            var results = await _portalItemService.GetByIdListAsync(compareList);
            ViewBag.Category = "Comparison";
            ViewBag.PageTitle = "Car Rental Items for Comparison";
            ViewBag.PageMessage = "Items selected for comparison";
            ViewBag.CompareList = compareList;
            ViewBag.cardMode = "List";

            return View("ItemList", results);
        }
        [HttpPost]
        public IActionResult AddToCompare(int itemId)
        {
            try
            {
                // Initialize or get the comparison list from session
                var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();

                // Check if item already exists in the list
                if (compareList.Contains(itemId))
                {
                    return Json(new { success = false, message = "Item is already in the comparison list" });
                }

                // Add item to the list
                compareList.Add(itemId);

                // Save back to session
                HttpContext.Session.SetObject("CompareList", compareList);

                return Json(new { 
                    success = true, 
                    message = "Item added to comparison list successfully",
                    count = compareList.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCompareCount()
        {
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
            return Json(new { count = compareList.Count });
        }

        [HttpPost]
        public IActionResult RemoveFromCompare(int itemId)
        {
            try
            {
                // Get the comparison list from session
                var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();

                // Check if item exists in the list
                if (!compareList.Contains(itemId))
                {
                    return Json(new { success = false, message = "Item is not in the comparison list" });
                }

                // Remove item from the list
                compareList.Remove(itemId);

                // Save back to session
                HttpContext.Session.SetObject("CompareList", compareList);

                return Json(new
                {
                    success = true,
                    message = "Item removed from comparison list successfully",
                    count = compareList.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ClearCompareList()
        {
            try
            {
                // Clear the comparison list from session
                HttpContext.Session.SetObject("CompareList", new List<int>());

                return Json(new
                {
                    success = true,
                    message = "Comparison list cleared successfully",
                    count = 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
