using Erp.Domain.Models;

namespace Portal.Models
{
    public class ReservationFormViewModel
    {
        public PortalReservation Reservation { get; set; } = new();
        public ReservationAdditionalInfo ReservationAdditionalInfo { get; set; } = new();
        public ItemDto? Item { get; set; }
    }
}
