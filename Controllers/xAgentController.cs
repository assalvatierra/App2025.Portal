using Microsoft.AspNetCore.Mvc;
using Portal.Services;
using Portal.SemanticKernelModel;

namespace Portal.Controllers
{
    public class xAgentController : Controller
    {
        private readonly ILogger<xAgentController> _logger;
        private readonly ISemanticKernelService _semanticKernelService;

        public xAgentController(ILogger<xAgentController> logger, ISemanticKernelService semanticKernelService)
        {
            _logger = logger;
            _semanticKernelService = semanticKernelService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetResponse([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { response = "Message cannot be empty." });
            }

            try
            {
                var response = await _semanticKernelService.ProcessUserMessageAsync(request.Message, request.History);
                return Json(new { response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                return StatusCode(500, new { response = "An error occurred while processing your request. Please try again." });
            }
        }
    }

}
