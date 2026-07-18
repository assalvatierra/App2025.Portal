using Erp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Portal.DBLayer
{
    public class PortalReservationDbLayer : IPortalReservationDbLayer
    {
        private readonly ApplicationDbContext _context;

        public PortalReservationDbLayer(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PortalReservation>> GetAllAsync()
        {
            return await _context.PortalReservation.ToListAsync();
        }

        public async Task<List<PortalReservation>> GetByStatusAsync(string status)
        {
            return await _context.PortalReservation
                .Where(r => r.Status == status)
                .ToListAsync();
        }

        public async Task<PortalReservation?> GetByIdAsync(int id)
        {
            return await _context.PortalReservation.FindAsync(id);
        }

        public async Task UpdateAsync(PortalReservation portalReservation)
        {
            _context.Entry(portalReservation).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<PortalReservation> AddAsync(PortalReservation portalReservation)
        {
            _context.PortalReservation.Add(portalReservation);
            await _context.SaveChangesAsync();
            return portalReservation;
        }

        public async Task DeleteAsync(PortalReservation portalReservation)
        {
            _context.PortalReservation.Remove(portalReservation);
            await _context.SaveChangesAsync();
        }

        public bool Exists(int id)
        {
            return _context.PortalReservation.Any(e => e.Id == id);
        }
    }
}
