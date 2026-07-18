using Erp.Domain.Models;

namespace Portal.DBServices
{
    public interface IPortalReservationService
    {
        Task<List<PortalReservation>> GetAllAsync();
        Task<List<PortalReservation>> GetByStatusAsync(string status);
        Task<PortalReservation?> GetByIdAsync(int id);
        Task UpdateAsync(PortalReservation portalReservation);
        Task<PortalReservation> AddAsync(PortalReservation portalReservation);
        Task DeleteAsync(PortalReservation portalReservation);
        bool Exists(int id);
    }
}
