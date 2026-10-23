using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Portal.Test.Integration
{
    public class VectorTests : IClassFixture<QdrantFixture>
    {
        private readonly QdrantFixture _fixture;

        public VectorTests(QdrantFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ShouldStoreVector()
        {
            var client = new QdrantClient("localhost", 6334);

            // Example: create a collection
            await client.CreateCollectionAsync("cars", new VectorParams { Size = 4, Distance = Distance.Cosine });

            // Insert a vector
            await client.UpsertAsync("cars", new[]
            {
            new PointStruct
            {
                Id = 1,
                Vectors = new float[] { 0.1f, 0.2f, 0.3f, 0.4f }
            }
        });

            var result = await client.SearchAsync("cars", new float[] { 0.1f, 0.2f, 0.3f, 0.4f });

            Assert.NotEmpty(result);
        }
    }
}
