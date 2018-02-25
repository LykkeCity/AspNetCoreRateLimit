using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public interface IRateLimitCounterStore
    {
        Task<RateLimitCounter?> GetAsync(string id);
        Task<RateLimitCounter> IncrementAsync(string id, TimeSpan expirationTime);
    }
}