using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public interface IClientPolicyStore
    {
        bool Exists(string id);
        Task<ClientRateLimitPolicy> GetAsync(string id);
        void Remove(string id);
        Task SetAsync(string id, ClientRateLimitPolicy policy);
    }
}