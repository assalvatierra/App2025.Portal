using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Qdrant;

public class QdrantFixture : IAsyncLifetime
{
    public QdrantContainer Qdrant { get; private set; }

    public async Task InitializeAsync()
    {
        Qdrant = new QdrantBuilder()
            .WithImage("qdrant/qdrant:latest")
            .WithPortBinding(6333, 6333)   // REST API
            .WithPortBinding(6334, 6334)   // gRPC
            .Build();

        await Qdrant.StartAsync();
    }

    public async Task DisposeAsync() 
    {
        await Qdrant.StopAsync();
    }
}
