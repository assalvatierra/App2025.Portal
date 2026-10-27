namespace Portal.Services.VectorSearch.Models
{
    public class QdrantOptions
    {
        public string Endpoint { get; set; } = default!;
        public string? ApiKey { get; set; }
        public bool UseHttps { get; set; } = false;

        public Uri GetUri()
        {
            var scheme = UseHttps ? "https" : "http";

            var uri = this.Endpoint.StartsWith("http")?
                new Uri(this.Endpoint) :
                new Uri($"{scheme}://{this.Endpoint}"); 

            return uri;
        }
    }
}
