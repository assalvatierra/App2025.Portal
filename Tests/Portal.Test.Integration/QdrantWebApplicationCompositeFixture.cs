using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portal.Services.VectorSearch.Models;
using Testcontainers.Qdrant;

namespace Portal.Test.Integration
{
    /// <summary>
    /// Composite fixture combining Qdrant TestContainer and WebApplicationFactory.
    /// Manages both the Qdrant container lifecycle and the test web application in a single fixture,
    /// eliminating circular dependency issues when using multiple IClassFixture declarations.
    /// Implements IAsyncLifetime to handle async initialization and cleanup.
    /// </summary>
    public class QdrantWebApplicationCompositeFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private QdrantContainer? _qdrant;
        private string _qdrantEndpoint = "localhost:6334";

        /// <summary>
        /// Indicates whether Docker is available on the system.
        /// If false, the container was not started and tests will use fallback configuration.
        /// </summary>
        public bool IsDockerAvailable { get; private set; } = true;

        /// <summary>
        /// Initializes the Qdrant TestContainer asynchronously.
        /// Called automatically by xUnit before any tests using this fixture run.
        /// Handles Docker unavailability gracefully without failing tests.
        /// Must complete before CreateClient() is called by tests.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _qdrant = new QdrantBuilder("qdrant/qdrant:latest")
                    .WithImage("qdrant/qdrant:latest")
                    .WithPortBinding(6333, 6333)   // REST API
                    .WithPortBinding(6334, 6334)   // gRPC
                    .Build();

                await _qdrant.StartAsync();

                // Store the actual container endpoint
                _qdrantEndpoint = _qdrant.GetGrpcConnectionString();
                IsDockerAvailable = true;
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("DockerUnavailable"))
            {
                // Docker is not available on this system - set flag and continue gracefully
                IsDockerAvailable = false;
                _qdrantEndpoint = "localhost:6334"; // Fallback
            }
            catch (Exception ex)
            {
                // Log other exceptions but also mark Docker unavailable
                // This handles various Docker-related failures gracefully
                if (ex.Message.Contains("docker", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("container", StringComparison.OrdinalIgnoreCase))
                {
                    IsDockerAvailable = false;
                    _qdrantEndpoint = "localhost:6334"; // Fallback
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Overrides ConfigureWebHost to inject Qdrant configuration.
        /// This is called when the host is being built, ensuring Qdrant options are configured.
        /// Uses the endpoint determined during InitializeAsync.
        /// </summary>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Configure QdrantOptions with the actual endpoint (from container or fallback)
                services.Configure<QdrantOptions>(opts =>
                {
                    opts.Endpoint = _qdrantEndpoint;
                    opts.UseHttps = false;
                });
            });

            base.ConfigureWebHost(builder);
        }

        /// <summary>
        /// Cleans up and stops the Qdrant TestContainer asynchronously.
        /// Called automatically by xUnit after all tests using this fixture complete.
        /// Only stops the container if it was successfully started.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_qdrant != null && IsDockerAvailable)
            {
                try
                {
                    await _qdrant.StopAsync();
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
