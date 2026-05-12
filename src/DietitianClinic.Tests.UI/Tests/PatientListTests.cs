using DietitianClinic.Tests.UI.Infrastructure;
using DietitianClinic.Tests.UI.Pages;
using NUnit.Framework;

namespace DietitianClinic.Tests.UI.Tests;

/// <summary>
/// Senaryo 2 — Giriş yaptıktan sonra Hastalar sayfasına gidilmesi
///             ve tablonun verilerle yüklendiğinin doğrulanması
/// </summary>
[TestFixture]
[Category("PatientList")]
public sealed class PatientListTests : BaseTest
{
    private DietitianPatientsPage _page = null!;

    [SetUp]
    public void Init()
    {
        _page = new DietitianPatientsPage(Driver, Wait);
        LoginAsDietitian();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Senaryo 2
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    [Description("Senaryo 2: Hastalar sayfası açıldığında tablo en az bir hasta içermeli")]
    public void PatientsPage_LoadsWithData()
    {
        // Act
        _page.NavigateTo(TestConfig.BaseUrl);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_page.IsTableLoadedWithData(), Is.True,
                "Hasta tablosu yüklenmiş ve en az bir satır içermeli");

            Assert.That(_page.GetPatientCount(), Is.GreaterThan(0),
                "Tablodaki hasta sayısı 0'dan büyük olmalı");
        });
    }

    [Test]
    [Description("Senaryo 2b: URL /dietitian/patients olmalı ve sayfa yüklenmeli")]
    public void PatientsPage_HasCorrectUrl()
    {
        // Act
        _page.NavigateTo(TestConfig.BaseUrl);

        // Assert
        Assert.That(Driver.Url, Does.Contain("/dietitian/patients"),
            "Sayfa URL'si /dietitian/patients içermeli");
    }

    [Test]
    [Description("Senaryo 2c: İlk hastanın adı boş olmamalı")]
    public void PatientsPage_FirstPatientHasName()
    {
        // Act
        _page.NavigateTo(TestConfig.BaseUrl);

        // Assert — tabloda en az bir kayıt varsayılıyor
        var firstName = _page.GetPatientNameAt(0);

        Assert.That(firstName, Is.Not.Null.And.Not.Empty,
            "Tablonun ilk satırında hasta adı bulunmalı");
    }
}
