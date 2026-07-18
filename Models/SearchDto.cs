namespace Portal.Models
{
    public class SearchDto
    {
        public string? searchTerm { get; set; }
        public DateTime pickup_date { get; set; }
        public DateTime return_date { get; set; }
        public int passenger_capacity { get; set; }
        public int luggage_capacity { get; set; }
        public string? transmission { get; set; }
        public string? category { get; set; }

    }
}
