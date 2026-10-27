using Portal.Services.VectorSearch.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Net.Http.Json;
using System.Text.Json;
using Portal.DBServices;

namespace Portal.Services.VectorSearch
{
    /// <summary>
    /// Qdrant-based implementation of <see cref="IVectorSearchService"/>. Handles collection
    /// management, data loading/upserting, and keyword/vector similarity search
    /// against a Qdrant vector database instance.
    /// </summary>
    public class QdrantVectorSearchService : IVectorSearchService
    {
        private readonly QdrantClient _client;
        private readonly ILogger<QdrantVectorSearchService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _openAiApiKey;
        private readonly string _openAiEndpoint;
        private readonly string _embeddingApiKey;
        private readonly string _embeddingModel;
        private readonly IPortalItemService _portalItemService;

        public QdrantVectorSearchService(
            IConfiguration configuration,
            ILogger<QdrantVectorSearchService> logger,
            IHttpClientFactory httpClientFactory,
            IPortalItemService portalItemService,
            QdrantClient client)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _portalItemService = portalItemService;
            _client = client;

            //var host = configuration["VectorDb:Qdrant:Host"] ?? "localhost";
            //var port = configuration.GetValue<int?>("VectorDb:Qdrant:Port") ?? 6334;
            //var https = configuration.GetValue<bool?>("VectorDb:Qdrant:Https") ?? false;
            //var apiKey = configuration["VectorDb:Qdrant:ApiKey"];
            //_client = new QdrantClient(host, port, https, apiKey);

            _openAiApiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
            _openAiEndpoint = configuration["VectorDb:EmbeddingEndpoint"]
                ?? configuration["OpenAI:Endpoint"]
                ?? "https://api.openai.com/v1";
            // Embedding API key: prefer VectorDb:EmbeddingApiKey, fall back to OpenAI:ApiKey
            _embeddingApiKey = configuration["VectorDb:EmbeddingApiKey"] ?? configuration["OpenAI:ApiKey"] ?? string.Empty;
            if (string.IsNullOrEmpty(_embeddingApiKey))
            {
                _logger.LogWarning("Embedding API key is not configured. Set VectorDb:EmbeddingApiKey or OpenAI:ApiKey.");
            }
            _embeddingModel = configuration["VectorDb:EmbeddingModel"] ?? "text-embedding-3-small";
        }

        /// <inheritdoc />
        public async Task CreateCollection(string collectionName, int vectorSize, CancellationToken cancellationToken = default)
        {
            try
            {

                var exists = await _client.CollectionExistsAsync(collectionName, cancellationToken);

                if (exists)
                {
                    _logger.LogInformation("Qdrant collection {CollectionName} already exists. Updating data.", collectionName);
                }
                else
                {
                    _logger.LogInformation("Creating Qdrant collection {CollectionName} with vector size {VectorSize}.", collectionName, vectorSize);
                    await this.CreateSampleCollection(_client, collectionName, cancellationToken);
                }

                // Always upsert sample points (create or update)
                //await this.UploadSamplePoints(_client, collectionName, cancellationToken);
                await this.LoadPortalItems(collectionName, cancellationToken);

                _logger.LogInformation("Qdrant collection {CollectionName} with vector size {VectorSize} is ready.", collectionName, vectorSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or updating Qdrant collection {CollectionName}.", collectionName);
            }
        }

        private async Task LoadPortalItems(string collectionName, CancellationToken cancellationToken = default)
        {
            // Load portal items from database using GetItemsByCategory
            var itemDtos = await _portalItemService.GetItemsByCategory("", "Product");

            // Convert ItemDto to VectorRecord format with embedding generation
            var recordTasks = itemDtos.Select(async item =>
            {
                // Combine description, PortalItem.JsonData, and all PortalItemSpecs.JsonData for embedding
                var description = $"{item.PortalItem.Name} {item.PortalItem.Description} {item.Title} {item.Description}" ?? "";
                var portalItemJsonData = item.PortalItem?.JsonData ?? "";
                var portalItemSpecsJsonData = string.Join(" ", item.PortalItem?.PortalItemSpecs?.Select(s => s.JsonData ?? "") ?? Enumerable.Empty<string>());
                var portalItemPricesJsonData = string.Join(" ", item.PortalItem?.PortalItemPrices?.Select(p => p.JsonData ?? "") ?? Enumerable.Empty<string>());
                var textForEmbedding = $"{description} {portalItemJsonData} {portalItemSpecsJsonData} {portalItemPricesJsonData}".Trim();

                // Generate embedding for the combined text
                float[]? vector = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(textForEmbedding))
                    {
                        vector = await GenerateEmbeddingAsync(textForEmbedding, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for item {ItemId}. Using null vector.", item.PortalItem?.Id);
                }

                return new VectorRecord
                {
                    Id = item.PortalItem?.Id.ToString() ?? Guid.NewGuid().ToString(),
                    Vector = vector,
                    Payload = new Dictionary<string, object>
                    {
                        { "id", item.PortalItem?.Id ?? 0 },
                        { "name", item.Title ?? "Unknown" },
                        { "description", item.Description ?? "" }
                    }
                };
            });

            // Await all embedding generation tasks
            var records = await Task.WhenAll(recordTasks);

            // Upsert the records into Qdrant
            await UpsertInternal(collectionName, records, cancellationToken);
        }

        // SAMPLE Collection
        private async Task CreateSampleCollection(QdrantClient client, string collectionName, CancellationToken cancellationToken = default)
        {
            // 1. Create the main collection with HNSW Vector Parameters
            await client.CreateCollectionAsync(
                collectionName: collectionName,
                vectorsConfig: new VectorParams
                {
                    Size = 1536,                // Matching your 1536 embedding size
                    Distance = Distance.Cosine  // Best practice metric for embeddings
                },
                hnswConfig: new HnswConfigDiff
                {
                    M = 16,
                    EfConstruct = 100
                }
            );

            // Load portal items into the collection
            await LoadPortalItems(collectionName, cancellationToken);

            // 2. Equivalent to making fields 'IsSearchable'. 
            // We add Text Payload Indexes so Qdrant compiles and tokens matching string logic.
            await client.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "name",
                schemaType: PayloadSchemaType.Text,
                wait: true
            );

            await client.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: "description",
                schemaType: PayloadSchemaType.Text,
                wait: true
            );
        }
        public async Task UploadSamplePoints(QdrantClient client, string collectionName, CancellationToken cancellationToken = default)
        {
            // Map your data models directly into PointStruct formats.
            // Qdrant Point IDs must be valid Unsigned 64-bit integers or system UUIDs.
            var points = new List<PointStruct>
        {
            new PointStruct
            {
                Id = 1,
                Vectors = await GenerateEmbeddingAsync("reliable SUV ideal for family trips", cancellationToken),
                Payload =
                {
                    { "name", "Toyota Fortuner" },
                    { "description", "A reliable SUV ideal for family trips." }
                }
            },
            new PointStruct
            {
                Id = 2,
                Vectors = await GenerateEmbeddingAsync("powerful pickup truck off-road", cancellationToken),
                Payload =
                {
                    { "name", "Ford Ranger" },
                    { "description", "A powerful pickup truck suitable for off-road adventures." }
                }
            }
        };

            await client.UpsertAsync(collectionName, points);
        }



        /// <inheritdoc />
        public async Task LoadData(string collectionName, IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default)
        {
            await UpsertInternal(collectionName, records, cancellationToken);
            _logger.LogInformation("Loaded {Count} records into Qdrant collection {CollectionName}.", records.Count(), collectionName);
        }

        /// <inheritdoc />
        public async Task Upsert(string collectionName, IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default)
        {
            await UpsertInternal(collectionName, records, cancellationToken);
            _logger.LogInformation("Upserted {Count} records into Qdrant collection {CollectionName}.", records.Count(), collectionName);
        }

        /// <inheritdoc />
        public async Task<List<VectorSearchResult>> SearchByKeyword(VectorSearchRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ArgumentException("Query text must be provided for keyword search.", nameof(request));
            }
            if (request.Vector == null || request.Vector.Length == 0)
            {
                request.Vector = await GenerateEmbeddingAsync(request.Query, cancellationToken);
            }

            var vectorRequest = new VectorSearchRequest
            {
                CollectionName = request.CollectionName,
                Vector = request.Vector,
                Limit = request.Limit,
                ScoreThreshold = request.ScoreThreshold,
                Filters = request.Filters
            };

            return await SearchByVector(vectorRequest, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<List<VectorSearchResult>> SearchByVector(VectorSearchRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Vector == null || request.Vector.Length == 0)
            {
                request.Vector = await GenerateEmbeddingAsync(request.Query, cancellationToken);
            }

            var points = await _client.SearchAsync(
                request.CollectionName,
                request.Vector,
                limit: (ulong)request.Limit,
                scoreThreshold: request.ScoreThreshold,
                cancellationToken: cancellationToken);

            return points.Select(p => new VectorSearchResult
            {
                Id = p.Id.Uuid ?? p.Id.Num.ToString(),
                Score = p.Score,
                Payload = p.Payload.ToDictionary(kv => kv.Key, kv => (object?)kv.Value.StringValue)
            }).ToList();
        }

        private async Task UpsertInternal(string collectionName, IEnumerable<VectorRecord> records, CancellationToken cancellationToken)
        {
            var points = records.Select(r =>
            {
                var point = new PointStruct
                {
                    Vectors = new Vectors
                    {
                        Vector = new Vector { Data = { r.Vector } }
                    }
                };

                // Use numeric ID if the string represents a valid ulong, otherwise use UUID
                if (ulong.TryParse(r.Id, out var numericId))
                {
                    point.Id = new PointId { Num = numericId };
                }
                else
                {
                    point.Id = new PointId { Uuid = r.Id };
                }

                foreach (var kv in r.Payload)
                {
                    point.Payload[kv.Key] = kv.Value?.ToString() ?? string.Empty;
                }

                return point;
            }).ToList();

            await _client.UpsertAsync(collectionName, points, cancellationToken: cancellationToken);
        }

        private async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text must be provided to generate an embedding.", nameof(text));
            }

            if (string.IsNullOrEmpty(_embeddingApiKey))
            {
                throw new InvalidOperationException("Embedding API Key is not configured. Please set VectorDb:EmbeddingApiKey or OpenAI:ApiKey in configuration.");
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_openAiEndpoint.TrimEnd('/') + "/");

            // Use Azure-style api-key header when endpoint looks like Azure; otherwise fall back to Bearer
            if (_openAiEndpoint.Contains("azure", StringComparison.OrdinalIgnoreCase) || _openAiEndpoint.Contains("services.ai.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                // ensure no conflicting Authorization header
                if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
                if (httpClient.DefaultRequestHeaders.Contains("api-key"))
                {
                    httpClient.DefaultRequestHeaders.Remove("api-key");
                }
                httpClient.DefaultRequestHeaders.Add("api-key", _embeddingApiKey);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _embeddingApiKey);
            }

            var payload = new
            {
                input = text,
                model = _embeddingModel
            };

            var response = await httpClient.PostAsJsonAsync("embeddings", payload, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            // Expecting response shape: { data: [ { embedding: [...] } ] }
            if (!json.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array || dataElement.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Unexpected embedding response format: missing data array.");
            }

            var embeddingArray = dataElement[0].GetProperty("embedding");
            return embeddingArray.EnumerateArray().Select(e => e.GetSingle()).ToArray();
        }
    }
}
