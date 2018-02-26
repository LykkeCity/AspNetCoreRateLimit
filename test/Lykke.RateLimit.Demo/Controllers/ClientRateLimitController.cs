using System.Threading.Tasks;
using Lykke.RateLimit.Models;
using Lykke.RateLimit.Store;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lykke.RateLimit.Demo.Controllers
{
    [Route("api/[controller]")]
    public class ClientRateLimitController : Controller
    {
        private readonly ClientRateLimitOptions _options;
        private readonly IClientPolicyStore _clientPolicyStore;

        public ClientRateLimitController(IOptions<ClientRateLimitOptions> optionsAccessor, IClientPolicyStore clientPolicyStore)
        {
            _options = optionsAccessor.Value;
            _clientPolicyStore = clientPolicyStore;
        }

        [HttpGet]
        public async Task<ClientRateLimitPolicy> Get()
        {
            return await _clientPolicyStore.GetAsync($"{_options.ClientPolicyPrefix}_cl-key-1");
        }

        [HttpPost]
        public async Task Post()
        {
            var id = $"{_options.ClientPolicyPrefix}_cl-key-1";
            var anonPolicy = await _clientPolicyStore.GetAsync(id);
            anonPolicy.Rules.Add(new RateLimitRule
            {
                Endpoint = "*/api/testpolicyupdate",
                Period = "1h",
                Limit = 100
            });
            await _clientPolicyStore.SetAsync(id, anonPolicy);
        }
    }
}
