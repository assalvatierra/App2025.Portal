using Microsoft.Extensions.Options;
using Portal.Services.VectorSearch.Models;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
using Qdrant.Client;



namespace Portal.Services.VectorSearch
{
    public static class QdrantServiceCollectionExtensions
    {
        public static IServiceCollection AddQdrant(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<QdrantOptions>(config.GetSection("Qdrant"));

            services.AddSingleton<QdrantClient>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;

                if (string.IsNullOrWhiteSpace(opts.Endpoint))
                    throw new InvalidOperationException("Qdrant:Endpoint is required.");

                return new QdrantClient(
                    address: opts.GetUri(),
                    apiKey: opts.ApiKey
                );
            });

            return services;
        }
    }
}




