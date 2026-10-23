using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Qdrant;

public class QdrantFixture : IAsyncLifetime
{
    public QdrantContainer Qdrant { get; private set; }
    public bool IsDockerAvailable { get; private set; } = true;

    public async Task InitializeAsync()
    {
        try
        {
            Qdrant = new QdrantBuilder()
                .WithImage("qdrant/qdrant:latest")
                .WithPortBinding(6333, 6333)   // REST API
                .WithPortBinding(6334, 6334)   // gRPC
                .Build();

            await Qdrant.StartAsync();
        }
        catch (DockerUnavailableException)
        {
            IsDockerAvailable = false;
        }
    }

    public async Task DisposeAsync() 
    {
        if (Qdrant != null && IsDockerAvailable)
        {
            await Qdrant.StopAsync();
        }
    }
}
