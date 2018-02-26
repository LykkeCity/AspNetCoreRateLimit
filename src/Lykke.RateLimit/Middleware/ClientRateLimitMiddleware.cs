using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Lykke.RateLimit.Core;
using Lykke.RateLimit.Models;
using Lykke.RateLimit.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lykke.RateLimit.Middleware
{
    public class ClientRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ClientRateLimitMiddleware> _logger;
        private readonly ClientRateLimitProcessor _processor;
        private readonly ClientRateLimitOptions _options;

        public ClientRateLimitMiddleware(RequestDelegate next,
            IOptions<ClientRateLimitOptions> options,
            IRateLimitCounterStore counterStore,
            IClientPolicyStore policyStore,
            ILogger<ClientRateLimitMiddleware> logger
            )
        {
            _next = next;
            _options = options.Value;
            _logger = logger;

            _processor = new ClientRateLimitProcessor(_options, counterStore, policyStore);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // check if rate limiting is enabled
            if (_options == null)
            {
                await _next.Invoke(httpContext);
                return;
            }

            // compute identity from request
            var identity = SetIdentity(httpContext);

            // check white list
            if (_processor.IsWhitelisted(identity))
            {
                await _next.Invoke(httpContext);
                return;
            }

            var rules = await _processor.GetMatchingRulesAsync(identity);

            foreach (var rule in rules)
            {
                if (rule.Limit > 0)
                {
                    // increment counter
                    var counter = await _processor.ProcessRequestAsync(identity, rule);

                    // check if limit is reached
                    if (counter.TotalRequests > rule.Limit)
                    {
                        // log blocked request
                        LogBlockedRequest(httpContext, identity, counter, rule);

                        // break execution
                        await ReturnQuotaExceededResponse(httpContext, rule, counter.Ttl);
                        return;
                    }
                }
                // if limit is zero or less, block the request.
                else
                {
                    // process request count
                    var counter = await _processor.ProcessRequestAsync(identity, rule);

                    // log blocked request
                    LogBlockedRequest(httpContext, identity, counter, rule);

                    // break execution (TimeSpan.MaxValue used to represent infinity)
                    await ReturnQuotaExceededResponse(httpContext, rule, TimeSpan.MaxValue);
                    return;
                }
            }

            //set X-Rate-Limit headers for the longest period
            if (rules.Any() && !_options.DisableRateLimitHeaders)
            {
                var rule = rules.OrderByDescending(x => x.PeriodTimespan.Value).First();
                var headers = await _processor.GetRateLimitHeadersAsync(identity, rule);
                headers.Context = httpContext;

                httpContext.Response.OnStarting(SetRateLimitHeaders, state: headers);
            }

            await _next.Invoke(httpContext);
        }

        public virtual ClientRequestIdentity SetIdentity(HttpContext httpContext)
        {
            var clientId = "anon";
            if (httpContext.Request.Headers.Keys.Contains(_options.ClientIdHeader, StringComparer.CurrentCultureIgnoreCase))
            {
                clientId = httpContext.Request.Headers[_options.ClientIdHeader].First();
            }

            return new ClientRequestIdentity
            {
                Path = httpContext.Request.Path.ToString().ToLowerInvariant(),
                HttpVerb = httpContext.Request.Method.ToLowerInvariant(),
                ClientId = clientId
            };
        }

        private Task ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitRule rule, TimeSpan retryAfter)
        {
            var message = string.IsNullOrEmpty(_options.QuotaExceededMessage) ? $"API calls quota exceeded! maximum admitted {rule.Limit} per {rule.Period}." : _options.QuotaExceededMessage;

            if (!_options.DisableRateLimitHeaders)
            {
                httpContext.Response.Headers["Retry-After"] = ((long)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }

            httpContext.Response.StatusCode = _options.HttpStatusCode;
            return httpContext.Response.WriteAsync(message);
        }

        private void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule)
        {
            _logger.LogInformation($"Request {identity.HttpVerb}:{identity.Path} from ClientId {identity.ClientId} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.TotalRequests}. Blocked by rule {rule.Endpoint}, TraceIdentifier {httpContext.TraceIdentifier}.");
        }

        private Task SetRateLimitHeaders(object rateLimitHeaders)
        {
            var headers = (RateLimitHeaders)rateLimitHeaders;

            headers.Context.Response.Headers["X-Rate-Limit-Limit"] = headers.Limit;
            headers.Context.Response.Headers["X-Rate-Limit-Remaining"] = headers.Remaining;
            headers.Context.Response.Headers["X-Rate-Limit-Reset"] = headers.Reset;

            return Task.CompletedTask;
        }
    }
}
