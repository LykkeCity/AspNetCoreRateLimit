using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AspNetCoreRateLimit.Demo.Controllers
{
    [Route("api/[controller]")]
    public class IpRateLimitController : Controller
    {
        private readonly IpRateLimitOptions _options;
        private readonly IIpPolicyStore _ipPolicyStore;

        public IpRateLimitController(IOptions<IpRateLimitOptions> optionsAccessor, IIpPolicyStore ipPolicyStore)
        {
            _options = optionsAccessor.Value;
            _ipPolicyStore = ipPolicyStore;
        }

        [HttpGet]
        public async Task<IpRateLimitPolicies> Get()
        {
            return await _ipPolicyStore.GetAsync(_options.IpPolicyPrefix);
        }

        [HttpPost]
        public async Task Post()
        {
            var pol = await _ipPolicyStore.GetAsync(_options.IpPolicyPrefix);

            pol.IpRules.Add(new IpRateLimitPolicy
            {
                Ip = "8.8.4.4",
                Rules = new List<RateLimitRule>(new RateLimitRule[] {
                    new RateLimitRule {
                        Endpoint = "*:/api/testupdate",
                        Limit = 100,
                        Period = "1d" }
                })
            });

            await _ipPolicyStore.SetAsync(_options.IpPolicyPrefix, pol);
        }
    }
}
