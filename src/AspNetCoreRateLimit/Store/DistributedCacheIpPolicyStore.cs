using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AspNetCoreRateLimit
{
    public class DistributedCacheIpPolicyStore : IIpPolicyStore
    {
        private readonly IDistributedCache _memoryCache;

        public DistributedCacheIpPolicyStore(IDistributedCache memoryCache,
            IOptions<IpRateLimitOptions> options = null,
            IOptions<IpRateLimitPolicies> policies = null)
        {
            _memoryCache = memoryCache;

            //save ip rules defined in appsettings in distributed cache on startup
            if (options?.Value != null && policies?.Value?.IpRules != null)
            {
                SetAsync($"{options.Value.IpPolicyPrefix}", policies.Value);

            }
        }

        public Task SetAsync(string id, IpRateLimitPolicies policy)
        {
            return _memoryCache.SetAsync(id, MessagePack.MessagePackSerializer.Serialize(policy));
        }

        public bool Exists(string id)
        {
            var stored = _memoryCache.GetString(id);
            return !string.IsNullOrEmpty(stored);
        }

        public async Task<IpRateLimitPolicies> GetAsync(string id)
        {
            var stored = await _memoryCache.GetAsync(id);
            if (stored != null)
            {
                return MessagePack.MessagePackSerializer.Deserialize<IpRateLimitPolicies>(stored);
            }
            return null;
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
        }
    }
}
