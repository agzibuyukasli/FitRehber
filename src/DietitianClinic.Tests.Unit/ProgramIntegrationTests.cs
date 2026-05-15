using DietitianClinic.API.Services;
using DietitianClinic.DataAccess.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Xunit;

namespace DietitianClinic.Tests.Unit;

/// <summary>
/// Program.cs'deki satırları kapsamak için minimal integration test.
/// InMemory veritabanı ile çalışır — SQL Server veya Redis gerekmez.
/// </summary>
public class ProgramIntegrationTests : IClassFixture<ProgramIntegrationTests.TestWebApp>
{
    private readonly TestWebApp _factory;
    private readonly HttpClient _client;

    public ProgramIntegrationTests(TestWebApp factory)
    {
        _factory = factory;
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

    // ── /api/dashboard/summary ────────────────────────────────────────────

    [Fact]
    public async Task DashboardSummary_Returns401_WhenNotAuthenticated()
    {
        // RequireAuthorization() → 401 when no token
        var response = await _client.GetAsync("/api/dashboard/summary");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DashboardSummary_Returns200_WhenAuthenticatedAsAdmin()
    {
        // isDietitian=false branch → totalDietitians count is fetched from DB
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateJwtToken("42", "Admin"));
        var response = await client.GetAsync("/api/dashboard/summary");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DashboardSummary_Returns200_WhenAuthenticatedAsDietitian()
    {
        // isDietitian=true && currentUserId.HasValue → filters queries by userId
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateJwtToken("5", "Dietitian"));
        var response = await client.GetAsync("/api/dashboard/summary");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── Swagger (Development environment) ────────────────────────────────

    [Fact]
    public async Task Swagger_IsAvailable_InDevelopmentEnvironment()
    {
        // Covers the if (app.Environment.IsDevelopment()) block (UseSwagger + UseSwaggerUI)
        await using var devFactory = new DevWebApp();
        using var client = devFactory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── JWT helper ────────────────────────────────────────────────────────

    private static string GenerateJwtToken(string userId, string role)
    {
        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("IntegrationTestSecretKey_MustBeAtLeast32Chars!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
        };
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Test factories ────────────────────────────────────────────────────

    /// <summary>
    /// Program.cs startup kodunu InMemory DB ile çalıştıran test factory (Testing env).
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
                    ["Sentry:Dsn"]                     = "",
                    ["Elasticsearch:Url"]              = "http://localhost:9999",
                    ["Redis:ConnectionString"]         = "localhost:0",
                    ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=_test_nonexistent_db_;"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DietitianClinicDbContext>>();
                services.RemoveAll<DietitianClinicDbContext>();
                services.AddDbContext<DietitianClinicDbContext>(opt =>
                    opt.UseInMemoryDatabase("IntegrationTestDb_" + Guid.NewGuid()));

                services.RemoveAll<ICacheService>();
                services.AddSingleton<ICacheService, TestNullCacheService>();
            });
        }
    }

    /// <summary>
    /// Development environment factory — Swagger middleware'ini aktive eder.
    /// </summary>
    private sealed class DevWebApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"]          = "IntegrationTestSecretKey_MustBeAtLeast32Chars!",
                    ["Jwt:Issuer"]                     = "TestIssuer",
                    ["Jwt:Audience"]                   = "TestAudience",
                    ["Jwt:AccessTokenExpiryMinutes"]   = "60",
                    ["Sentry:Dsn"]                     = "",
                    ["Elasticsearch:Url"]              = "http://localhost:9999",
                    ["Redis:ConnectionString"]         = "localhost:0",
                    ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=_test_nonexistent_db_;"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DietitianClinicDbContext>>();
                services.RemoveAll<DietitianClinicDbContext>();
                services.AddDbContext<DietitianClinicDbContext>(opt =>
                    opt.UseInMemoryDatabase("DevTestDb_" + Guid.NewGuid()));

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
