using Portal.Services.VectorSearch.Models;

namespace Portal.Services.VectorSearch
{
    /// <summary>
    /// Defines the contract for interacting with a vector database used for semantic search.
    /// Implementations are responsible for managing collections, loading/upserting data,
    /// and performing keyword or vector-based similarity search.
    /// </summary>
    public interface IVectorSearchService
    {
        /// <summary>
        /// Creates (or ensures the existence of) a vector collection with the given name
        /// and vector dimensionality.
        /// </summary>
        /// <param name="collectionName">The name of the collection to create.</param>
        /// <param name="vectorSize">The dimensionality of the vectors stored in this collection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CreateCollection(string collectionName, int vectorSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk loads a set of vector records into the specified collection.
        /// </summary>
        /// <param name="collectionName">The name of the target collection.</param>
        /// <param name="records">The records to load.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LoadData(string collectionName, IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts or updates a single vector record (or small batch) in the specified collection.
        /// </summary>
        /// <param name="collectionName">The name of the target collection.</param>
        /// <param name="records">The records to upsert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task Upsert(string collectionName, IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a semantic search using a raw keyword/text query. Implementations should
        /// embed the query text before performing the underlying vector similarity search.
        /// </summary>
        /// <param name="request">The search request containing the query text and search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<VectorSearchResult>> SearchByKeyword(VectorSearchRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a similarity search using a pre-computed embedding vector.
        /// </summary>
        /// <param name="request">The search request containing the vector and search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<VectorSearchResult>> SearchByVector(VectorSearchRequest request, CancellationToken cancellationToken = default);
    }
}
