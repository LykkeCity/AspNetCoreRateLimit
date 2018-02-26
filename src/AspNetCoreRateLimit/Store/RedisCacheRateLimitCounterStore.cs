using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace AspNetCoreRateLimit
{
    public class RedisCacheRateLimitCounterStore : IRateLimitCounterStore
    {
        private readonly IDatabase _redisDatabase;
        private readonly string _instanceName;

        public RedisCacheRateLimitCounterStore(IDatabase redisDatabase, string instanceName = "rate-limits:")
        {
            _redisDatabase = redisDatabase;
            _instanceName = instanceName;
        }

        public async Task<RateLimitCounter> IncrementAsync(string id, TimeSpan expirationTime)
        {
            var requestsCount = await _redisDatabase.StringIncrementAsync(GetKey(id));
            var ttl = await _redisDatabase.KeyTimeToLiveAsync(GetKey(id));
            if (ttl == null)
            {
                ttl = expirationTime;
                await _redisDatabase.KeyExpireAsync(GetKey(id), ttl);
            }

            return new RateLimitCounter
            {
                TotalRequests = requestsCount,
                Ttl = ttl.Value
            };
        }

        public async Task<RateLimitCounter?> GetAsync(string id)
        {
            var stored = await _redisDatabase.StringGetAsync(GetKey(id));
            if (stored.HasValue)
            {
                return new RateLimitCounter
                {
                    TotalRequests = (long)stored,
                    Ttl = (await _redisDatabase.KeyTimeToLiveAsync(GetKey(id))).GetValueOrDefault()
                };
            }
            return null;
        }
        private string GetKey(string id)
        {
            return _instanceName + id;
        }
    }
}
