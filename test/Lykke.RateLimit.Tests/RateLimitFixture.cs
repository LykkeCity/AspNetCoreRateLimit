namespace Lykke.RateLimit.Tests
{
    public class RateLimitFixture<TStartup> : RateLimitFixtureBase<TStartup>
        where TStartup : class
    {
        public RateLimitFixture() : base("http://localhost:5000")
        {
        }
    }
}
