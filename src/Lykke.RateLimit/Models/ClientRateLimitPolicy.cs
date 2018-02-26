using System.Collections.Generic;
using MessagePack;

namespace Lykke.RateLimit.Models
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ClientRateLimitPolicy
    {
        public string ClientId { get; set; }
        public List<RateLimitRule> Rules { get; set; }
    }
}
