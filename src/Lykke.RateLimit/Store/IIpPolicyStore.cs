using System.Threading.Tasks;
using Lykke.RateLimit.Models;

namespace Lykke.RateLimit.Store
{
    public interface IIpPolicyStore
    {
        bool Exists(string id);
        Task<IpRateLimitPolicies> GetAsync(string id);
        void Remove(string id);
        Task SetAsync(string id, IpRateLimitPolicies policy);
    }
}