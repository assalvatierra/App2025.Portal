# VectorDB Implementation Summary

## Overview
The VectorDB module provides automated scheduling and management of vector database updates for semantic search capabilities within the Portal application. It integrates Qdrant vector storage with Azure AI embedding services and automatic collection lifecycle management.

## Architecture

### Core Components

#### 1. **VectorDbSchedulerService** (`Services/VectorDbSchedulerService.cs`)
A hosted background service that runs continuously, monitors scheduled updates, and manages vector collection lifecycle.

**Key Features:**
- Polls every 30 seconds to check if a scheduled update is due
- Uses distributed cache to store and retrieve schedule settings persistently
- Automatically creates an initial schedule (60 seconds from startup, 1-day interval) if none exists
- Gracefully handles cancellation and exceptions with comprehensive logging
- Creates isolated service scopes for cache operations
- **Initializes vector collections automatically on each scheduled update**
- **Manages collection lifecycle with CreateCollection (1536-dimensional, Cosine distance)**

**Polling Mechanism:**
- Runs on a 30-second interval (`PollingInterval = TimeSpan.FromSeconds(30)`)
- Checks whether `DateTime.UtcNow >= NextSchedule`
- Triggers update callbacks when schedule is reached
- Automatically recalculates next schedule based on configured interval

**Collection Management:**
- Injects `IVectorSearchService` for Qdrant operations
- Creates/ensures collection exists with vector size 1536 (text-embedding-3-small standard)
- Collection name from `VectorDb:Qdrant:CollectionName` configuration ("cars" default)
- Called on every scheduled update trigger via `InitializeVectorCollectionsAsync()`
- Logs success and failures; continues scheduler if CreateCollection fails
- **CreateCollection now implements upsert semantics: creates if not exists, updates data if already exists**

#### 2. **VectorDbScheduleSettings** (`Models/VectorDbScheduleSettings.cs`)
Configuration model representing scheduling parameters.

**Properties:**
- `NextSchedule` (DateTime): The next scheduled time for an update
- `IntervalValue` (int): Numeric interval value (e.g., 1, 2, 30)
- `IntervalUnit` (string): Interval unit - "Minutes", "Hours", or "Days"
- `LastModified` (DateTime): When settings were last changed
- `LastUpdateTriggered` (DateTime?): When the last update was executed (manual or scheduled)

#### 3. **QdrantVectorSearchService** (`Services/VectorSearch/QdrantVectorSearchService.cs`)
Handles vector collection management and semantic search operations.

**Key Methods:**
- `CreateCollection(collectionName, vectorSize, cancellationToken)` - Creates or updates vector collection (upsert semantics)
  - If collection exists: Updates sample data points
  - If collection not exists: Creates new collection structure and loads data
  - Calls `LoadPortalItems()` to populate collection with database items
- `LoadPortalItems()` - Loads items from database and generates embeddings
  - Fetches ItemDto records using `IPortalItemService.GetItemsByCategory("", "Product")`
  - For each item, combines text from: 
    - Item Description
    - Item JsonData (from `PortalItem.JsonData`)
    - Specification JsonData (from all `PortalItem.PortalItemSpecs[*].JsonData` combined)
  - Generates vector embeddings asynchronously using Azure AI embeddings
  - Gracefully handles null vectors with warning logs
  - Upserts records to Qdrant collection with payload metadata (id, name, description)
- `LoadData()` - Bulk loads vector records
- `SearchByKeyword()` - Text-to-embedding then vector search
- `SearchByVector()` - Direct vector similarity search

**Data Enrichment Strategy:**
- Embeddings capture comprehensive product information by combining multiple data sources
- Description: User-friendly summary text
- JsonData: Structured product attributes (title, category, etc.)
- PortalItemSpecs JsonData: Technical specifications data (features, colors, sizes, etc.)
- Multi-source approach improves search relevance for semantic queries

#### 4. **HomeController Integration** (`Controllers/HomeController.cs`)
Provides UI and API endpoints for managing schedules and triggering updates.

**Endpoints:**
- `GET /Home/VectorDB` - Displays vector database configuration and scheduling UI
- `POST /Home/SetVectorDbSchedule` - Sets/updates the schedule configuration
- `POST /Home/UpdateVectorDbNow` - Manually triggers immediate vector collection upsert
  - Calls `_vectorSearchService.CreateCollection("cars", 1536)` to update collection
  - Updates `LastUpdateTriggered` timestamp
  - Recalculates next scheduled time
  - Returns success/error status to UI

**Integration:**
- Injected with `IVectorSearchService` for direct collection management
- Allows manual updates independent of scheduled polling
- Persists updated schedule timestamps to distributed cache

## Registration & Setup

**Program.cs Configuration:**
```csharp
// Register VectorDB Scheduler as hosted service
builder.Services.AddHostedService<Portal.Services.VectorDbSchedulerService>();

// Register IVectorSearchService (injected by scheduler for collection management)
builder.Services.AddScoped<IVectorSearchService, QdrantVectorSearchService>();
```

The services are registered during application startup and begin operations immediately.

## Embedding Configuration

**appsettings.Development.json:**
```json
{
  "VectorDb": {
    "Type": "Qdrant",
    "EmbeddingEndpoint": "https://#########-######.ai.azure.com/openai/v1",
    "EmbeddingApiKey": "#############################",
    "EmbeddingModel": "text-embedding-3-small",
    "Qdrant": {
      "Host": "################.qdrant.io",
      "Port": 6334,
      "Https": true,
      "ApiKey": "#############################",
      "CollectionName": "cars"
    }
  }
}
```

**Embedding Endpoint Format:**
- **Azure OpenAI:** Must include `/openai/v1` path (e.g., `https://resource.openai.azure.com/openai/v1`)
- **Public OpenAI:** Format is `https://api.openai.com/v1`
- Incorrect endpoint URL will result in 404 errors (see embedding endpoint fix documentation)

**Vector Collection Details:**
- Vector Size: 1536 dimensions (text-embedding-3-small standard output)
- Distance Metric: Cosine similarity
- Collection Name: "cars" (configurable via `VectorDb:Qdrant:CollectionName`)
- HNSW Index: M=16, EfConstruct=100
- Payload Indexes: "name" and "description" text fields

## Cache Strategy

- **Storage:** Distributed Cache (Redis/SQL Server configured at startup)
- **Cache Key:** `"VectorDbScheduleSettings"`
- **TTL:** 365 days (effectively persistent until manually modified)
- **Serialization:** JSON format using `System.Text.Json`

## Update Flow

1. **Initialization:** Service starts and checks for existing schedule in cache
2. **Default Schedule:** If no schedule exists, creates default (60s delay, 1-day recurring)
3. **Polling:** Every 30 seconds, compares current time to `NextSchedule`
4. **Trigger:** When `DateTime.UtcNow >= NextSchedule`:
   - Calls `InitializeVectorCollectionsAsync()` to ensure collection exists
   - Creates new collection with 1536 dimensions OR updates existing collection (upsert)
   - Upserts sample points (creates if missing, updates if exists)
   - Recalculates `NextSchedule` based on interval settings
   - Updates `LastUpdateTriggered` timestamp
5. **Persistence:** Updated settings saved back to distributed cache

**Manual Trigger Flow (UpdateVectorDbNow):**
- User clicks "Update Vector DB Now" button in UI
- `POST /Home/UpdateVectorDbNow` endpoint triggered
- Calls `_vectorSearchService.CreateCollection("cars", 1536)` directly
- Updates scheduling timestamps
- Returns success/error response immediately to UI

## Collection Initialization Flow

**InitializeVectorCollectionsAsync Method:**
1. Logs collection initialization with timestamp and vector size (1536)
2. Calls `IVectorSearchService.CreateCollection(collectionName, 1536, cancellationToken)`
3. CreateCollection implementation:
   - Checks if collection exists in Qdrant
   - **If exists:** Logs "updating data" and proceeds to upsert sample points
   - **If not exists:** Creates new collection structure with HNSW parameters
   - Always upserts sample points (Toyota Fortuner, Ford Ranger) regardless of collection state
   - Ends with "collection is ready" message
4. Logs success with timestamp
5. Catches exceptions, logs with full context and collection name
6. Returns without throwing (scheduler continues to next interval)

**Upsert Behavior Details:**
- Collections are created on-demand with proper indexing structure
- Sample data points are refreshed on every update (create if new, update if exists)
- No data loss on subsequent updates; existing collections retain history
- Flexible for future data loading via LoadData() or Upsert() methods

## Integration with Embedding Service

When scheduled updates trigger:
1. Vector collection is created/ensured via `IVectorSearchService.CreateCollection()`
2. Collection ready for vector storage and search operations
3. Embedding conversion (text ? vectors) handled by `GenerateEmbeddingAsync`
4. Supports Azure AI (`api-key` header) and OpenAI (`Bearer` token) endpoints
5. Embeddings stored as float[] in collection with metadata

## Error Handling

- Logs all errors with context timestamps
- Catches and logs cancellation exceptions
- **Catches CreateCollection exceptions and logs them; continues polling (no crash)**
- Preserves scheduler stability across collection initialization failures
- Preserves service stability across exception events
- Clear error messages with collection name, timestamp, and root cause

## Embedding Endpoint Configuration Issues

**Common Problem: 404 Not Found Errors**
- **Root Cause:** Embedding endpoint URL missing required path segment
- **Azure OpenAI:** Endpoint MUST include `/openai/v1` path
  - ❌ Wrong: `https://resource.openai.azure.com/`
  - ✅ Correct: `https://resource.openai.azure.com/openai/v1`
- **Public OpenAI:** Endpoint MUST include `/v1` path
  - ❌ Wrong: `https://api.openai.com`
  - ✅ Correct: `https://api.openai.com/v1`
- **Error Impact:** GenerateEmbeddingAsync will fail with HTTP 404, blocking collection initialization
- **Fix:** Verify appsettings configuration includes full endpoint path with API version segment

## Interval Calculation Engine

Supports flexible scheduling intervals:
- **Minutes:** For testing or high-frequency updates (e.g., 15 minutes)
- **Hours:** For moderate-frequency syncs (e.g., 2 hours)
- **Days:** For daily or multi-day batch updates (default: 1 day)

Next schedule is calculated as: `CurrentTime + (IntervalValue × IntervalUnit)`

## Vector Search Endpoint

#### **SearchByVectorAsync** (`Controllers/HomeController.cs`)
Provides user-facing semantic search capability using vector similarity.

**Endpoint Details:**
- **Route:** `GET /Home/SearchByVectorAsync`
- **Parameter:** `query` (string) - Free-text search query (e.g., "compact sedan with leather seats")
- **Returns:** `View("ItemList", List<ItemDto>)` - Same view as traditional keyword search results

**Method Flow:**
1. Accepts free-text query parameter
2. Creates `VectorSearchRequest` with:
   - `CollectionName: "cars"` - Hardcoded collection name
   - `Query: query` - The user's text input
   - `Limit: 20` - Maximum 20 results
   - `ScoreThreshold: 0.5f` - Minimum similarity score filter
3. Calls `_vectorSearchService.SearchByVector(vectorSearchRequest)`
4. Extracts item IDs from `VectorSearchResult.Payload["id"]` field
5. Fetches full `PortalItem` details via `_portalItemService.GetByIdListAsync(itemIds)`
6. Maps results to `ItemDto` objects (already done by service layer)
7. Sets ViewBag properties:
   - `PageTitle: "Vector Search Results"`
   - `PageMessage: "AI searched results for: {query}"`
   - `SearchTerm: query`
   - `CompareList: compareList` (from session, same as SearchItems)
   - `cardMode: "List"`
8. Returns populated `ItemList` view

**Error Handling:**
- **Null/Empty Query:** Returns empty ItemList with message "Please provide a search query."
- **No Results Found:** Returns empty ItemList with message "No results found for: {query}"
- **Invalid ID Parsing:** Silently skips malformed IDs in payload using `int.TryParse()`
- **Service Exceptions:** Wrapped in try-catch, returns ItemList with error message "An error occurred during vector search: {ex.Message}"

**Key Differences from SearchItems:**
- Accepts only `query` parameter (no date, capacity, transmission filters)
- Uses vector similarity instead of traditional keyword matching
- Powered by Azure AI embeddings and Qdrant vector database
- Results are semantic matches rather than lexical matches

**UI Integration:**
- Located in `Views/Home/Index.cshtml` (lines 59-75)
- Separate form section with distinct visual styling
- Input placeholder: "Search by features, style, or description..."
- Button label: "Search with AI" with `bi-search` icon
- Background color: `#6c757d` (gray) to differentiate from regular search button

**Configuration:**
- Collection name is hardcoded to `"cars"` (matches VectorDbSchedulerService default)
- Search limit and score threshold can be adjusted in endpoint implementation
- Shares the same rating limit attribute `[RateLimit]` as other search endpoints

## Debugging Guide

### Common Issues & Solutions

#### 1. **Qdrant Upsert Not Persisting**
**Symptom:** Data sent to Qdrant but collection remains empty

**Root Causes & Fixes:**
- **Empty Vectors:** Embeddings not generated or lost during PointStruct conversion
  - Fix: Ensure `Vector.Data` is populated with float array values
  - Use: `new Vectors { Vector = new Vector { Data = { r.Vector } } }`
- **Invalid Point IDs:** Numeric IDs as strings being assigned to `PointId.Uuid`
  - Fix: Use `PointId.Num` for numeric IDs, `PointId.Uuid` only for valid UUID formats
  - Example: `if (ulong.TryParse(r.Id, out var numericId)) { point.Id = new PointId { Num = numericId }; }`
- **Wrong Collection:** Verifying you're writing to and reading from same collection name
  - Check: `appsettings.Development.json` > `VectorDb:Qdrant:CollectionName` (default: "cars")

#### 2. **Embedding Generation Fails**
**Symptom:** Null vectors in database, warning logs about failed embeddings

**Root Causes & Fixes:**
- **Invalid Embedding Endpoint:** Missing `/openai/v1` path for Azure
  - Fix: Use `https://resource.services.ai.azure.com/openai/v1` (not just domain)
  - Verify: Endpoint matches your Azure AI resource URL
- **Invalid API Key:** Authentication failure
  - Check: Key hasn't expired or been revoked in Azure
  - Move to User Secrets: See Security Considerations section
- **Empty Text:** Generator skips null/whitespace text
  - Fix: Ensure `item.Description`, `item.PortalItem.JsonData`, and specs data are populated

#### 3. **Collection Creation Hangs or Times Out**
**Symptom:** CreateCollection waits indefinitely without response

**Possible Causes:**
- Network connectivity to Qdrant cloud instance
- Qdrant service temporarily unavailable
- Invalid Qdrant credentials or API key

**Mitigation:**
- Check Qdrant dashboard health: https://app.qdrant.tech
- Verify connection details in `appsettings.Development.json`
- Review application logs for network errors

#### 4. **Search Returns No Results**
**Symptom:** Vector search executes but returns empty list

**Investigation Checklist:**
1. Verify collection exists and is named "cars"
   - Check VectorDbScheduleSettings in cache
   - Confirm `CreateCollection` was called successfully
2. Verify points exist in collection
   - Qdrant dashboard shows point count > 0
   - `LoadPortalItems` executed without errors in logs
3. Check search threshold
   - If `ScoreThreshold` is too high (e.g., 0.95), may filter all results
   - Try lowering threshold or setting to null (no filtering)
4. Verify embeddings are 1536 dimensions
   - Model must be `text-embedding-3-small`
   - Wrong dimension embeddings cause search failures

### Logging & Debugging

**Key Log Points:**
- VectorDbSchedulerService: "Collection initialization started"
- QdrantVectorSearchService: "Loaded {Count} records into Qdrant collection"
- LoadPortalItems: "Failed to generate embedding for item {ItemId}"
- UpsertInternal: LogWarning for embedding failures

**Enable Detailed Logging:**
```json
"Logging": {
  "LogLevel": {
    "Portal.Services.VectorSearch": "Debug",
    "Portal.Services.VectorDbSchedulerService": "Debug"
  }
}
```

### Testing Vector Search

**Manual Test Query:**
```csharp
var request = new VectorSearchRequest
{
    CollectionName = "cars",
    Query = "reliable family SUV",
    Limit = 5,
    ScoreThreshold = 0.6f  // Adjust based on desired precision
};
var results = await _vectorSearchService.SearchByVector(request);
```

**Expected Behavior:**
- Returns List<VectorSearchResult> with Id, Score, and Payload
- Score ranges 0.0-1.0 (higher = more similar)
- Payload contains id, name, description from original item

### Data Enrichment Details

**LoadPortalItems Text Combination:**
The embedding text combines three data sources for comprehensive product representation:
1. **Description:** Item Description field (user-friendly text)
2. **PortalItem.JsonData:** Structured product data (title, category attributes)
3. **PortalItemSpecs.JsonData:** All specifications from related PortalItemSpec records
   - Joined with spaces between each spec's data
   - Safely handles null specs or null JsonData entries
   - Multiple specs are aggregated for richer embeddings

**Example Embedding Input:**
```
"Reliable SUV for family trips" "{\\"Title\\":\\"Toyota Fortuner\\",\\"Category\\":\\"Vehicle\\"}" "{\\"Engine\\":\\"2.8L Diesel\\",\\"Transmission\\":\\"Automatic\\"} {\\"Color\\":\\"Silver\\",\\"Capacity\\":\\"7 seats\\"}"
```

## Security Considerations

?? **Important:**
- Move `VectorDb:EmbeddingApiKey` to User Secrets or Azure Key Vault in production
- Never commit API keys to version control
- Use environment-specific configuration files

**Setup User Secrets:**
```powershell
dotnet user-secrets set "VectorDb:EmbeddingApiKey" "your-api-key-here"
```

## Future Enhancements

- Database persistence for audit trail of scheduled updates
- More granular scheduling (cron expressions, specific times of day)
- Update failure recovery with exponential backoff retry
- Metrics/monitoring for scheduler and collection initialization performance
- Support for multiple collections with configurable list
- Batch embedding for bulk data ingestion
- Collection deletion and cleanup operations
- Vector search filters (e.g., by category, price range) in SearchByVectorAsync
- Hybrid search combining vector and traditional keyword results
- User preference learning to improve semantic search quality
- Multi-language semantic search support
