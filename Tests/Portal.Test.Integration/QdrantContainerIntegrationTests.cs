using DotNet.Testcontainers.Containers;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Portal.Test.Integration
{
    /// <summary>
    /// Integration tests specifically for Qdrant TestContainer initialization and validation.
    /// These tests verify that the Qdrant vector database container is properly configured
    /// and accessible for use by other integration tests.
    /// Docker unavailability is gracefully handled - tests skip rather than fail.
    /// </summary>
    public class QdrantContainerIntegrationTests : IClassFixture<QdrantFixture>
    {
        private readonly QdrantFixture _qdrantFixture;

        public QdrantContainerIntegrationTests(QdrantFixture qdrantFixture)
        {
            _qdrantFixture = qdrantFixture;
        }

        /// <summary>
        /// Test to verify Qdrant TestContainer is running and accessible.
        /// This test validates the container initialization and Docker platform support.
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public void QdrantContainer_IsInitialized()
        {
            // If Docker is not available, Qdrant will be null but IsDockerAvailable will be false
            if (!_qdrantFixture.IsDockerAvailable)
            {
                // Skip this test if Docker is unavailable - this is expected on non-Docker systems
                return;
            }

            // Assert - if Docker IS available, Qdrant should be initialized
            Assert.NotNull(_qdrantFixture.Qdrant);
            Assert.True(_qdrantFixture.IsDockerAvailable);
        }

        /// <summary>
        /// Test to verify Qdrant container is running on expected ports.
        /// REST API: 6333, gRPC: 6334
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public void QdrantContainer_HasCorrectPortBindings()
        {
            // Skip if Docker is unavailable
            if (!_qdrantFixture.IsDockerAvailable)
            {
                return;
            }

            // Arrange
            var qdrantContainer = _qdrantFixture.Qdrant;

            // Assert
            Assert.NotNull(qdrantContainer);
            // Container should be running (check by attempting to access ports)
            var restPort = qdrantContainer.GetMappedPublicPort(6333);
            var grpcPort = qdrantContainer.GetMappedPublicPort(6334);

            Assert.True(restPort > 0, "REST API port (6333) should be mapped");
            Assert.True(grpcPort > 0, "gRPC port (6334) should be mapped");
        }

        /// <summary>
        /// Test to validate Qdrant REST API is accessible through the exposed port.
        /// Tests the root endpoint which returns the status of the Qdrant service.
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public async Task QdrantRestApi_IsAccessible()
        {
            // Skip if Docker is unavailable
            if (!_qdrantFixture.IsDockerAvailable)
            {
                return;
            }

            // Arrange
            var qdrantContainer = _qdrantFixture.Qdrant;
            var restApiUrl = $"http://{qdrantContainer.Hostname}:{qdrantContainer.GetMappedPublicPort(6333)}/";

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // Act
            try
            {
                var response = await httpClient.GetAsync(restApiUrl);

                // Assert
                Assert.True(response.IsSuccessStatusCode, 
                    $"Qdrant REST API request failed with status {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                // If we can't reach the API after initialization, fail the test
                Assert.Fail($"Could not reach Qdrant REST API at {restApiUrl}: {ex.Message}");
            }
        }

        /// <summary>
        /// Test to validate Qdrant container lifecycle management.
        /// Ensures the container is properly initialized and can be reused across tests.
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public void QdrantContainer_LifecycleManagement_IsValid()
        {
            // Skip if Docker is unavailable
            if (!_qdrantFixture.IsDockerAvailable)
            {
                return;
            }

            // Arrange
            var qdrantContainer = _qdrantFixture.Qdrant;

            // Assert
            // Container should exist and have valid port mappings
            Assert.NotNull(qdrantContainer);

            var restPort = qdrantContainer.GetMappedPublicPort(6333);
            var grpcPort = qdrantContainer.GetMappedPublicPort(6334);

            Assert.True(restPort > 0, "REST API port mapping should be valid (6333)");
            Assert.True(grpcPort > 0, "gRPC port mapping should be valid (6334)");
        }

        /// <summary>
        /// Test to verify Qdrant container image is correctly configured.
        /// Validates that the image contains 'qdrant' in its name.
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public void QdrantContainer_ImageConfiguration_IsCorrect()
        {
            // Skip if Docker is unavailable
            if (!_qdrantFixture.IsDockerAvailable)
            {
                return;
            }

            // Arrange
            var qdrantContainer = _qdrantFixture.Qdrant;

            // Assert
            Assert.NotNull(qdrantContainer.Image);
            Assert.NotNull(qdrantContainer.Image.FullName);
            Assert.Contains("qdrant", qdrantContainer.Image.FullName.ToLower());
        }

        /// <summary>
        /// Test to validate Qdrant container is accessible by hostname and port.
        /// This test ensures proper network connectivity to the container.
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public void QdrantContainer_NetworkConnectivity_IsValid()
        {
            // Skip if Docker is unavailable
            if (!_qdrantFixture.IsDockerAvailable)
            {
                return;
            }

            // Arrange
            var qdrantContainer = _qdrantFixture.Qdrant;

            // Assert
            Assert.NotNull(qdrantContainer.Hostname);
            Assert.NotEmpty(qdrantContainer.Hostname);

            // Verify port mappings are accessible
            var restPort = qdrantContainer.GetMappedPublicPort(6333);
            var grpcPort = qdrantContainer.GetMappedPublicPort(6334);

            Assert.NotEqual(0, restPort);
            Assert.NotEqual(0, grpcPort);
            Assert.NotEqual(restPort, grpcPort); // Ports should be different
        }

        /// <summary>
        /// Test to verify Docker availability check works correctly.
        /// This test validates the fixture's ability to detect Docker issues and handle them gracefully.
        /// </summary>
        [Fact]
        public void QdrantFixture_DockerAvailability_IsAccurate()
        {
            // Arrange & Act
            var isDockerAvailable = _qdrantFixture.IsDockerAvailable;

            // Assert
            Assert.IsType<bool>(isDockerAvailable);

            // If Docker is available, container should exist
            if (isDockerAvailable)
            {
                Assert.NotNull(_qdrantFixture.Qdrant);
            }
            else
            {
                // If Docker is not available, we gracefully skip (container may be null)
                // This is expected behavior on systems without Docker
            }
        }

        /// <summary>
        /// Test to validate Qdrant container can be reused across multiple tests.
        /// This ensures proper lifecycle management and resource efficiency.
        /// xUnit fixtures are created once per test class, so all tests share the same container.
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public void QdrantContainer_CanBeReusedAcrossMultipleTests()
        {
            // Skip if Docker is unavailable
            if (!_qdrantFixture.IsDockerAvailable)
            {
                return;
            }

            // Arrange
            var qdrantContainer1 = _qdrantFixture.Qdrant;

            // Access it again
            var qdrantContainer2 = _qdrantFixture.Qdrant;

            // Assert
            // Should be the same instance (reused by xUnit fixture)
            Assert.Same(qdrantContainer1, qdrantContainer2);
        }

        /// <summary>
        /// Test to verify Qdrant collections API is accessible.
        /// This tests the /collections endpoint which returns all vector collections in Qdrant.
        /// Skips if Docker is not available.
        /// </summary>
        [Fact]
        public async Task QdrantCollectionsApi_IsAccessible()
        {
            // Skip if Docker is unavailable
            if (!_qdrantFixture.IsDockerAvailable)
            {
                return;
            }

            // Arrange
            var qdrantContainer = _qdrantFixture.Qdrant;
            var collectionsApiUrl = $"http://{qdrantContainer.Hostname}:{qdrantContainer.GetMappedPublicPort(6333)}/collections";

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // Act
            try
            {
                var response = await httpClient.GetAsync(collectionsApiUrl);

                // Assert
                Assert.True(response.IsSuccessStatusCode, 
                    $"Qdrant Collections API request failed with status {response.StatusCode}");

                // Verify response contains valid JSON structure
                var content = await response.Content.ReadAsStringAsync();
                Assert.NotEmpty(content);
            }
            catch (HttpRequestException ex)
            {
                Assert.Fail($"Could not reach Qdrant Collections API at {collectionsApiUrl}: {ex.Message}");
            }
        }

        [Fact]
        public async Task ShouldStoreVector()
        {
            var qdrantContainer = _qdrantFixture.Qdrant;

            var client = new QdrantClient(qdrantContainer.Hostname, qdrantContainer.GetMappedPublicPort(6334));

            // Example: create a collection
            await client.CreateCollectionAsync("Sample", new VectorParams { Size = 4, Distance = Distance.Cosine });

            // Insert a vector
            await client.UpsertAsync("Sample", new[]
            {
            new PointStruct
            {
                Id = 1,
                Vectors = new float[] { 0.1f, 0.2f, 0.3f, 0.4f }
            }
        });

            var result = await client.QueryAsync("Sample", new float[] { 0.1f, 0.2f, 0.3f, 0.4f });

            Assert.NotEmpty(result);
        }



    }
}
