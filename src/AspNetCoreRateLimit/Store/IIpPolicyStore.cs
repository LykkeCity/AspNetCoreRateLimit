using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public interface IIpPolicyStore
    {
        bool Exists(string id);
        Task<IpRateLimitPolicies> GetAsync(string id);
        void Remove(string id);
        Task SetAsync(string id, IpRateLimitPolicies policy);
    }
}