using System.Collections.Generic;
using MessagePack;

namespace AspNetCoreRateLimit
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class IpRateLimitPolicy
    {
        public string Ip { get; set; }
        public List<RateLimitRule> Rules { get; set; }
    }
}
