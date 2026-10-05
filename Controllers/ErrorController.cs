using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Portal.Models;

namespace Portal.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Displays the rate limit exceeded page.
        /// </summary>
        [Route("Error/RateLimitReached")]
        public IActionResult RateLimitReached(string returnUrl = null, int requestsPerWindow = 10, int windowSeconds = 60)
        {
            try
            {
                _logger.LogInformation(
                    "User hit rate limit - ReturnUrl: {ReturnUrl}, Limit: {RequestsPerWindow} requests per {WindowSeconds}s, RemoteIP: {RemoteIP}",
                    returnUrl, requestsPerWindow, windowSeconds, HttpContext.Connection.RemoteIpAddress);

                var model = new RateLimitViewModel
                {
                    ReturnUrl = returnUrl,
                    RequestsPerWindow = requestsPerWindow,
                    WindowSeconds = windowSeconds,
                    ResetTime = DateTime.Now.AddSeconds(windowSeconds)
                };

                _logger.LogDebug("RateLimitReached model created - Reset time: {ResetTime}, Model: {@Model}", model.ResetTime, model);

                // Return the simple view without layout to avoid layout rendering issues
                return View("RateLimitReachedSimple", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in RateLimitReached action");
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}", "text/plain");
            }
        }

        /// <summary>
        /// Generic error page (can be extended for other error codes).
        /// </summary>
        [Route("Error/{id?}")]
        public IActionResult Error(int id = 0)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier
            };

            return View(errorViewModel);
        }
    }
}
