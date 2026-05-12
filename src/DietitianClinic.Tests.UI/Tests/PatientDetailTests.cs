using DietitianClinic.Tests.UI.Infrastructure;
using DietitianClinic.Tests.UI.Pages;
using NUnit.Framework;

namespace DietitianClinic.Tests.UI.Tests;

/// <summary>
/// Senaryo 5 — Hasta listesindeki Detay butonuna tıklanınca
///             o hastaya özel bilgilerin (TabView, sekmeler) ekrana geldiğinin kontrolü
///
/// Not: /dietitian/patients sayfası Dietitian rolüyle erişilir.
///      Tabloda en az bir hasta kaydının bulunması gerekir.
/// </summary>
[TestFixture]
[Category("PatientDetail")]
public sealed class PatientDetailTests : BaseTest
{
    private DietitianPatientsPage _listPage   = null!;
    private PatientDetailPage     _detailPage = null!;

    [SetUp]
    public void Init()
    {
        _listPage   = new DietitianPatientsPage(Driver, Wait);
        _detailPage = new PatientDetailPage(Driver, Wait);
        LoginAsDietitian();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Senaryo 5
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    [Description("Senaryo 5: Detay butonuna tıklanınca hasta detay sayfasına yönlendirilmeli")]
    public void ClickDetailButton_NavigatesToPatientDetailPage()
    {
        // Arrange — hasta listesine git, tablonun dolduğunu doğrula
        _listPage.NavigateTo(TestConfig.BaseUrl);

        Assert.That(_listPage.IsTableLoadedWithData(), Is.True,
            "Önkoşul: Tabloda en az bir hasta olmalı");

        var urlBeforeClick = Driver.Url;

        // Act — ilk satırın Detay (folder-open) butonuna tıkla
        _listPage.ClickDetailButton(rowIndex: 0);

        // Assert 1 — URL /dietitian/patients/{id} formatına geçmeli
        Wait.WaitForUrlContains("/dietitian/patients/");

        Assert.Multiple(() =>
        {
            Assert.That(Driver.Url, Is.Not.EqualTo(urlBeforeClick),
                "Detay butonuna tıklayınca URL değişmeli");

            Assert.That(_detailPage.UrlMatchesDetailPattern(TestConfig.BaseUrl), Is.True,
                "URL /dietitian/patients/{numericId} desenine uymalı");
        });
    }

    [Test]
    [Description("Senaryo 5b: Detay sayfası yüklenince TabView ve Ölçüm sekmesi görünmeli")]
    public void DetailPage_ShowsMeasurementTab()
    {
        // Arrange
        _listPage.NavigateTo(TestConfig.BaseUrl);
        Assert.That(_listPage.IsTableLoadedWithData(), Is.True,
            "Önkoşul: Tabloda en az bir hasta olmalı");

        // Act
        _listPage.ClickDetailButton(rowIndex: 0);
        Wait.WaitForUrlContains("/dietitian/patients/");

        // Assert — TabView bileşeni ve Ölçüm Geçmişi sekmesi görünmeli
        Assert.Multiple(() =>
        {
            Assert.That(_detailPage.IsLoaded(), Is.True,
                "Hasta detay sayfası (TabView) yüklenmiş olmalı");

            Assert.That(_detailPage.HasMeasurementTab(), Is.True,
                "'Ölçüm Geçmişi' sekmesi mevcut olmalı");
        });
    }

    [Test]
    [Description("Senaryo 5c: Detay sayfasında Diyet Programı sekmesi de bulunmalı")]
    public void DetailPage_ShowsDietTab()
    {
        // Arrange
        _listPage.NavigateTo(TestConfig.BaseUrl);
        Assert.That(_listPage.IsTableLoadedWithData(), Is.True,
            "Önkoşul: Tabloda en az bir hasta olmalı");

        // Act
        _listPage.ClickDetailButton(rowIndex: 0);
        Wait.WaitForUrlContains("/dietitian/patients/");

        // Assert
        Assert.That(_detailPage.HasDietTab(), Is.True,
            "'Diyet Programı' sekmesi mevcut olmalı");
    }

    [Test]
    [Description("Senaryo 5d: Ölçüm Geçmişi sekmesinde 'Yeni Ölçüm Ekle' butonu görünmeli")]
    public void DetailPage_MeasurementTab_HasAddButton()
    {
        // Arrange
        _listPage.NavigateTo(TestConfig.BaseUrl);
        Assert.That(_listPage.IsTableLoadedWithData(), Is.True,
            "Önkoşul: Tabloda en az bir hasta olmalı");

        // Act — detay sayfasına git, ilk sekme (Ölçüm Geçmişi) zaten açık olur
        _listPage.ClickDetailButton(rowIndex: 0);
        Wait.WaitForUrlContains("/dietitian/patients/");
        Assert.That(_detailPage.IsLoaded(), Is.True);

        // İlk sekmeye tıkla (varsayılan olarak açık olsa da garantilemek için)
        _detailPage.ClickTab(0);

        // Assert
        Assert.That(_detailPage.IsAddMeasurementButtonVisible(), Is.True,
            "'Yeni Ölçüm Ekle' butonu görünür olmalı");
    }
}
