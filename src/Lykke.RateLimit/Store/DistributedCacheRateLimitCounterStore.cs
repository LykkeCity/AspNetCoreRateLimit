using System;
using System.Threading.Tasks;
using Lykke.RateLimit.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Lykke.RateLimit.Store
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
            return _memoryCache.SetAsync(id, MessagePack.MessagePackSerializer.Serialize(counter), new DistributedCacheEntryOptions().SetAbsoluteExpiration(expirationTime));
        }

        public async Task<RateLimitCounter> IncrementAsync(string id, TimeSpan expirationTime)
        {
            // this method is not atomic and should not be used

            var entry = await GetAsync(id);

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

            await SetAsync(id, counter, ttl);

            return counter;
        }

        public async Task<RateLimitCounter?> GetAsync(string id)
        {
            var stored = await _memoryCache.GetAsync(id);
            if (stored != null)
            {
                return MessagePack.MessagePackSerializer.Deserialize<RateLimitCounter>(stored);
            }
            return null;
        }
    }
}
