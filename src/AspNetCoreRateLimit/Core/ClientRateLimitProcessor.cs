using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class ClientRateLimitProcessor
    {
        private readonly ClientRateLimitOptions _options;
        private readonly IClientPolicyStore _policyStore;
        private readonly RateLimitCore _core;

        public ClientRateLimitProcessor(ClientRateLimitOptions options,
           IRateLimitCounterStore counterStore,
           IClientPolicyStore policyStore)
        {
            _options = options;
            _policyStore = policyStore;

            _core = new RateLimitCore(false, options, counterStore);
        }

        public async Task<List<RateLimitRule>> GetMatchingRulesAsync(ClientRequestIdentity identity)
        {
            var limits = new List<RateLimitRule>();
            var policy = await _policyStore.GetAsync($"{_options.ClientPolicyPrefix}_{identity.ClientId}");

            if (policy != null)
            {
                if (_options.EnableEndpointRateLimiting)
                {
                    // search for rules with endpoints like "*" and "*:/matching_path"
                    var pathLimits = policy.Rules.Where(l => $"*:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
                    limits.AddRange(pathLimits);

                    // search for rules with endpoints like "matching_verb:/matching_path"
                    var verbLimits = policy.Rules.Where(l => $"{identity.HttpVerb}:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
                    limits.AddRange(verbLimits);
                }
                else
                {
                    //ignore endpoint rules and search for global rules only
                    var genericLimits = policy.Rules.Where(l => l.Endpoint == "*").AsEnumerable();
                    limits.AddRange(genericLimits);
                }
            }

            // get the most restrictive limit for each period 
            limits = limits.GroupBy(l => l.Period).Select(l => l.OrderBy(x => x.Limit)).Select(l => l.First()).ToList();

            // search for matching general rules
            if (_options.GeneralRules != null)
            {
                var matchingGeneralLimits = new List<RateLimitRule>();
                if (_options.EnableEndpointRateLimiting)
                {
                    // search for rules with endpoints like "*" and "*:/matching_path" in general rules
                    var pathLimits = _options.GeneralRules.Where(l => $"*:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
                    matchingGeneralLimits.AddRange(pathLimits);

                    // search for rules with endpoints like "matching_verb:/matching_path" in general rules
                    var verbLimits = _options.GeneralRules.Where(l => $"{identity.HttpVerb}:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
                    matchingGeneralLimits.AddRange(verbLimits);
                }
                else
                {
                    //ignore endpoint rules and search for global rules in general rules
                    var genericLimits = _options.GeneralRules.Where(l => l.Endpoint == "*").AsEnumerable();
                    matchingGeneralLimits.AddRange(genericLimits);
                }

                // get the most restrictive general limit for each period 
                var generalLimits = matchingGeneralLimits.GroupBy(l => l.Period).Select(l => l.OrderBy(x => x.Limit)).Select(l => l.First()).ToList();

                foreach (var generalLimit in generalLimits)
                {
                    // add general rule if no specific rule is declared for the specified period
                    if (!limits.Exists(l => l.Period == generalLimit.Period))
                    {
                        limits.Add(generalLimit);
                    }
                }
            }

            foreach (var item in limits)
            {
                //parse period text into time spans
                item.PeriodTimespan = _core.ConvertToTimeSpan(item.Period);
            }

            limits = limits.OrderBy(l => l.PeriodTimespan).ToList();
            if (_options.StackBlockedRequests)
            {
                limits.Reverse();
            }

            return limits;
        }

        public bool IsWhitelisted(ClientRequestIdentity requestIdentity)
        {
            if (_options.ClientWhitelist != null && _options.ClientWhitelist.Contains(requestIdentity.ClientId))
            {
                return true;
            }

            if (_options.EndpointWhitelist != null && _options.EndpointWhitelist.Any())
            {
                if (_options.EndpointWhitelist.Any(x => $"{requestIdentity.HttpVerb}:{requestIdentity.Path}".ContainsIgnoreCase(x)) ||
                    _options.EndpointWhitelist.Any(x => $"*:{requestIdentity.Path}".ContainsIgnoreCase(x)))
                    return true;
            }

            return false;
        }

        public Task<RateLimitCounter> ProcessRequestAsync(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            return _core.ProcessRequestAsync(requestIdentity, rule);
        }

        public Task<RateLimitHeaders> GetRateLimitHeadersAsync(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            return _core.GetRateLimitHeadersAsync(requestIdentity, rule);
        }

        public string RetryAfterFrom(DateTime timestamp, RateLimitRule rule)
        {
            return _core.RetryAfterFrom(timestamp, rule);
        }
    }
}
