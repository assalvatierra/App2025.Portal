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
    }
}
