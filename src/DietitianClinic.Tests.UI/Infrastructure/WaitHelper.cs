#nullable disable warnings

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DietitianClinic.Tests.UI.Infrastructure;

/// <summary>
/// WebDriverWait için extension metodlar.
/// Tüm metodlar StaleElementReferenceException ve NoSuchElementException'ı
/// otomatik yutar — sayfa yeniden render olsa bile beklemeye devam eder.
/// </summary>
public static class WaitHelper
{
    // ── Element Bekleme ────────────────────────────────────────────────────

    /// <summary>Element görünür olana kadar bekler ve döndürür.</summary>
    public static IWebElement WaitForVisible(this WebDriverWait wait, By locator)
    {
        return wait.Until(driver =>
        {
            try
            {
                var el = driver.FindElement(locator);
                return el.Displayed ? el : null;
            }
            catch (NoSuchElementException)          { return null; }
            catch (StaleElementReferenceException)  { return null; }
        });
    }

    /// <summary>Element tıklanabilir (görünür + etkin) olana kadar bekler.</summary>
    public static IWebElement WaitForClickable(this WebDriverWait wait, By locator)
    {
        return wait.Until(driver =>
        {
            try
            {
                var el = driver.FindElement(locator);
                return (el.Displayed && el.Enabled) ? el : null;
            }
            catch (NoSuchElementException)          { return null; }
            catch (StaleElementReferenceException)  { return null; }
        });
    }

    /// <summary>En az <paramref name="minCount"/> eleman bulunana kadar bekler.</summary>
    public static IReadOnlyCollection<IWebElement> WaitForElements(
        this WebDriverWait wait, By locator, int minCount = 1)
    {
        return wait.Until(driver =>
        {
            var elements = driver.FindElements(locator);
            return elements.Count >= minCount ? elements : null;
        });
    }

    // ── URL Bekleme ────────────────────────────────────────────────────────

    /// <summary>URL belirtilen metni içerene kadar bekler.</summary>
    public static void WaitForUrlContains(this WebDriverWait wait, string fragment)
    {
        wait.Until(driver =>
            driver.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>URL belirtilen metni içerMEyene kadar bekler (yönlendirme sonrası).</summary>
    public static void WaitForUrlNotContains(this WebDriverWait wait, string fragment)
    {
        wait.Until(driver =>
            !driver.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    // ── Element Yokluğu ────────────────────────────────────────────────────

    /// <summary>Element DOM'dan kaybolana ya da gizlenene kadar bekler.</summary>
    public static void WaitForElementToDisappear(this WebDriverWait wait, By locator)
    {
        wait.Until(driver =>
        {
            try
            {
                var el = driver.FindElement(locator);
                return !el.Displayed;
            }
            catch (NoSuchElementException)          { return true; }
            catch (StaleElementReferenceException)  { return true; }
        });
    }

    // ── Yardımcı ──────────────────────────────────────────────────────────

    /// <summary>Senkron yardımcı — element var mı kontrol eder (wait olmadan).</summary>
    public static bool IsElementPresent(this IWebDriver driver, By locator)
    {
        try   { driver.FindElement(locator); return true; }
        catch (NoSuchElementException) { return false; }
    }
}
