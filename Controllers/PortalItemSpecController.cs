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
    public class PortalItemSpecController : ControllerBase
    {
        private readonly IPortalItemSpecService _service;

        public PortalItemSpecController(IPortalItemSpecService service)
        {
            _service = service;
        }

        // GET: api/PortalItemSpec
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortalItemSpec>>> GetPortalItemSpecs()
        {
            return await _service.GetAllAsync();
        }

        // GET: api/PortalItemSpec/ByPortalItem/5
        [HttpGet("ByPortalItem/{portalItemId}")]
        public async Task<ActionResult<IEnumerable<PortalItemSpec>>> GetByPortalItem(int portalItemId)
        {
            return await _service.GetByPortalItemIdAsync(portalItemId);
        }

        // GET: api/PortalItemSpec/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PortalItemSpec>> GetPortalItemSpec(int id)
        {
            var portalItemSpec = await _service.GetByIdAsync(id);

            if (portalItemSpec == null)
            {
                return NotFound();
            }

            return portalItemSpec;
        }

        // PUT: api/PortalItemSpec/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPortalItemSpec(int id, PortalItemSpec portalItemSpec)
        {
            if (id != portalItemSpec.Id)
            {
                return BadRequest();
            }

            try
            {
                await _service.UpdateAsync(portalItemSpec);
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

        // POST: api/PortalItemSpec
        [HttpPost]
        public async Task<ActionResult<PortalItemSpec>> PostPortalItemSpec(PortalItemSpec portalItemSpec)
        {
            await _service.AddAsync(portalItemSpec);

            return CreatedAtAction(nameof(GetPortalItemSpec), new { id = portalItemSpec.Id }, portalItemSpec);
        }

        // DELETE: api/PortalItemSpec/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortalItemSpec(int id)
        {
            var portalItemSpec = await _service.GetByIdAsync(id);
            if (portalItemSpec == null)
            {
                return NotFound();
            }

            await _service.DeleteAsync(portalItemSpec);

            return NoContent();
        }
    }
}
