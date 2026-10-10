namespace Portal.Services.MessageBroker.Events
{
    /// <summary>
    /// Published when a reservation's OTP has been verified.
    /// </summary>
    public class ReservationVerified
    {
        public int ReservationId { get; set; }
        public string? CustomerName { get; set; }
        public string? ContactEmail { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? DateReceived { get; set; }
        public int? PortalItem { get; set; }
        public string? ContactNo { get; set; }
        public string? jsonData { get; set; }
        public string? Status { get; set; }
    }
}
