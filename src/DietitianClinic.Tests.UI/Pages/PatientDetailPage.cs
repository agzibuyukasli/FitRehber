using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DietitianClinic.Tests.UI.Pages;

/// <summary>
/// /dietitian/patients/{id} sayfası — Page Object Model
///
/// İki sekme içerir:
///   [0] Ölçüm Geçmişi  — ölçüm tablosu + grafik
///   [1] Diyet Programı  — diyet planı kartları
/// </summary>
public sealed class PatientDetailPage : BasePage
{
    // ── Locators ───────────────────────────────────────────────────────────

    // PrimeReact TabView
    private static readonly By TabView =
        By.CssSelector(".p-tabview");

    private static readonly By TabLinks =
        By.CssSelector(".p-tabview-nav .p-tabview-nav-link");

    // Ölçüm tablosu ve "Yeni Ölçüm Ekle" butonu
    private static readonly By AddMeasurementButton =
        By.XPath("//button[.//span[normalize-space()='Yeni Ölçüm Ekle']]");

    private static readonly By MeasurementTable =
        By.CssSelector(".p-datatable");

    // ── Kurucu ────────────────────────────────────────────────────────────
    public PatientDetailPage(IWebDriver driver, WebDriverWait wait) : base(driver, wait) { }

    // ── Yükleme Kontrolü ──────────────────────────────────────────────────

    /// <summary>
    /// TabView bileşeni görünene kadar bekler.
    /// Sayfanın tam yüklendiğini doğrular.
    /// </summary>
    public bool IsLoaded()
    {
        try
        {
            Wait.WaitForVisible(TabView);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    /// <summary>URL'nin hasta detay formatıyla eşleştiğini doğrular.</summary>
    public bool UrlMatchesDetailPattern(string baseUrl)
    {
        var currentUrl = Driver.Url;
        var patientBasePath = $"{baseUrl}/dietitian/patients/";

        return currentUrl.StartsWith(patientBasePath, StringComparison.OrdinalIgnoreCase)
               && currentUrl.Length > patientBasePath.Length; // /patients/ değil /patients/123
    }

    // ── Sekme Doğrulama ────────────────────────────────────────────────────

    public IReadOnlyCollection<IWebElement> GetTabs() =>
        Wait.WaitForElements(TabLinks, minCount: 1);

    /// <summary>Ölçüm Geçmişi sekmesinin varlığını kontrol eder.</summary>
    public bool HasMeasurementTab()
    {
        var tabs = GetTabs();
        return tabs.Any(t =>
            t.Text.Contains("Ölçüm", StringComparison.OrdinalIgnoreCase) ||
            t.Text.Contains("lcm",   StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Diyet Programı sekmesinin varlığını kontrol eder.</summary>
    public bool HasDietTab()
    {
        var tabs = GetTabs();
        return tabs.Any(t =>
            t.Text.Contains("Diyet",   StringComparison.OrdinalIgnoreCase) ||
            t.Text.Contains("Program", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Belirtilen sekmeye (0=Ölçüm, 1=Diyet) tıklar.</summary>
    public void ClickTab(int index)
    {
        var tabs = Wait.WaitForElements(TabLinks, minCount: index + 1);
        tabs.ElementAt(index).Click();
    }

    // ── İçerik Doğrulama ──────────────────────────────────────────────────

    public bool IsMeasurementTableVisible() =>
        Driver.IsElementPresent(MeasurementTable);

    public bool IsAddMeasurementButtonVisible() =>
        Driver.IsElementPresent(AddMeasurementButton);
}
