using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DietitianClinic.Tests.UI.Pages;

/// <summary>
/// /dietitian/patients sayfası — Page Object Model
///
/// Yalnızca Dietitian rolü için erişilebilir.
/// Tablo satırlarında iki aksiyon butonu bulunur:
///   pi-folder-open → Detay sayfasına yönlendirir (/dietitian/patients/{id})
///   pi-chart-line  → Ölçüm geçmişi dialogu açar
/// </summary>
public sealed class DietitianPatientsPage : BasePage
{
    // ── Locators ───────────────────────────────────────────────────────────

    private static readonly By TableBody =
        By.CssSelector(".p-datatable-tbody");

    private static readonly By TableRows =
        By.CssSelector(".p-datatable-tbody tr");

    // Sayfadaki tüm aksiyon butonları — kullanılmıyor, tanısal amaçlı bırakıldı
    // private static readonly By DetailButtonsByIcon =
    //     By.XPath("//div[contains(@class,'p-datatable')]//button[.//i[contains(@class,'pi-folder-open')]]");

    private static readonly By SearchInput =
        By.CssSelector("input[placeholder='Danışan ara...']");

    // ── Kurucu ────────────────────────────────────────────────────────────
    public DietitianPatientsPage(IWebDriver driver, WebDriverWait wait) : base(driver, wait) { }

    // ── Navigasyon ────────────────────────────────────────────────────────

    public void NavigateTo(string baseUrl)
    {
        Driver.Navigate().GoToUrl($"{baseUrl}/dietitian/patients");
        WaitForTableToLoad();
    }

    // ── Tablo ─────────────────────────────────────────────────────────────

    public void WaitForTableToLoad() =>
        Wait.WaitForVisible(TableBody);

    public int GetPatientCount() =>
        Driver.FindElements(TableRows).Count;

    public bool IsTableLoadedWithData()
    {
        Wait.WaitForVisible(TableBody);
        return Driver.FindElements(TableRows).Count > 0;
    }

    public string GetPatientNameAt(int rowIndex = 0)
    {
        var rows = Wait.WaitForElements(TableRows, minCount: rowIndex + 1);
        // Hasta adı ilk hücrede render edilir
        var cell = rows.ElementAt(rowIndex).FindElement(By.CssSelector("td:first-child"));
        return cell.Text.Trim();
    }

    // ── Detay Butonu ───────────────────────────────────────────────────────

    /// <summary>
    /// Belirtilen indeksteki satırın Detay (folder-open) butonuna tıklar.
    /// Tıklama sonrası /dietitian/patients/{id} sayfasına yönlendirilir.
    /// </summary>
    /// <summary>
    /// Belirtilen indeksteki satırın Detay butonuna tıklar.
    /// İkon sınıfı yerine satır içindeki buton sırasına göre bulur:
    ///   İşlemler sütunundaki 1. buton = folder-open (Detay)
    ///   İşlemler sütunundaki 2. buton = chart-line (Hızlı Ölçüm)
    /// </summary>
    public void ClickDetailButton(int rowIndex = 0)
    {
        // Stale element durumunda yeniden dene (Wait otomatik olarak kurtarır)
        try
        {
            Wait.Until(driver =>
            {
                var rows = driver.FindElements(TableRows);
                if (rows.Count <= rowIndex) return false;

                var targetRow = rows.ElementAt(rowIndex);
                // Tablodaki tek butonlu sütun = İşlemler; ilk buton = Detay
                var buttons = targetRow.FindElements(By.TagName("button"));
                if (buttons.Count == 0) return false;

                var btn = buttons[0];
                if (!btn.Displayed || !btn.Enabled) return false;

                btn.Click();
                return true;
            });
        }
        catch (WebDriverTimeoutException)
        {
            var count = Driver.FindElements(TableRows).Count;
            if (count == 0)
                throw new InvalidOperationException(
                    "Hasta tablosu boş — detay butonuna tıklanamadı. " +
                    "Veritabanında en az bir hasta kaydı olmalı.");

            throw new InvalidOperationException(
                $"Satır {rowIndex} içinde tıklanabilir buton bulunamadı " +
                $"(tabloda {count} satır var). " +
                "Sayfa tam yüklenmiş mi?");
        }
    }

    // ── Arama ─────────────────────────────────────────────────────────────

    public void SearchPatient(string name)
    {
        var input = FindClickable(SearchInput);
        input.Clear();
        input.SendKeys(name);
    }
}
