using System.Collections.Generic;

namespace Lykke.RateLimit.Models
{
    public class ClientRateLimitPolicies
    {
        public List<ClientRateLimitPolicy> ClientRules { get; set; }
    }
}
