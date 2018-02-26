using System.Collections.Generic;
using MessagePack;

namespace AspNetCoreRateLimit
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class IpRateLimitPolicies
    {
        public List<IpRateLimitPolicy> IpRules { get; set; }
    }
}
