using System.Collections.Generic;
using MessagePack;

namespace Lykke.RateLimit.Models
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class IpRateLimitPolicies
    {
        public List<IpRateLimitPolicy> IpRules { get; set; }
    }
}
