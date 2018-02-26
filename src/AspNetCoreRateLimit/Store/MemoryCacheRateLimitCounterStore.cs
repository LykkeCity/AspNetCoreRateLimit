using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class MemoryCacheRateLimitCounterStore : IRateLimitCounterStore
    {
        private readonly IMemoryCache _memoryCache;
        private static readonly object _locker = new object();

        public MemoryCacheRateLimitCounterStore(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        private void Set(string id, RateLimitCounter counter, TimeSpan expirationTime)
        {
            _memoryCache.Set(id, counter, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expirationTime));
        }

        public Task<RateLimitCounter> IncrementAsync(string id, TimeSpan expirationTime)
        {
            lock (_locker)
            {
                var entry = Get(id);

                var ttl = entry.HasValue ? entry.Value.Timestamp + expirationTime - DateTime.UtcNow : expirationTime;
                if (ttl < TimeSpan.Zero)
                {
                    ttl = TimeSpan.FromMilliseconds(1);
                }

                var counter = entry.HasValue
                    ? new RateLimitCounter
                    {
                        Timestamp = entry.Value.Timestamp,
                        TotalRequests = entry.Value.TotalRequests + 1,
                        Ttl = ttl
                    }
                    : new RateLimitCounter
                    {
                        Timestamp = DateTime.UtcNow,
                        TotalRequests = 1,
                        Ttl = ttl
                    };

                Set(id, counter, ttl);

                return Task.FromResult(counter);
            }
        }

        public Task<RateLimitCounter?> GetAsync(string id)
        {
            return Task.FromResult(Get(id));
        }

        private RateLimitCounter? Get(string id)
        {
            if (_memoryCache.TryGetValue(id, out RateLimitCounter stored))
            {
                return stored;
            }

            return null;
        }
    }
}
