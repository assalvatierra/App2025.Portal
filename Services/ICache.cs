namespace Portal.Services
{
    public interface ICache
    {
        // Core CRUD
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        // Bulk Operations
        Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);
        Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        // Atomic / Pattern Helpers
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
