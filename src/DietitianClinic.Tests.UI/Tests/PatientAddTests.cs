using DietitianClinic.Tests.UI.Infrastructure;
using DietitianClinic.Tests.UI.Pages;
using NUnit.Framework;

namespace DietitianClinic.Tests.UI.Tests;

/// <summary>
/// Senaryo 3 — Yeni hasta ekleme formu doldurulup kaydedilince
///             başarılı Toast mesajının görüntülenmesi
///
/// ÖNEMLİ: Bu sayfa (/patients) yalnızca Admin rolüyle erişilebilir.
/// TEST_ADMIN_EMAIL ve TEST_ADMIN_PASSWORD ortam değişkenlerini ayarlayın.
/// </summary>
[TestFixture]
[Category("PatientAdd")]
public sealed class PatientAddTests : BaseTest
{
    private PatientsPage _page = null!;

    [SetUp]
    public void Init()
    {
        _page = new PatientsPage(Driver, Wait);
        LoginAsAdmin();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Senaryo 3
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    [Description("Senaryo 3: Geçerli hasta bilgileri girilip kaydedilince başarı toast'u görünmeli")]
    public void AddPatient_WithValidData_ShowsSuccessToast()
    {
        // Arrange
        _page.NavigateTo(TestConfig.BaseUrl);

        // Unique suffix — aynı TC No ile çift kayıt olmasını önler
        var suffix    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tcNo      = $"1{suffix.ToString()[..9]}"; // 11 haneli

        // Act
        _page.ClickAddPatient();

        _page.FillPatientForm(
            firstName: "Selenium",
            lastName:  $"Test{suffix}",
            tcNo:      tcNo,
            email:     $"selenium.test{suffix}@example.com",
            phone:     "05009876543",
            birthDate: "1995-06-20",
            city:      "Ankara"
        );

        _page.ClickSave();

        // Assert
        Assert.That(_page.IsSuccessToastVisible(), Is.True,
            "Yeni hasta başarıyla kaydedilince başarı (success) toast'u görünmeli");
    }

    [Test]
    [Description("Senaryo 3b: 'Yeni Hasta Ekle' butonuna basınca kayıt dialogu açılmalı")]
    public void ClickAddPatient_OpensDialog()
    {
        // Arrange
        _page.NavigateTo(TestConfig.BaseUrl);

        // Act
        _page.ClickAddPatient();

        // Assert — dialog görünür olmalı (p-dialog CSS sınıfı)
        Assert.That(
            Driver.IsElementPresent(By.CssSelector(".p-dialog")), Is.True,
            "'Yeni Hasta Kaydı' dialogu açılmış olmalı");
    }
}
