using System;
using MessagePack;

namespace AspNetCoreRateLimit
{
    /// <summary>
    /// Stores the initial access time and the numbers of calls made from that point
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public struct RateLimitCounter
    {
        public DateTime Timestamp { get; set; }

        public TimeSpan Ttl { get; set; }

        public long TotalRequests { get; set; }
    }
}
