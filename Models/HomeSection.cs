namespace Portal.Models
{
    public class HomeSection
    {
        public string? Section { get; set; }
        public string? Title { get; set; }
        public string? Intro { get; set; }
        public string? FieldLabel { get; set; }
        public string? FieldText { get; set; }
        public string? ButtonLabel { get; set; }
        public string? ButtonText { get; set; }
        public string? ExtraInfo { get; set; }
        public string? DataFilter { get; set; }
        public bool Enabled { get; set; }
        public int itemsToShow { get; set; } = 0;
    }
}
