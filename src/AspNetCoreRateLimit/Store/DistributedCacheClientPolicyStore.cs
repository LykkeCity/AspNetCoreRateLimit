using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AspNetCoreRateLimit
{
    public class DistributedCacheClientPolicyStore : IClientPolicyStore
    {
        private readonly IDistributedCache _memoryCache;

        public DistributedCacheClientPolicyStore(IDistributedCache memoryCache, 
            IOptions<ClientRateLimitOptions> options = null, 
            IOptions<ClientRateLimitPolicies> policies = null)
        {
            _memoryCache = memoryCache;

            //save client rules defined in appsettings in distributed cache on startup
            if (options?.Value != null && policies?.Value?.ClientRules != null)
            {
                foreach (var rule in policies.Value.ClientRules)
                {
                    SetAsync($"{options.Value.ClientPolicyPrefix}_{rule.ClientId}", new ClientRateLimitPolicy { ClientId = rule.ClientId, Rules = rule.Rules });
                }
            }
        }

        public Task SetAsync(string id, ClientRateLimitPolicy policy)
        {
            return _memoryCache.SetAsync(id, MessagePack.MessagePackSerializer.Serialize(policy));
        }

        public bool Exists(string id)
        {
            var stored = _memoryCache.GetString(id);
            return !string.IsNullOrEmpty(stored);
        }

        public async Task<ClientRateLimitPolicy> GetAsync(string id)
        {
            var stored = await _memoryCache.GetAsync(id);
            if (stored != null)
            {
                return MessagePack.MessagePackSerializer.Deserialize<ClientRateLimitPolicy>(stored);
            }
            return null;
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
        }
    }
}
