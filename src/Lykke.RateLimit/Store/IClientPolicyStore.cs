using System.Threading.Tasks;
using Lykke.RateLimit.Models;

namespace Lykke.RateLimit.Store
{
    public interface IClientPolicyStore
    {
        bool Exists(string id);
        Task<ClientRateLimitPolicy> GetAsync(string id);
        void Remove(string id);
        Task SetAsync(string id, ClientRateLimitPolicy policy);
    }
}