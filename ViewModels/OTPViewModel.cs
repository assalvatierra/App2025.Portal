namespace Portal.ViewModels
{
    public class OTPViewModel
    {
        public int id { get; set; }
        public string Otp { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Timeout { get; set; }
        public int MaxAttempts { get; set; }
    }
}
