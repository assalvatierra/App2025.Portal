namespace Portal.Services.VectorSearch.Models
{
    /// <summary>
    /// Represents a single match returned from a vector database search.
    /// </summary>
    public class VectorSearchResult
    {
        /// <summary>
        /// The unique identifier of the matched point/record in the vector database.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The similarity score of the match (higher is more similar).
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// The payload/metadata associated with the matched point.
        /// </summary>
        public Dictionary<string, object?> Payload { get; set; } = new();

        /// <summary>
        /// The vector associated with the matched point, if requested/returned.
        /// </summary>
        public float[]? Vector { get; set; }
    }
}
