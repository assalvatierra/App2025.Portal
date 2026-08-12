using Microsoft.SemanticKernel;
using Portal.DBServices;
using System.ComponentModel;
using System.Text.Json;

namespace Portal.Services.SemanticKernelPlugins
{
    public class ProductsPlugin
    {
        private readonly IPortalItemService _portalItemService;
        private readonly IPortalCategoryServices _portalCategoryService;


        public ProductsPlugin(
            IPortalItemService portalItemService,
            IPortalCategoryServices portalCategoryService
            )
        {
            _portalItemService = portalItemService;
            _portalCategoryService = portalCategoryService;
        }

        [KernelFunction("get_product_categories")]
        [Description("Gets a list of available Product categories")]
        public async Task<string> GetProductCategories()
        {
            // Implementation to retrieve product categories
            var categories = await _portalCategoryService.GetAllByStatusAsync("Active");

            // fetch categories for Product
            var productCategories = categories
                .Where(c => c.PortalCategory.CategoryType == "Product")
                .Select(c => new
                {
                    CategoryName = c.PortalCategory.Name,
                    CategoryType = c.PortalCategory.CategoryType,
                    Title = c.Title,
                    Description = c.Description,
                    SeoTitle = c.SeoTitle,
                    SeoDescription = c.SeoDescription,
                    PageUrl = c.PageUrl
                });

            return JsonSerializer.Serialize(productCategories);
        }

        [KernelFunction("get_products")]
        [Description(
            "Gets a list of available products along with prices and passenger and cargo/luggage capacities." +
            "Use optional parameter (CategoryName) to filter the results, by Category or Type, if needed."
            )]
        public async Task<string> GetProducts(string? CategoryName)
        {
            string sCat = CategoryName ?? "";
            try
            {
                // Implementation to retrieve products
                var items = (await _portalItemService.GetItemsByCategory(sCat, "Product"));

                var products = items.Select(p=>
                        new { 
                            Name = p.PortalItem.Name ,
                            Description = p.PortalItem.Description,
                            Title = p.Title,
                            LongDescription = p.Description,
                            Specification = p.PortalItem.PortalItemSpecs.Select(s => s.JsonData),
                            PriceConfiguration = p.PortalItem.PortalItemPrices.Select(s => 
                            new {
                                Price = s.BasePrice,
                                Currency = s.BaseCurrency,
                                Unit = s.BaseUnit,
                                ValidFrom = s.ValidFrom,
                                ValidTo = s.ValidTo
                            })
                        }   
                    );
                string sdata = JsonSerializer.Serialize(products);
                return sdata;
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return $"Error retrieving products: {ex.Message}";
            }
        }


    }
}
