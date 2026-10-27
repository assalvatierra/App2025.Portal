namespace Portal.Services.VectorSearch.Models
{
    /// <summary>
    /// Represents a single vector record to be inserted or updated (upserted)
    /// in the vector database, including its payload/metadata.
    /// </summary>
    public class VectorRecord
    {
        /// <summary>
        /// The unique identifier of the point/record.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The embedding vector representing this record.
        /// </summary>
        public float[]? Vector { get; set; } = Array.Empty<float>();

        /// <summary>
        /// The payload/metadata to store alongside the vector (e.g., source text, category, etc.).
        /// </summary>
        public Dictionary<string, object?> Payload { get; set; } = new();
    }
}
