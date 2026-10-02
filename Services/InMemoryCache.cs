using Microsoft.Extensions.Caching.Memory;
//using Portal.Services;
using System.Collections.Concurrent;
//using System.Threading;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

namespace Portal.Services
{

    public class InMemoryCache : ICache
    {
        private readonly IMemoryCache _memoryCache;
        // Thread-safe locks mapped to keys to prevent cache stampedes
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public InMemoryCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        // Core CRUD
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _memoryCache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }

            _memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _memoryCache.Remove(key);
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool exists = _memoryCache.TryGetValue(key, out _);
            return Task.FromResult(exists);
        }

        // Bulk Operations
        public Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new Dictionary<string, T>();

            foreach (var key in keys)
            {
                if (_memoryCache.TryGetValue(key, out T? value) && value != null)
                {
                    result[key] = value;
                }
            }

            return Task.FromResult<IDictionary<string, T>>(result);
        }

        public Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }

            foreach (var item in items)
            {
                _memoryCache.Set(item.Key, item.Value, options);
            }

            return Task.CompletedTask;
        }

        public Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }

            return Task.CompletedTask;
        }

        // Atomic Helper (Thread-Safe Cache-Aside)
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            // Double-checked locking pattern
            if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            // Get or create a lock specific to this key
            var myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await myLock.WaitAsync(cancellationToken);

            try
            {
                // Check again inside the lock
                if (_memoryCache.TryGetValue(key, out T? secondaryValue) && secondaryValue != null)
                {
                    return secondaryValue;
                }

                // Fetch from database/external source via the factory
                T freshValue = await factory();
                await SetAsync(key, freshValue, expiration, cancellationToken);
                return freshValue;
            }
            finally
            {
                myLock.Release();
                // Optional cleanup for the lock entry if no one else is waiting
                if (myLock.CurrentCount == 1)
                {
                    _locks.TryRemove(key, out _);
                }
            }
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note: IMemoryCache doesn't have a native Clear() method. 
            // Compacting by 1.0 (100%) forces eviction of all entries.
            if (_memoryCache is MemoryCache concreteCache)
            {
                concreteCache.Compact(1.0);
            }

            return Task.CompletedTask;
        }
    }
}