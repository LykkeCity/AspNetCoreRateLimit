using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class MemoryCacheClientPolicyStore: IClientPolicyStore
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheClientPolicyStore(IMemoryCache memoryCache, 
            IOptions<ClientRateLimitOptions> options = null, 
            IOptions<ClientRateLimitPolicies> policies = null)
        {
            _memoryCache = memoryCache;

            //save client rules defined in appsettings in cache on startup
            if(options?.Value != null && policies?.Value?.ClientRules != null)
            {
                foreach (var rule in policies.Value.ClientRules)
                {
                    SetAsync($"{options.Value.ClientPolicyPrefix}_{rule.ClientId}", new ClientRateLimitPolicy { ClientId = rule.ClientId, Rules = rule.Rules });
                }
            }
        }

        public Task SetAsync(string id, ClientRateLimitPolicy policy)
        {
            _memoryCache.Set(id, policy);
            return Task.CompletedTask;
        }

        public bool Exists(string id)
        {
            return _memoryCache.TryGetValue(id, out ClientRateLimitPolicy stored);
        }

        public Task<ClientRateLimitPolicy> GetAsync(string id)
        {
            if (_memoryCache.TryGetValue(id, out ClientRateLimitPolicy stored))
            {
                return Task.FromResult(stored);
            }

            return Task.FromResult<ClientRateLimitPolicy>(null);
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
        }
    }
}
