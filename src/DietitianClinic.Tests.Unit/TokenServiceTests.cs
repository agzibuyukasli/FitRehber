using DietitianClinic.Business.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DietitianClinic.Tests.Unit;

public class TokenServiceTests
{
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]          = "UnitTestSecretKey_MustBeAtLeast32Characters!",
                ["Jwt:Issuer"]                     = "TestIssuer",
                ["Jwt:Audience"]                   = "TestAudience",
                ["Jwt:AccessTokenExpiryMinutes"]   = "60"
            })
            .Build();

        _sut = new TokenService(config);
    }

    [Fact]
    public async Task GenerateRefreshToken_Returns_Non_Empty_String()
    {
        var token = await _sut.GenerateRefreshTokenAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task GenerateRefreshToken_Each_Call_Returns_Unique_Value()
    {
        var first  = await _sut.GenerateRefreshTokenAsync();
        var second = await _sut.GenerateRefreshTokenAsync();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task GenerateToken_Returns_Three_Part_JWT_Structure()
    {
        var token = await _sut.GenerateTokenAsync(1, "test@example.com", "Admin");

        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public async Task ValidateToken_Valid_Token_Returns_True()
    {
        var token = await _sut.GenerateTokenAsync(1, "test@example.com", "Admin");

        Assert.True(await _sut.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateToken_Tampered_Token_Returns_False()
    {
        Assert.False(await _sut.ValidateTokenAsync("not.a.valid.token"));
    }

    [Fact]
    public async Task GetTokenClaims_Contains_Expected_Email()
    {
        const string email = "test@example.com";
        var token = await _sut.GenerateTokenAsync(1, email, "Admin");

        var claims = await _sut.GetTokenClaimsAsync(token);

        Assert.Contains(claims, c => c.Value?.ToString() == email);
    }

    [Fact]
    public async Task GetTokenClaims_Contains_UserId_As_Subject()
    {
        var token = await _sut.GenerateTokenAsync(42, "test@example.com", "Dietitian");

        var claims = await _sut.GetTokenClaimsAsync(token);

        Assert.Contains(claims, c => c.Value?.ToString() == "42");
    }
}
