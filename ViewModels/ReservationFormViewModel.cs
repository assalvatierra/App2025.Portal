using Erp.Domain.Models;
using Portal.Models;

namespace Portal.ViewModels
{
    public class ReservationFormViewModel
    {
        public PortalReservation Reservation { get; set; } = new();
        public ReservationAdditionalInfo ReservationAdditionalInfo { get; set; } = new();
        public ItemDto? Item { get; set; }
    }
}
