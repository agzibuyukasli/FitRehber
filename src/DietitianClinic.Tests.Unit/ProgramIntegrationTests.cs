using DietitianClinic.API.Services;
using DietitianClinic.DataAccess.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DietitianClinic.Tests.Unit;

/// <summary>
/// Program.cs'deki satırları kapsamak için minimal integration test.
/// InMemory veritabanı ile çalışır — SQL Server veya Redis gerekmez.
/// </summary>
public class ProgramIntegrationTests : IClassFixture<ProgramIntegrationTests.TestWebApp>
{
    private readonly HttpClient _client;

    public ProgramIntegrationTests(TestWebApp factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthStatus_Returns200_WhenAppIsRunning()
    {
        var response = await _client.GetAsync("/api/health/status");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthInfo_Returns200_WhenAppIsRunning()
    {
        var response = await _client.GetAsync("/api/health/info");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Program.cs startup kodunu InMemory DB ile çalıştıran test factory.
    /// SQL Server/Redis/Elasticsearch başarısız olursa uygulama yine de ayağa kalkar.
    /// </summary>
    public sealed class TestWebApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"]          = "IntegrationTestSecretKey_MustBeAtLeast32Chars!",
                    ["Jwt:Issuer"]                     = "TestIssuer",
                    ["Jwt:Audience"]                   = "TestAudience",
                    ["Jwt:AccessTokenExpiryMinutes"]   = "60",
                    ["Sentry:Dsn"]                     = "",    // Sentry devre dışı
                    ["Elasticsearch:Url"]              = "http://localhost:9999", // erişilmez, log error olarak geçer
                    ["Redis:ConnectionString"]         = "localhost:0",  // bağlanamaz → NullCacheService fallback
                    ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=_test_nonexistent_db_;"
                });
            });

            builder.ConfigureServices(services =>
            {
                // SQL Server yerine InMemory — migration'lar catch bloğunda hata verir ama uygulama ayakta kalır
                services.RemoveAll<DbContextOptions<DietitianClinicDbContext>>();
                services.RemoveAll<DietitianClinicDbContext>();
                services.AddDbContext<DietitianClinicDbContext>(opt =>
                    opt.UseInMemoryDatabase("IntegrationTestDb_" + Guid.NewGuid()));

                // NullCacheService internal olduğu için test projesinden erişilemez;
                // aynı no-op davranışı lokal stub ile sağlanır.
                services.RemoveAll<ICacheService>();
                services.AddSingleton<ICacheService, TestNullCacheService>();
            });
        }
    }

    private sealed class TestNullCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) => Task.CompletedTask;
        public Task RemoveAsync(string key) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix) => Task.CompletedTask;
    }
}
