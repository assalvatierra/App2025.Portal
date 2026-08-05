namespace Portal.ViewModels
{
    public class OTPViewModel
    {
        public int id { get; set; }
        public string Otp { get; set; }
        public string Message { get; set; }
        public int Timeout { get; set; }
        public int MaxAttempts { get; set; }
    }
}
