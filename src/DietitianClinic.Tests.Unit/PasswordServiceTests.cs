using DietitianClinic.Business.Services;
using Xunit;

namespace DietitianClinic.Tests.Unit;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void HashPassword_Returns_BCrypt_Hash()
    {
        var hash = _sut.HashPassword("Test@123");

        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void VerifyPassword_Correct_Password_Returns_True()
    {
        var hash = _sut.HashPassword("Test@123");

        Assert.True(_sut.VerifyPassword("Test@123", hash));
    }

    [Fact]
    public void VerifyPassword_Wrong_Password_Returns_False()
    {
        var hash = _sut.HashPassword("Test@123");

        Assert.False(_sut.VerifyPassword("WrongPass!", hash));
    }

    [Fact]
    public void VerifyPassword_Empty_Hash_Returns_False()
    {
        Assert.False(_sut.VerifyPassword("Test@123", ""));
    }

    [Fact]
    public void NeedsRehash_BCrypt_Hash_Returns_False()
    {
        var hash = _sut.HashPassword("Test@123");

        Assert.False(_sut.NeedsRehash(hash));
    }

    [Fact]
    public void NeedsRehash_Legacy_Hash_Returns_True()
    {
        Assert.True(_sut.NeedsRehash("sha256base64legacyhashvalue=="));
    }

    [Theory]
    [InlineData("weak",         false)]
    [InlineData("NoDigitOrSp!", false)]
    [InlineData("nouppercase1!", false)]
    [InlineData("StrongP@ss1",  true)]
    public async Task ValidatePasswordStrength_Returns_Expected_Result(string password, bool expectedValid)
    {
        var (isValid, _) = await _sut.ValidatePasswordStrengthAsync(password);

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task ValidatePasswordStrength_Weak_Password_Returns_Multiple_Errors()
    {
        var (_, errors) = await _sut.ValidatePasswordStrengthAsync("weak");

        Assert.True(errors.Count > 1);
    }
}
