using Erp.Domain.Models;
namespace Portal.Services
{
    public interface IReservationService
    {
        Task SendCustomerNotification(PortalReservation reservation );
        Task ProcessPendingReservations();
    }
}
