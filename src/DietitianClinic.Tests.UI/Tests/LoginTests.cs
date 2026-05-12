using DietitianClinic.Tests.UI.Infrastructure;
using DietitianClinic.Tests.UI.Pages;
using NUnit.Framework;

namespace DietitianClinic.Tests.UI.Tests;

/// <summary>
/// Senaryo 1 — Geçerli bilgilerle giriş ve dashboard yönlendirmesi
/// Senaryo 4 — Yanlış şifre ile hata mesajı doğrulaması
/// </summary>
[TestFixture]
[Category("Login")]
public sealed class LoginTests : BaseTest
{
    private LoginPage _page = null!;

    // NUnit, [SetUp]'ı base sınıftan SONRA bu sınıfta otomatik çağırır.
    [SetUp]
    public void Init()
    {
        _page = new LoginPage(Driver, Wait);
        NavigateToLoginPage();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Senaryo 1
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    [Description("Senaryo 1: Diyetisyen geçerli bilgilerle giriş yapınca /dietitian dashboard'una yönlendirilmeli")]
    public void ValidLogin_AsDietitian_RedirectsToDashboard()
    {
        // Act
        _page.Login(TestConfig.DietitianEmail, TestConfig.DietitianPassword);

        // URL değişmesini veya hata toastını bekle — yanlış kimlik bilgisi varsa açık mesaj ver
        var redirected = _page.WaitForLoginOutcome();
        Assert.That(redirected, Is.True,
            $"Diyetisyen girişi başarısız. TestConfig.DietitianEmail='{TestConfig.DietitianEmail}' " +
            "kimlik bilgilerini veritabanındaki gerçek hesapla karşılaştırın.");

        Assert.That(
            Driver.Url.Contains("/dietitian") ||
            Driver.Url.TrimEnd('/').Equals(TestConfig.BaseUrl.TrimEnd('/')),
            Is.True,
            "Dashboard'a (/dietitian veya /) yönlendirilmiş olmalı");
    }

    [Test]
    [Description("Senaryo 1b: Admin geçerli bilgilerle giriş yapınca kök dashboard'una yönlendirilmeli")]
    public void ValidLogin_AsAdmin_RedirectsToRootDashboard()
    {
        // Act
        _page.Login(TestConfig.AdminEmail, TestConfig.AdminPassword);

        // URL değişmesini veya hata toastını bekle — yanlış kimlik bilgisi varsa açık mesaj ver
        var redirected = _page.WaitForLoginOutcome();
        Assert.That(redirected, Is.True,
            $"Admin girişi başarısız. TestConfig.AdminEmail='{TestConfig.AdminEmail}' " +
            "kimlik bilgilerini veritabanındaki gerçek hesapla karşılaştırın.");

        Assert.That(Driver.Url, Does.Not.Contain("/auth/login"),
            "Admin girişi sonrası login sayfasında kalınmamalı");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Senaryo 4
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    [Description("Senaryo 4: Yanlış şifre girilince hata toast'u görünmeli, sayfa değişmemeli")]
    public void WrongPassword_ShowsErrorToast_AndStaysOnLoginPage()
    {
        // Act — mevcut e-posta, yanlış şifre
        _page.Login(TestConfig.DietitianEmail, "YanlisS1fre!XYZ");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_page.IsErrorToastVisible(), Is.True,
                "Hata (error) toast mesajı ekranda görünmeli");

            Assert.That(Driver.Url, Does.Contain("/auth/login"),
                "Başarısız girişten sonra login sayfasında kalınmalı");
        });
    }

    [Test]
    [Description("Senaryo 4b: Boş alanlarla giriş denenince uyarı mesajı çıkmalı")]
    public void EmptyFields_ShowsWarningToast()
    {
        // Act — alanları doldurmadan direkt login butonuna bas
        _page.ClickLogin();

        // Assert — warn veya error toast görünmeli
        Assert.That(_page.IsAnyErrorVisible(), Is.True,
            "Boş alanlarla giriş denemesi uyarı/hata toast'u üretmeli");
    }
}
