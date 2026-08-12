using Microsoft.SemanticKernel;
using Portal.DBServices;
using System.ComponentModel;
using System.Text.Json;

namespace Portal.Services.SemanticKernelPlugins
{
    public class ContentsPlugin
    {
        private readonly IPortalContentService _portalContentService;
        public ContentsPlugin(
            IPortalContentService portalContentService
            )
        {
            _portalContentService = portalContentService;
        }

        [KernelFunction("get_services")]
        [Description("Gets a list of available Services")]
        public async Task<string> GetServices()
        {
            var results = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "Services" }, null);
            return JsonSerializer.Serialize(results);
        }

        [KernelFunction("get_articles_and_faqs")]
        [Description("Gets a list of available Articles and FAQs")]
        public async Task<string> GetArticlesAndFaqs()
        {
            var results = await _portalContentService.GetContentsByCategoryAsync(new List<string> { "Articles", "Faq" }, null);
            return JsonSerializer.Serialize(results);
        }

    }
}

