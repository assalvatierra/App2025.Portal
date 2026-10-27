using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Portal.Services.VectorSearch.Models;
using Qdrant.Client;

namespace Portal.Test.Integration
{
    /// <summary>
    /// Custom WebApplicationFactory that configures Qdrant options for integration testing.
    /// Overrides the production Qdrant configuration to use the test container endpoint.
    /// Removes the singleton QdrantClient and re-creates it with test settings to ensure
    /// the correct endpoint is used.
    /// </summary>
    public class QdrantWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly QdrantFixture _qdrantFixture;

        public QdrantWebApplicationFactory(QdrantFixture qdrantFixture)
        {
            _qdrantFixture = qdrantFixture;

            // Configure web host builder for test Qdrant endpoint
            WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing QdrantClient singleton from production configuration
                    var qdrantClientDescriptor = services.FirstOrDefault(d => 
                        d.ServiceType == typeof(QdrantClient));
                    if (qdrantClientDescriptor != null)
                    {
                        services.Remove(qdrantClientDescriptor);
                    }

                    // Override QdrantOptions for testing with the test container endpoint
                    services.Configure<QdrantOptions>(opts =>
                    {
                        if (_qdrantFixture.Qdrant != null && _qdrantFixture.IsDockerAvailable)
                        {
                            opts.Endpoint = _qdrantFixture.Qdrant.GetConnectionString();
                        }
                        else
                        {
                            // Fallback to localhost if Docker is unavailable
                            opts.Endpoint = "localhost:6334";
                        }
                        opts.UseHttps = false;  // Test container uses HTTP, not HTTPS
                    });

                    // Re-register QdrantClient with test-specific configuration
                    services.AddSingleton<QdrantClient>(sp =>
                    {
                        var opts = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
                        return new QdrantClient(
                            address: opts.GetUri(),
                            apiKey: opts.ApiKey
                        );
                    });
                });
            });
        }
    }
}
