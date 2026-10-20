using Microsoft.AspNetCore.Mvc;
using Portal.Controllers.Attributes;
using Portal.DBServices;
using Portal.Helpers;
using Portal.Models;
using Portal.Services.VectorSearch;
using Portal.Services.VectorSearch.Models;
using System.Diagnostics;
using Erp.Domain.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Portal.Controllers
{
    public class HomeConfiguration
    {
        public List<HomeSection> Sections { get; set; }
    }
    public class HomeSection
    {
        public string Section { get; set; }
        public string Title { get; set; }
        public string intro { get; set; }
        public bool Enabled { get; set; }
        public int itemsToShow { get; set; } = 0;
    }

    public class HomeController : Controller
    {
        private readonly IPortalItemService _portalItemService;
        private readonly IPortalCategoryServices _portalCategoryService;
        private readonly IPortalContentService _portalContentService;
        private readonly IDistributedCache _cache;
        private readonly IVectorSearchService _vectorSearchService;
        private const string VectorDbCacheKey = "VectorDbScheduleSettings";
        private readonly IPortalConfigurationService _portalConfigurationService;

        public HomeController(
            IPortalItemService portalItemService, 
            IPortalCategoryServices portalCategoryService,
            IPortalContentService portalContentService,
            IDistributedCache cache,
            IVectorSearchService vectorSearchService,
            IPortalConfigurationService portalConfigurationService)
        {
            _portalItemService = portalItemService;
            _portalCategoryService = portalCategoryService;
            _portalContentService = portalContentService;
            _cache = cache;
            _vectorSearchService = vectorSearchService;
            _portalConfigurationService = portalConfigurationService;
        }

        public async Task<IActionResult> Index()
        {
            var hpc = await _portalConfigurationService.GetPortalConfigurationByNameAsync("HomePage");
            PortalConfiguration pc = hpc.FirstOrDefault();
            if (pc != null && !string.IsNullOrEmpty(pc.Settings))
            {
                var homeconfig = System.Text.Json.JsonSerializer.Deserialize<HomeConfiguration>(pc.Settings);
                ViewBag.HomeSections = homeconfig.Sections.Where(s => s.Enabled).ToList();

                ViewBag.FeaturedArticlesConfig = homeconfig.Sections.Where(s => s.Section == "FeaturedArticles").FirstOrDefault();
            }
            else
            {
                ViewBag.HomeSections = new List<HomeSection>();
            }

            var categories = await _portalCategoryService.GetAllByStatusAsync("Active");

            // fetch categories for Product
            ViewBag.ProductCategories = categories
                .Where(c => c.PortalCategory.CategoryType == "Product");

            ViewBag.ServiceCategories = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "Services" }, "Service");
            ViewBag.WhyUs = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "WhyUs" }, "WhyUs");
            ViewBag.Faq = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "Faq" }, "Faq");
            ViewBag.FeaturedArticles = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "FeaturedBlog" }, "FeaturedBlog");

            return View();
        }

        [HttpGet]
        [RateLimit]
        public async Task<IActionResult> AskAI()
        {
            return View();
        }

        [HttpGet]
        [RateLimit]
        public IActionResult VectorDB()
        {
            // Retrieve saved schedule settings from distributed cache
            var scheduleSettings = GetScheduleSettingsFromCache();

            // Map to DTO for view
            var viewDto = new VectorDbScheduleViewDto();

            if (scheduleSettings != null)
            {
                viewDto.HasSchedule = true;
                viewDto.NextScheduleFormatted = scheduleSettings.NextSchedule.ToString("yyyy-MM-dd HH:mm:ss");
                viewDto.NextScheduleDateTime = scheduleSettings.NextSchedule;
                viewDto.IntervalDisplay = $"{scheduleSettings.IntervalValue} {scheduleSettings.IntervalUnit}";
                viewDto.IntervalValue = scheduleSettings.IntervalValue;
                viewDto.IntervalUnit = scheduleSettings.IntervalUnit;

                if (scheduleSettings.LastUpdateTriggered.HasValue)
                {
                    viewDto.LastUpdateTriggeredFormatted = scheduleSettings.LastUpdateTriggered.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else
            {
                viewDto.HasSchedule = false;
            }

            return View(viewDto);
        }

        [HttpPost]
        [Route("Home/SetVectorDbSchedule")]
        public IActionResult SetVectorDbSchedule([FromBody] VectorDbScheduleDto? model)
        {
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Invalid request data" }) { StatusCode = 400 };
            }

            // Validate schedule data
            if (string.IsNullOrWhiteSpace(model.IntervalUnit) || 
                !new[] { "Minutes", "Hours", "Days" }.Contains(model.IntervalUnit))
            {
                return new JsonResult(new { success = false, message = "Invalid interval unit. Must be 'Minutes', 'Hours', or 'Days'." }) { StatusCode = 400 };
            }

            if (model.IntervalValue < 1)
            {
                return new JsonResult(new { success = false, message = "Interval value must be at least 1" }) { StatusCode = 400 };
            }

            try
            {
                // Create schedule settings from DTO
                var settings = new VectorDbScheduleSettings
                {
                    NextSchedule = model.NextSchedule ?? DateTime.UtcNow,
                    IntervalValue = model.IntervalValue,
                    IntervalUnit = model.IntervalUnit,
                    LastModified = DateTime.UtcNow
                };

                // Save to distributed cache only
                SaveScheduleSettingsToCache(settings);

                return new JsonResult(new 
                { 
                    success = true, 
                    message = $"Schedule set successfully. Next update scheduled for {settings.NextSchedule:yyyy-MM-dd HH:mm:ss} UTC",
                    data = new
                    {
                        nextSchedule = settings.NextSchedule,
                        intervalValue = settings.IntervalValue,
                        intervalUnit = settings.IntervalUnit
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error setting schedule: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        [HttpPost]
        [Route("Home/UpdateVectorDbNow")]
        public async Task<IActionResult> UpdateVectorDbNow()
        {
            try
            {
                // Get current schedule settings from cache
                var settings = GetScheduleSettingsFromCache();

                if (settings != null)
                {
                    settings.LastUpdateTriggered = DateTime.UtcNow;

                    // Calculate next schedule based on interval
                    settings.NextSchedule = settings.IntervalUnit.ToLower() switch
                    {
                        "minutes" => DateTime.UtcNow.AddMinutes(settings.IntervalValue),
                        "hours" => DateTime.UtcNow.AddHours(settings.IntervalValue),
                        "days" => DateTime.UtcNow.AddDays(settings.IntervalValue),
                        _ => DateTime.UtcNow.AddHours(1)
                    };

                    // Save updated settings back to cache
                    SaveScheduleSettingsToCache(settings);

                    // Upsert vector database collections and sample data
                    await _vectorSearchService.CreateCollection("cars", 1536);
                }

                return new JsonResult(new 
                { 
                    success = true, 
                    message = "Update triggered successfully. Vector database collections and indexes refreshed.",
                    data = new
                    {
                        lastUpdateTriggered = settings?.LastUpdateTriggered,
                        nextSchedule = settings?.NextSchedule
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error triggering update: {ex.Message}" }) { StatusCode = 500 };
            }
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
        [RateLimit]
        public async Task<IActionResult> SearchByVector([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    ViewBag.PageTitle = "Vector Search Results";
                    ViewBag.PageMessage = "Please provide a search query.";
                    ViewBag.SearchTerm = query;
                    var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
                    ViewBag.CompareList = compareList;
                    ViewBag.cardMode = "List";
                    return View("ItemList", new List<ItemDto>());
                }

                // Create vector search request
                var vectorSearchRequest = new VectorSearchRequest
                {
                    CollectionName = "cars",
                    Query = $"{query} cars",
                    //ScoreThreshold = 0.65f,
                    Limit = 5
                };

                // Search using vector database
                //var vectorResults = await _vectorSearchService.SearchByVector(vectorSearchRequest);
                var vectorResults = await _vectorSearchService.SearchByKeyword(vectorSearchRequest);


                if (vectorResults == null || vectorResults.Count == 0)
                {
                    ViewBag.PageTitle = "Vector Search Results";
                    ViewBag.PageMessage = $"No results found for: {query}";
                    ViewBag.SearchTerm = query;
                    var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
                    ViewBag.CompareList = compareList;
                    ViewBag.cardMode = "List";
                    return View("ItemList", new List<ItemDto>());
                }

                // Extract item IDs from vector search results
                var itemIds = new List<int>();
                foreach (var result in vectorResults)
                {
                    if (result.Payload != null && result.Payload.TryGetValue("id", out var idObj))
                    {
                        if (int.TryParse(idObj?.ToString(), out var itemId))
                        {
                            itemIds.Add(itemId);
                        }
                    }
                }

                // Fetch full item details from database
                List<ItemDto> results = new List<ItemDto>();
                if (itemIds.Count > 0)
                {
                    var portalItems = await _portalItemService.GetByIdListAsync(itemIds);
                    //results = portalItems ?? new List<ItemDto>();
                    foreach(var iId in itemIds)
                    {
                        var item = portalItems.Find(p => p.PortalItem.Id == iId);
                        if (item != null)
                        {
                            results.Add(item);
                        }
                    }
                }

                // Set ViewBag properties
                ViewBag.SearchTerm = query;
                ViewBag.PageTitle = "Search Results";
                ViewBag.PageMessage = $"Related items for: {query}";

                // Pass compare list to view
                var compareListFinal = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
                ViewBag.CompareList = compareListFinal;
                ViewBag.cardMode = "List";

                return View("ItemList", results);
            }
            catch (Exception ex)
            {
                ViewBag.PageTitle = "Search Results";
                ViewBag.PageMessage = $"An error occurred during search: {ex.Message}";
                ViewBag.SearchTerm = query;
                var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
                ViewBag.CompareList = compareList;
                ViewBag.cardMode = "List";
                return View("ItemList", new List<ItemDto>());
            }
        }

        [HttpGet]
        [RateLimit]
        public async Task<IActionResult> ItemsByCategory(string category)
        {
            var results = await _portalItemService.GetItemsByCategory(category,"Product");
            var catResult = await _portalCategoryService.GetByName(category);
            ViewBag.PageTitle = catResult?.SeoTitle ?? $"Items in category: {category}";
            ViewBag.PageDescription = catResult?.SeoDescription ?? $"Items in category: {category}";

            ViewBag.Category = category;
            ViewBag.PageMessage = $"Items in category: {category}";

            // Pass compare list to view
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();
            ViewBag.CompareList = compareList;
            ViewBag.cardMode = "List";

            return View("ItemList", results);
        }

        [HttpGet]
        [RateLimit]
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
        [RateLimit]
        public async Task<IActionResult> BrowseProducts(string? transactionType)
        {
            var results = await _portalItemService.GetItemsByCategory("", "Product");
            ViewBag.PageTitle = "Browse Car Rental Fleet";
            ViewBag.cardMode = "Browse";
            ViewBag.TransactionType = transactionType ?? "Reservation";

            return View("ItemList", results);
        }

        [HttpGet]
        [RateLimit]
        public async Task<IActionResult> OurServices()
        {
            ViewBag.PageTitle = "Car Rental Services";

            var results = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "Services" }, null);

            return View("ContentList", results);
        }

        [HttpGet]
        [RateLimit]
        public async Task<IActionResult> Articles()
        {
            ViewBag.PageTitle = "Car Rental Articles";
            var results = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "Articles" }, null);

            return View("ContentList", results);
        }

        [HttpGet]
        [RateLimit]
        public async Task<IActionResult> FeaturedBlog()
        {
            ViewBag.PageTitle = "Car Rental Featured";
            var results = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "FeaturedBlog" }, "");

            return View("ContentList", results);
        }

        [HttpGet]
        [RateLimit]
        public async Task<IActionResult> FaqList()
        {
            ViewBag.PageTitle = "Car Rental FAQs";
            var results = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "Faq" }, null);

            return View("FaqList", results);
        }

        [HttpGet]
        [RateLimit]
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

        /// <summary>
        /// Retrieves VectorDB schedule settings from distributed cache.
        /// </summary>
        private VectorDbScheduleSettings? GetScheduleSettingsFromCache()
        {
            try
            {
                var cachedBytes = _cache.Get(VectorDbCacheKey);
                if (cachedBytes == null)
                {
                    return null;
                }

                using (var ms = new System.IO.MemoryStream(cachedBytes))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<VectorDbScheduleSettings>(ms);
                }
            }
            catch (Exception ex)
            {
                // Log error and return null if cache retrieval fails
                return null;
            }
        }

        /// <summary>
        /// Saves VectorDB schedule settings to distributed cache.
        /// </summary>
        private void SaveScheduleSettingsToCache(VectorDbScheduleSettings settings)
        {
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
                };
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var writer = new System.IO.StreamWriter(ms))
                    {
                        System.Text.Json.JsonSerializer.Serialize(writer.BaseStream, settings);
                        writer.Flush();
                        _cache.Set(VectorDbCacheKey, ms.ToArray(), cacheOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid interrupting the flow
            }
        }

    }

    // DTO used by VectorDB scheduling endpoints
    public class VectorDbScheduleDto
    {
        public DateTime? NextSchedule { get; set; }
        public int IntervalValue { get; set; }
        public string? IntervalUnit { get; set; }
    }
}

