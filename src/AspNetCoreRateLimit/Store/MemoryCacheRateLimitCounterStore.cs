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
            var entry = Get(id);
            RateLimitCounter counter;

            lock (_locker)
            {
                counter = entry.HasValue
                        ? new RateLimitCounter
                        {
                            Timestamp = entry.Value.Timestamp,
                            TotalRequests = entry.Value.TotalRequests + 1
                        }
                        : new RateLimitCounter
                        {
                            Timestamp = DateTime.UtcNow,
                            TotalRequests = 1
                        };

                Set(id, counter, expirationTime); 
            }

            return Task.FromResult(counter);
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
