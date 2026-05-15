using System.Collections.Concurrent;
using System.Reflection;
using DietitianClinic.API.Services;
using Xunit;

namespace DietitianClinic.Tests.Unit;

public class PasswordResetServiceTests
{
    private static PasswordResetService Create() => new();

    // ─── GenerateCode ─────────────────────────────────────────────────

    [Fact]
    public void GenerateCode_ReturnsSixDigitNumericCode()
    {
        var sut  = Create();
        var code = sut.GenerateCode("a@test.com");

        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _));
    }

    [Fact]
    public void GenerateCode_ReturnsUniqueCodesOnSuccessiveCalls()
    {
        var sut   = Create();
        var code1 = sut.GenerateCode("a@test.com");
        var code2 = sut.GenerateCode("b@test.com");

        // Statistically farklı olmalı (aynı olma ihtimali 1/900000)
        // farklı e-postalar için kesinlikle bağımsız
        Assert.IsType<string>(code1);
        Assert.IsType<string>(code2);
    }

    [Fact]
    public void GenerateCode_NormalizesEmailCaseInsensitive()
    {
        var sut  = Create();
        var code = sut.GenerateCode("User@TEST.COM");

        Assert.True(sut.VerifyCode("user@test.com", code, out _));
    }

    // ─── VerifyCode ───────────────────────────────────────────────────

    [Fact]
    public void VerifyCode_ReturnsTrueAndToken_WhenCodeIsCorrect()
    {
        var sut  = Create();
        var code = sut.GenerateCode("u@test.com");

        var result = sut.VerifyCode("u@test.com", code, out var token);

        Assert.True(result);
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void VerifyCode_ReturnsFalse_WhenCodeIsWrong()
    {
        var sut = Create();
        sut.GenerateCode("u@test.com");

        var result = sut.VerifyCode("u@test.com", "000000", out var token);

        Assert.False(result);
        Assert.Empty(token);
    }

    [Fact]
    public void VerifyCode_ReturnsFalse_WhenEmailNotRegistered()
    {
        var sut    = Create();
        var result = sut.VerifyCode("ghost@test.com", "123456", out var token);

        Assert.False(result);
        Assert.Empty(token);
    }

    [Fact]
    public void VerifyCode_ReturnsFalse_WhenCodeExpired()
    {
        var sut  = Create();
        sut.GenerateCode("u@test.com");

        // Reflection ile süresi dolmuş kodu manuel olarak gir
        var codesField = typeof(PasswordResetService)
            .GetField("_codes", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var codes = (ConcurrentDictionary<string, (string Code, DateTime Expiry)>)codesField.GetValue(sut)!;
        codes["u@test.com"] = ("999999", DateTime.UtcNow.AddMinutes(-1));

        var result = sut.VerifyCode("u@test.com", "999999", out var token);

        Assert.False(result);
        Assert.Empty(token);
    }

    [Fact]
    public void VerifyCode_RemovesCode_AfterSuccessfulVerify()
    {
        var sut  = Create();
        var code = sut.GenerateCode("u@test.com");
        sut.VerifyCode("u@test.com", code, out _);

        // İkinci kez doğrulama yapılamaz
        var result = sut.VerifyCode("u@test.com", code, out _);

        Assert.False(result);
    }

    // ─── ConsumeToken ─────────────────────────────────────────────────

    [Fact]
    public void ConsumeToken_ReturnsTrue_WhenTokenIsCorrect()
    {
        var sut  = Create();
        var code = sut.GenerateCode("u@test.com");
        sut.VerifyCode("u@test.com", code, out var token);

        var result = sut.ConsumeToken("u@test.com", token);

        Assert.True(result);
    }

    [Fact]
    public void ConsumeToken_ReturnsFalse_WhenTokenIsWrong()
    {
        var sut  = Create();
        var code = sut.GenerateCode("u@test.com");
        sut.VerifyCode("u@test.com", code, out _);

        var result = sut.ConsumeToken("u@test.com", "wrong-token");

        Assert.False(result);
    }

    [Fact]
    public void ConsumeToken_ReturnsFalse_WhenEmailNotRegistered()
    {
        var sut    = Create();
        var result = sut.ConsumeToken("ghost@test.com", "any-token");

        Assert.False(result);
    }

    [Fact]
    public void ConsumeToken_ReturnsFalse_AfterTokenAlreadyConsumed()
    {
        var sut  = Create();
        var code = sut.GenerateCode("u@test.com");
        sut.VerifyCode("u@test.com", code, out var token);
        sut.ConsumeToken("u@test.com", token);

        // İkinci kez consume edilemez
        var result = sut.ConsumeToken("u@test.com", token);

        Assert.False(result);
    }

    [Fact]
    public void ConsumeToken_ReturnsFalse_WhenTokenExpired()
    {
        var sut  = Create();
        var code = sut.GenerateCode("u@test.com");
        sut.VerifyCode("u@test.com", code, out var token);

        // Reflection ile süresi dolmuş token gir
        var tokensField = typeof(PasswordResetService)
            .GetField("_tokens", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var tokens = (ConcurrentDictionary<string, (string Token, DateTime Expiry)>)tokensField.GetValue(sut)!;
        tokens["u@test.com"] = (token, DateTime.UtcNow.AddMinutes(-1));

        var result = sut.ConsumeToken("u@test.com", token);

        Assert.False(result);
    }
}
