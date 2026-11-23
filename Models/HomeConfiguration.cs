namespace Portal.Models
{
    public class HomeConfiguration
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? h1Title { get; set; }
        public string? h1Subtitle { get; set; }
        public List<HomeSection>? Sections { get; set; }
    }
}
