using System.Collections.Generic;
using MessagePack;

namespace AspNetCoreRateLimit
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ClientRateLimitPolicy
    {
        public string ClientId { get; set; }
        public List<RateLimitRule> Rules { get; set; }
    }
}
