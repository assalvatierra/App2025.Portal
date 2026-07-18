using Erp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.DBServices;

namespace Portal.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PortalItemController : ControllerBase
    {
        private readonly IPortalItemService _service;

        public PortalItemController(IPortalItemService service)
        {
            _service = service;
        }

        // GET: api/PortalItem
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortalItem>>> GetPortalItems()
        {
            return await _service.GetAllAsync();
        }

        // GET: api/PortalItem/searchItems?searchTerm=example
        //[AllowAnonymous]
        //[HttpGet("searchItems")]
        //public async Task<ActionResult<IEnumerable<PortalItem>>> SearchItems([FromQuery] string? searchTerm)
        //{
        //    var search = new SearchDto { searchTerm = searchTerm };
        //    return await _service.SearchItemsAsync(search);
        //}

        // GET: api/PortalItem/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PortalItem>> GetPortalItem(int id)
        {
            var portalItem = await _service.GetByIdAsync(id);

            if (portalItem == null)
            {
                return NotFound();
            }

            return portalItem;
        }

        // PUT: api/PortalItem/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPortalItem(int id, PortalItem portalItem)
        {
            if (id != portalItem.Id)
            {
                return BadRequest();
            }

            try
            {
                await _service.UpdateAsync(portalItem);
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

        // POST: api/PortalItem
        [HttpPost]
        public async Task<ActionResult<PortalItem>> PostPortalItem(PortalItem portalItem)
        {
            await _service.AddAsync(portalItem);

            return CreatedAtAction(nameof(GetPortalItem), new { id = portalItem.Id }, portalItem);
        }

        // DELETE: api/PortalItem/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortalItem(int id)
        {
            var portalItem = await _service.GetByIdAsync(id);
            if (portalItem == null)
            {
                return NotFound();
            }

            await _service.DeleteAsync(portalItem);

            return NoContent();
        }
    }
}
