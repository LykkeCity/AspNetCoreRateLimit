using System;
using System.Globalization;
using System.Threading.Tasks;
using Lykke.RateLimit.Models;
using Lykke.RateLimit.Store;

namespace Lykke.RateLimit.Core
{
    public class RateLimitCore
    {
        private readonly RateLimitCoreOptions _options;
        private readonly IRateLimitCounterStore _counterStore;
        private readonly bool _ipRateLimiting;

        public RateLimitCore(bool ipRateLimiting,
            RateLimitCoreOptions options,
           IRateLimitCounterStore counterStore)
        {
            _ipRateLimiting = ipRateLimiting;
            _options = options;
            _counterStore = counterStore;
        }

        private string ComputeCounterKey(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            var key = _ipRateLimiting ?
                $"{_options.RateLimitCounterPrefix}_{requestIdentity.ClientIp}_{rule.Period}" :
                $"{_options.RateLimitCounterPrefix}_{requestIdentity.ClientId}_{rule.Period}";

            if (_options.EnableEndpointRateLimiting)
            {
                key += $"_{requestIdentity.HttpVerb}_{requestIdentity.Path}";

                // TODO: consider using the rule endpoint as key, this will allow to rate limit /api/values/1 and api/values/2 under same counter
                //key += $"_{rule.Endpoint}";
            }

            var idBytes = System.Text.Encoding.UTF8.GetBytes(key);

            byte[] hashBytes;

            using (var algorithm = System.Security.Cryptography.SHA1.Create())
            {
                hashBytes = algorithm.ComputeHash(idBytes);
            }

            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public async Task<RateLimitCounter> ProcessRequestAsync(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            var counterId = ComputeCounterKey(requestIdentity, rule);

            return await _counterStore.IncrementAsync(counterId, rule.PeriodTimespan.Value);
        }

        public async Task<RateLimitHeaders> GetRateLimitHeadersAsync(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            var headers = new RateLimitHeaders();
            var counterId = ComputeCounterKey(requestIdentity, rule);
            var entry = await _counterStore.GetAsync(counterId);
            if (entry.HasValue)
            {
                headers.Reset = (DateTime.UtcNow + entry.Value.Ttl).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo);
                headers.Limit = rule.Period;
                headers.Remaining = (rule.Limit - entry.Value.TotalRequests).ToString();
            }
            else
            {
                headers.Reset = (DateTime.UtcNow + ConvertToTimeSpan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo);
                headers.Limit = rule.Period;
                headers.Remaining = rule.Limit.ToString();
            }

            return headers;
        }

        public TimeSpan ConvertToTimeSpan(string timeSpan)
        {
            var l = timeSpan.Length - 1;
            var value = timeSpan.Substring(0, l);
            var type = timeSpan.Substring(l, 1);

            switch (type)
            {
                case "d": return TimeSpan.FromDays(double.Parse(value));
                case "h": return TimeSpan.FromHours(double.Parse(value));
                case "m": return TimeSpan.FromMinutes(double.Parse(value));
                case "s": return TimeSpan.FromSeconds(double.Parse(value));
                default: throw new FormatException($"{timeSpan} can't be converted to TimeSpan, unknown type {type}");
            }
        }
    }
}
