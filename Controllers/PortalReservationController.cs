using Erp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portal.DBServices;
using Portal.Models;
using Portal.Services;
using Portal.ViewModels;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace Portal.Controllers
{
    //[Authorize]
    public class PortalReservationController : Controller
    {
        private readonly IPortalConfigurationService _Configuration;
        private readonly IPortalReservationService _service;
        private readonly IPortalItemService _portalItemService;
        private readonly IReservationService _reservationService;
        private readonly ILogger<PortalReservationController> _logger;

        public PortalReservationController(
            IPortalConfigurationService portalConfigurationService,
            IPortalReservationService service, 
            IPortalItemService portalItemService,
            IReservationService reservationService,
            ILogger<PortalReservationController> logger)
        {
            _Configuration = portalConfigurationService;
            _service = service;
            _portalItemService = portalItemService;
            _reservationService = reservationService;
            _logger = logger;
        }

        // GET: PortalReservation/ReservationForm
        [HttpGet]
        public async Task<IActionResult> ReservationForm(int? itemId, string? transactionType)
        {
            string returnUrl = Request.Headers["Referer"].ToString();

            var reservation = new PortalReservation
            {
                PortalItemId = itemId,
                DateReceived = DateTime.Now,
                Status = "New",
                TransactionType = transactionType ?? "Reservation",
                JsonData = "{}"
            };

            var viewModel = new ReservationFormViewModel
            {
                Reservation = reservation
            };

            // Fetch the portal item if itemId is provided
            if (itemId.HasValue)
            {
                var portalItem = await _portalItemService.GetByIdAsync(itemId.Value);
                if (portalItem != null)
                {
                    viewModel.Item = portalItem.MapToDto();
                }
            }

            // Store the return URL for cancellation
            // Check if returnUrl is not coming from PortalReservationController
            if (!string.IsNullOrEmpty(returnUrl) && !returnUrl.Contains("/PortalReservation/"))
            {
                HttpContext.Session.SetString("reservationReturnUrl", returnUrl);
            }

            ViewBag.transactionType = transactionType ?? "Reservation";
            return View(viewModel);
        }

        // POST: PortalReservation/ReservationForm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationForm(ReservationFormViewModel viewModel)
        {
            var reservation = viewModel.Reservation;
            reservation.DateReceived = DateTime.Now;
            reservation.Status = "New";

            if (ModelState.IsValid)
            {
                // JsonData is already populated by JavaScript in the view
                if (string.IsNullOrEmpty(reservation.JsonData))
                {
                    reservation.JsonData = "{}";
                }

                var addedReservation = await _service.AddAsync(reservation);

                if (addedReservation == null)
                {
                    ModelState.AddModelError("", "Failed to add reservation. Please try again.");
                    return View(viewModel);
                }

                //transfer to After otp confirmation
                //await this._reservationService.SendCustomerNotification(addedReservation);

                //this._reservationService.
                TempData["SuccessMessage"] = "Reservation submitted successfully!";
                TempData["TransactionType"] = addedReservation.TransactionType;
                //return RedirectToAction("Success", new { id = addedReservation.Id });
                return RedirectToAction("ConfirmReservationOtp", new { id = addedReservation.Id });
            }

            // Reload the item on validation failure
            if (reservation.PortalItemId.HasValue)
            {
                var portalItem = await _portalItemService.GetByIdAsync(reservation.PortalItemId.Value);
                if (portalItem != null)
                {
                    viewModel.Item = portalItem.MapToDto();
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var reservations = await _service.GetByIdAsync(id);
            if (reservations == null)
            {
                return NotFound();
            }

            ViewBag.Message = "Reservation submitted successfully!";
            var config = await _Configuration.GetPortalConfigurationByNameAsync("Reservation");
            if(config.Any())
            {
                string jsonsetting = config.First().Settings;
                var settings = JsonSerializer.Deserialize<InternalEmailNotificationJsonModel>(jsonsetting);
                ViewBag.ProceedToPayment = settings.ProceedToPayment != null && bool.TryParse(settings.ProceedToPayment, out var proceed) ? proceed : false;  
            }
            return View(reservations);
        }

        // GET: PortalReservation/CancelReservationForm
        [HttpGet]
        public IActionResult CancelReservationForm(string? returnUrl)
        {
            // Try to get returnUrl from session if not provided
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = HttpContext.Session.GetString("reservationReturnUrl");
            }

            // Check if the return URL is valid and local to prevent open redirect attacks
            if (!string.IsNullOrEmpty(returnUrl) 
                //&& Url.IsLocalUrl(returnUrl)
                )
            {
                // Clear the session after using it
                HttpContext.Session.Remove("reservationReturnUrl");
                return Redirect(returnUrl);
            }

            // Default fallback to home page if no valid return URL
            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        public async Task<IActionResult> ConfirmReservationOtp(int id)
        {
            OTPViewModel otp = new OTPViewModel();
            otp.id = id;
            otp.Otp = string.Empty;
            otp.Message = string.Empty;
            otp.MaxAttempts = 3; // Default max attempts

            //get OTP Timeout and MaxAttempts from configuration
            var config = await _Configuration.GetPortalConfigurationByNameAsync("Reservation");
            if (config.Any())
            {
                string jsonsetting = config.First().Settings;
                var settings = JsonSerializer.Deserialize<InternalEmailNotificationJsonModel>(jsonsetting);
                int otpTimeout = 0;
                int.TryParse(settings.OtpTimeout, out otpTimeout);
                otp.Timeout = otpTimeout > 0 ? otpTimeout : 300; // 5 minutes (default)

                int maxAttempts = 0;
                int.TryParse(settings.OtpMaxAttempts, out maxAttempts);
                otp.MaxAttempts = maxAttempts > 0 ? maxAttempts : 3; // Default to 3 if not set
            }

            var reservations = await _service.GetByIdAsync(id);
            if (reservations == null)
            {
                return NotFound();
            }

            // Check if timeout flag is set (OTP expired)
            string timeoutKey = $"OTPTimeout_{id}";
            string timeoutFlag = HttpContext.Session.GetString(timeoutKey);
            if (!string.IsNullOrEmpty(timeoutFlag) && timeoutFlag == "true")
            {
                otp.Message = "OTP Verification Failed - Maximum timeout exceeded. Your reservation could not be verified and will be discarded.";
                HttpContext.Session.Remove(timeoutKey);

                // Mark reservation as discarded
                reservations.Status = "Discarded";
                await _service.UpdateAsync(reservations);

                return View(otp);
            }

            // Check if max attempts already exceeded
            string attemptKey = $"OTPAttempts_{id}";
            string attemptCountStr = HttpContext.Session.GetString(attemptKey);
            if (int.TryParse(attemptCountStr, out int attempts) && attempts >= otp.MaxAttempts)
            {
                otp.Message = $"Maximum OTP attempts ({otp.MaxAttempts}) exceeded. Your reservation could not be verified and will be discarded.";

                // Mark reservation as discarded
                reservations.Status = "Discarded";
                await _service.UpdateAsync(reservations);

                return View(otp);
            }

            var OTP = await _reservationService.GenerateOTP();// 6 digit OTP
            if (OTP.IsNullOrEmpty())
            {
                return NotFound();
            }

            ViewBag.Message = "Generating OTP!";

            // Reset attempts counter for new OTP generation
            HttpContext.Session.Remove(attemptKey);

            //save OTP to session
            HttpContext.Session.SetString("OTP", OTP);

            //send OTP to user email
            await _reservationService.SendCustomerOTP(reservations, OTP);

            return View(otp);
        }



        [HttpPost]
        public async Task<IActionResult> ConfirmReservationOtp(OTPViewModel viewModel)
        {
            var reservations = await _service.GetByIdAsync(viewModel.id);
            if (reservations == null)
            {
                return NotFound();
            }

            // Get max attempts from configuration
            int maxAttempts = 3; // Default
            var config = await _Configuration.GetPortalConfigurationByNameAsync("Reservation");
            if (config.Any())
            {
                string jsonsetting = config.First().Settings;
                var settings = JsonSerializer.Deserialize<InternalEmailNotificationJsonModel>(jsonsetting);
                int configMaxAttempts = 0;
                int.TryParse(settings.OtpMaxAttempts, out configMaxAttempts);
                maxAttempts = configMaxAttempts > 0 ? configMaxAttempts : 3;
            }

            // Track OTP verification attempts using session key with reservation ID
            string attemptKey = $"OTPAttempts_{viewModel.id}";
            int attempts = 0;
            string attemptCountStr = HttpContext.Session.GetString(attemptKey);
            if (int.TryParse(attemptCountStr, out int storedAttempts))
            {
                attempts = storedAttempts;
            }

            // Check if max attempts exceeded
            if (attempts >= maxAttempts)
            {
                viewModel.Message = $"Maximum OTP attempts ({maxAttempts}) exceeded. Please request a new OTP.";
                viewModel.MaxAttempts = maxAttempts;
                return View("ConfirmReservationOtp", viewModel);
            }

            // Get stored OTP from session
            var sessionOTP = HttpContext.Session.GetString("OTP");

            // Verify OTP
            if (string.Equals(sessionOTP, viewModel.Otp))
            {
                // OTP is valid - update reservation status to Verified
                reservations.Status = "Verified";
                await _service.UpdateAsync(reservations);

                // Send client email notification
                await this._reservationService.SendCustomerNotification(reservations);

                // Clear OTP and attempt tracking from session
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove(attemptKey);

                return RedirectToAction("Success", new { id = viewModel.id });
            }

            // Invalid OTP - increment attempt counter
            attempts++;
            HttpContext.Session.SetString(attemptKey, attempts.ToString());

            int remainingAttempts = maxAttempts - attempts;
            if (remainingAttempts > 0)
            {
                viewModel.Message = $"Invalid OTP. Please try again. ({remainingAttempts} attempt{(remainingAttempts != 1 ? "s" : "")} remaining)";
            }
            else
            {
                viewModel.Message = $"Maximum OTP attempts ({maxAttempts}) exceeded. Please request a new OTP.";
            }

            viewModel.MaxAttempts = maxAttempts;
            return View("ConfirmReservationOtp", viewModel);
        }



        // API ENDPOINTS
        // GET: api/PortalReservation
        [HttpGet]
        [Route("api/[controller]")]
        public async Task<ActionResult<IEnumerable<PortalReservation>>> GetPortalReservations()
        {
            return await _service.GetAllAsync();
        }

        // GET: api/PortalReservation/5
        [HttpGet("{id}")]
        [Route("api/[controller]/{id}")]
        public async Task<ActionResult<PortalReservation>> GetPortalReservation(int id)
        {
            var portalReservation = await _service.GetByIdAsync(id);

            if (portalReservation == null)
            {
                return NotFound();
            }

            return portalReservation;
        }

        // PUT: api/PortalReservation/5
        [HttpPut("{id}")]
        [Route("api/[controller]/{id}")]
        public async Task<IActionResult> PutPortalReservation(int id, PortalReservation portalReservation)
        {
            if (id != portalReservation.Id)
            {
                return BadRequest();
            }

            try
            {
                await _service.UpdateAsync(portalReservation);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_service.Exists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/PortalReservation
        [HttpPost]
        [Route("api/[controller]")]
        public async Task<ActionResult<PortalReservation>> PostPortalReservation(PortalReservation portalReservation)
        {
            await _service.AddAsync(portalReservation);

            return CreatedAtAction(nameof(GetPortalReservation), new { id = portalReservation.Id }, portalReservation);
        }

        // DELETE: api/PortalReservation/5
        [HttpDelete("{id}")]
        [Route("api/[controller]/{id}")]
        public async Task<IActionResult> DeletePortalReservation(int id)
        {
            var portalReservation = await _service.GetByIdAsync(id);
            if (portalReservation == null)
            {
                return NotFound();
            }

            await _service.DeleteAsync(portalReservation);

            return NoContent();
        }

        [HttpGet]
        [Route("api/[controller]/ProcessPendingReservations")]
        public async Task<IActionResult> ProcessPendingReservations()
        {
            // to be called by a cron job(cron-job.org) or scheduled task to process pending reservations
            _logger.LogInformation("ProcessPendingReservations endpoint called at {Time} from {RemoteIp}", 
                DateTime.Now, 
                HttpContext.Connection.RemoteIpAddress);

            await _reservationService.ProcessPendingReservations();
            return Ok(new { message = "Pending reservations processed successfully" });
        }


        [HttpPost]
        [Route("api/[controller]/PostRemoveSessionOTP")]
        public async Task<IActionResult> PostRemoveSessionOTP()
        {
            //clear OTP
            HttpContext.Session.SetString("OTP", String.Empty);

            return Ok(new { message = "Pending reservations processed successfully" });
        }

        [HttpPost]
        [Route("api/[controller]/SetOTPTimeout/{id}")]
        public IActionResult SetOTPTimeout(int id)
        {
            // Set timeout flag in session
            string timeoutKey = $"OTPTimeout_{id}";
            HttpContext.Session.SetString(timeoutKey, "true");

            return Ok(new { message = "OTP timeout flag set successfully" });
        }



    }
}
