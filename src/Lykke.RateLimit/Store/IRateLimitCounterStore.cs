using System;
using System.Threading.Tasks;
using Lykke.RateLimit.Models;

namespace Lykke.RateLimit.Store
{
    public interface IRateLimitCounterStore
    {
        Task<RateLimitCounter?> GetAsync(string id);
        Task<RateLimitCounter> IncrementAsync(string id, TimeSpan expirationTime);
    }
}