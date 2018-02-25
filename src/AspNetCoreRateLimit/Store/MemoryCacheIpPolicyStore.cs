using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class MemoryCacheIpPolicyStore : IIpPolicyStore
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheIpPolicyStore(IMemoryCache memoryCache, 
            IOptions<IpRateLimitOptions> options = null, 
            IOptions<IpRateLimitPolicies> policies = null)
        {
            _memoryCache = memoryCache;

            //save ip rules defined in appsettings in cache on startup
            if (options != null && options.Value != null && policies != null && policies.Value != null && policies.Value.IpRules != null)
            {
                SetAsync($"{options.Value.IpPolicyPrefix}", policies.Value);
            }
        }

        public Task SetAsync(string id, IpRateLimitPolicies policy)
        {
            _memoryCache.Set(id, policy);
            return Task.CompletedTask;
        }

        public bool Exists(string id)
        {
            return _memoryCache.TryGetValue(id, out IpRateLimitPolicies stored);
        }

        public Task<IpRateLimitPolicies> GetAsync(string id)
        {
            if (_memoryCache.TryGetValue(id, out IpRateLimitPolicies stored))
            {
                return Task.FromResult(stored);
            }

            return Task.FromResult<IpRateLimitPolicies>(null);
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
        }
    }
}
