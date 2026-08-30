namespace App2025.Portal.Models
{
    public class CtaBoxViewModel
    {
        public string SectionTitle { get; set; }
        public string SectionIntro { get; set; }
        public ContactInfoViewModel ContactInfo { get; set; }
        public List<CtaActionViewModel> Actions { get; set; }
        public List<string> Tags { get; set; }

        // Display flags for contact info sections
        public bool ShowChat { get; set; }
        public bool ShowMobile { get; set; }
        public bool ShowEmail { get; set; }
        public bool ShowFacebook { get; set; }

        // Optional item ID for booking or quote requests
        public string ItemId { get; set; }
    }

    public class ContactInfoViewModel
    {
        public string ContactTitle { get; set; }
        public string Chat { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Facebook { get; set; }
    }

    public class CtaActionViewModel
    {
        public string Text { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; } // Bootstrap icon name (e.g., "calendar-check")
    }
}
