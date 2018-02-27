using Lykke.RateLimit.Middleware;
using Lykke.RateLimit.Models;
using Lykke.RateLimit.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Lykke.RateLimit.Demo
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            var useInMemoryCache = false;
            if (useInMemoryCache)
            {
                services.AddMemoryCache();
                services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
                services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
                services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            }
            else
            {
                services.AddDistributedRedisCache(options =>
                {
                    options.Configuration = "localhost:6379";
                    options.InstanceName = "rate-limits:";
                });

                var redis = ConnectionMultiplexer.Connect("localhost:6379");
                services.AddSingleton(redis);

                services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
                services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
                services.AddSingleton<IRateLimitCounterStore, RedisCacheRateLimitCounterStore>();
            }

            //configure ip rate limiting middle-ware
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            //configure client rate limiting middleware
            services.Configure<ClientRateLimitOptions>(Configuration.GetSection("ClientRateLimiting"));
            services.Configure<ClientRateLimitPolicies>(Configuration.GetSection("ClientRateLimitPolicies"));

            var opt = new ClientRateLimitOptions();
            ConfigurationBinder.Bind(Configuration.GetSection("ClientRateLimiting"), opt);

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            app.UseIpRateLimiting();
            app.UseClientRateLimiting();

            app.UseMvc();
        }
    }
}
