using Erp.Domain.Models;
using Portal.DBLayer;

namespace Portal.DBServices
{
    public class PortalReservationService : IPortalReservationService
    {
        private readonly IPortalReservationDbLayer _db;

        public PortalReservationService(IPortalReservationDbLayer db)
        {
            _db = db;
        }

        public async Task<List<PortalReservation>> GetAllAsync()
        {
            return await _db.GetAllAsync();
        }

        public async Task<List<PortalReservation>> GetByStatusAsync(string status)
        {
            return await _db.GetByStatusAsync(status);
        }

        public async Task<PortalReservation?> GetByIdAsync(int id)
        {
            return await _db.GetByIdAsync(id);
        }

        public async Task UpdateAsync(PortalReservation portalReservation)
        {
            await _db.UpdateAsync(portalReservation);
        }

        public async Task<PortalReservation> AddAsync(PortalReservation portalReservation)
        {
            portalReservation.DateReceived = DateTime.UtcNow;
            return await _db.AddAsync(portalReservation);
        }

        public async Task DeleteAsync(PortalReservation portalReservation)
        {
            await _db.DeleteAsync(portalReservation);
        }

        public bool Exists(int id)
        {
            return _db.Exists(id);
        }
    }
}
