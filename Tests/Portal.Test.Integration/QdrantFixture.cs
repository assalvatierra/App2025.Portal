using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Qdrant;

namespace Portal.Test.Integration
{
    /// <summary>
    /// xUnit async fixture for Qdrant TestContainer management.
    /// Handles container lifecycle (start/stop) and graceful Docker unavailability detection.
    /// </summary>
    public class QdrantFixture : IAsyncLifetime
    {
        /// <summary>
        /// The Qdrant container instance. Null if Docker is unavailable.
        /// </summary>
        public QdrantContainer Qdrant { get; private set; }

        /// <summary>
        /// Indicates whether Docker is available on the system.
        /// If false, the container was not started and Qdrant is null.
        /// </summary>
        public bool IsDockerAvailable { get; private set; } = true;

        /// <summary>
        /// Initializes the Qdrant TestContainer asynchronously.
        /// Called automatically by xUnit before any tests in a class using this fixture run.
        /// Catches Docker unavailability and sets IsDockerAvailable to false gracefully.
        /// </summary>
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
            catch (Exception ex) when (ex.GetType().Name.Contains("DockerUnavailable"))
            {
                // Docker is not available on this system - set flag and continue gracefully
                IsDockerAvailable = false;
            }
            catch (Exception ex)
            {
                // Log other exceptions but also mark Docker unavailable
                // This handles various Docker-related failures gracefully
                if (ex.Message.Contains("docker", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("container", StringComparison.OrdinalIgnoreCase))
                {
                    IsDockerAvailable = false;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Cleans up and stops the Qdrant TestContainer asynchronously.
        /// Called automatically by xUnit after all tests in a class using this fixture complete.
        /// Only stops the container if it was successfully started.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (Qdrant != null && IsDockerAvailable)
            {
                try
                {
                    await Qdrant.StopAsync();
                }
                catch (Exception)
                {
                    // Suppress exceptions during cleanup to avoid test failures
                    // Container may have already stopped or other cleanup issues
                }
            }
        }
    }
}

