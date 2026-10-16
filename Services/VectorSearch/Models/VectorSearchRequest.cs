namespace Portal.Services.VectorSearch.Models
{
    /// <summary>
    /// Represents a request to search the vector database, either by raw keyword/text
    /// (which will be embedded) or by a pre-computed vector.
    /// </summary>
    public class VectorSearchRequest
    {
        /// <summary>
        /// The name of the collection to search within.
        /// </summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// The raw text/keyword query used for keyword-based semantic search.
        /// Ignored when performing a direct vector search.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// The vector representation used for direct similarity search.
        /// Ignored when performing a keyword-based search.
        /// </summary>
        public float[]? Vector { get; set; }

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int Limit { get; set; } = 10;

        /// <summary>
        /// Minimum similarity score threshold (0.0 - 1.0) for results to be included.
        /// </summary>
        public float? ScoreThreshold { get; set; }

        /// <summary>
        /// Optional metadata filters to apply to the search (field name/value pairs).
        /// </summary>
        public Dictionary<string, object>? Filters { get; set; }
    }
}
