using Erp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.DBServices;
using Portal.Services;
using Portal.Models;
using Portal.ViewModels;

namespace Portal.Controllers
{
    //[Authorize]
    public class PortalReservationController : Controller
    {
        private readonly IPortalConfigurationService _Configuration;
        private readonly IPortalReservationService _service;
        private readonly IPortalItemService _portalItemService;
        private readonly IReservationService _reservationService;

        public PortalReservationController(
            IPortalConfigurationService portalConfigurationService,
            IPortalReservationService service, 
            IPortalItemService portalItemService,
            IReservationService reservationService)
        {
            _Configuration = portalConfigurationService;
            _service = service;
            _portalItemService = portalItemService;
            _reservationService = reservationService;
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
                Status = "Pending",
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
            reservation.Status = "Pending";

            if (ModelState.IsValid)
            {
                // JsonData is already populated by JavaScript in the view
                if (string.IsNullOrEmpty(reservation.JsonData))
                {
                    reservation.JsonData = "{}";
                }

                await _service.AddAsync(reservation);
                TempData["SuccessMessage"] = "Reservation submitted successfully!";
                TempData["TransactionType"] = reservation.TransactionType;
                return RedirectToAction("Success", new { id = reservation.Id });
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
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonsetting);
                ViewBag.ProceedToPayment = settings.ContainsKey("ProceedToPayment") && bool.TryParse(settings["ProceedToPayment"], out var proceed) ? proceed : false;  
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
        public async Task<IActionResult> ProcessPendingReservations()
        {
            _reservationService.ProcessPendingReservations();
            return NoContent();
        }

    }
}
