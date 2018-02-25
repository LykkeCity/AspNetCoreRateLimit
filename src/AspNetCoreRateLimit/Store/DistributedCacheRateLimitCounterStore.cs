using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class DistributedCacheRateLimitCounterStore : IRateLimitCounterStore
    {
        private readonly IDistributedCache _memoryCache;

        public DistributedCacheRateLimitCounterStore(IDistributedCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        private Task SetAsync(string id, RateLimitCounter counter, TimeSpan expirationTime)
        {
            return _memoryCache.SetStringAsync(id, JsonConvert.SerializeObject(counter), new DistributedCacheEntryOptions().SetAbsoluteExpiration(expirationTime));
        }

        public async Task<RateLimitCounter> IncrementAsync(string id, TimeSpan expirationTime)
        {
            // todo: atomic

            var entry = await GetAsync(id);
            var counter = entry.HasValue
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

            await SetAsync(id, counter, expirationTime);

            return counter;
        }

        public async Task<RateLimitCounter?> GetAsync(string id)
        {
            var stored = await _memoryCache.GetStringAsync(id);
            if (!string.IsNullOrEmpty(stored))
            {
                return JsonConvert.DeserializeObject<RateLimitCounter>(stored);
            }
            return null;
        }
    }
}
