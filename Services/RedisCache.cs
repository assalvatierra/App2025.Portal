using StackExchange.Redis;
using System.Text.Json;


namespace Portal.Services
{

    public class RedisCache : ICache
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;

        public RedisCache(IConnectionMultiplexer redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetDatabase();
        }

        // Core CRUD
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisValue value = await _db.StringGetAsync(key);
            if (!value.HasValue) return default;

            return JsonSerializer.Deserialize<T>((string)value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiration.HasValue ? new Expiration(DateTime.UtcNow.Add(expiration.Value)) : new Expiration());
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _db.KeyExistsAsync(key);
        }

        // Bulk Operations (Using Redis Pipelines for massive speedup)
        public async Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keyArray = keys.Select(k => (RedisKey)k).ToArray();
            if (keyArray.Length == 0) return new Dictionary<string, T>();

            RedisValue[] values = await _db.StringGetAsync(keyArray);
            var result = new Dictionary<string, T>();

            for (int i = 0; i < keyArray.Length; i++)
            {
                if (values[i].HasValue)
                {
                    var deserialized = JsonSerializer.Deserialize<T>((string)values[i]!);
                    if (deserialized != null)
                    {
                        result[keyArray[i]!] = deserialized;
                    }
                }
            }

            return result;
        }

        public async Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (items.Count == 0) return;

            // Use Redis Batching/Pipelining to execute all sets in one network trip
            var batch = _db.CreateBatch();
            var tasks = new List<Task>();

            foreach (var item in items)
            {
                string json = JsonSerializer.Serialize(item.Value);
                tasks.Add(batch.StringSetAsync(item.Key, json, expiration.HasValue ? new Expiration(DateTime.UtcNow.Add(expiration.Value)) : new Expiration()));
            }

            batch.Execute();
            await Task.WhenAll(tasks);
        }

        public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keyArray = keys.Select(k => (RedisKey)k).ToArray();
            if (keyArray.Length == 0) return;

            await _db.KeyDeleteAsync(keyArray);
        }

        // Atomic Helper (Distributed Lock Implementation)
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            // 1. Try reading from cache first
            T? cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null) return cachedValue;

            // 2. Setup a distributed lock key to prevent Cache Stampede across distributed servers
            string lockKey = $"lock:{key}";
            string lockValue = Guid.NewGuid().ToString();
            TimeSpan lockTimeout = TimeSpan.FromSeconds(10);

            // Try acquiring the lock (Token valid for 10 seconds)
            while (!await _db.LockTakeAsync(lockKey, lockValue, lockTimeout))
            {
                // If failed to get lock, wait briefly and check if the other process filled the cache
                await Task.Delay(100, cancellationToken);

                cachedValue = await GetAsync<T>(key, cancellationToken);
                if (cachedValue != null) return cachedValue;
            }

            try
            {
                // Double-check inside the lock
                cachedValue = await GetAsync<T>(key, cancellationToken);
                if (cachedValue != null) return cachedValue;

                // 3. Cache Miss: Run factory function to query the DB
                T freshValue = await factory();
                await SetAsync(key, freshValue, expiration, cancellationToken);
                return freshValue;
            }
            finally
            {
                // Release the distributed lock safely
                await _db.LockReleaseAsync(lockKey, lockValue);
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Flushes the currently selected database on all connected masters
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsReplica)
                {
                    await server.FlushDatabaseAsync(_db.Database);
                }
            }
        }
    }


}
