using DietitianClinic.Tests.UI.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DietitianClinic.Tests.UI.Pages;

/// <summary>
/// Tüm Page Object'lerin türediği soyut taban sınıf.
/// PrimeReact bileşenlerine özgü ortak yardımcı metodları barındırır.
/// </summary>
public abstract class BasePage
{
    protected readonly IWebDriver    Driver;
    protected readonly WebDriverWait Wait;

    protected BasePage(IWebDriver driver, WebDriverWait wait)
    {
        Driver = driver;
        Wait   = wait;
    }

    // ── Element Erişimi ────────────────────────────────────────────────────

    protected IWebElement FindVisible(By locator)   => Wait.WaitForVisible(locator);
    protected IWebElement FindClickable(By locator) => Wait.WaitForClickable(locator);

    // ── PrimeReact Button ──────────────────────────────────────────────────

    /// <summary>
    /// PrimeReact Button'ı label metni ile bulur ve tıklar.
    /// Render: &lt;button&gt;&lt;span class="p-button-label"&gt;Label&lt;/span&gt;&lt;/button&gt;
    /// </summary>
    protected IWebElement ClickButton(string label)
    {
        var btn = FindClickable(
            By.XPath($"//button[.//span[normalize-space()='{label}']]"));
        btn.Click();
        return btn;
    }

    // ── PrimeReact Toast ───────────────────────────────────────────────────

    /// <summary>
    /// Toast görünene kadar bekler.
    /// severity: "success" | "error" | "warn" | "info"
    /// </summary>
    protected void WaitForToast(string severity = "success")
    {
        Wait.WaitForVisible(By.CssSelector($".p-toast-message-{severity}"));
    }

    protected bool IsToastVisible(string severity)
    {
        try
        {
            var el = Driver.FindElement(By.CssSelector($".p-toast-message-{severity}"));
            return el.Displayed;
        }
        catch (NoSuchElementException) { return false; }
    }

    protected string GetToastDetail()
    {
        var el = Wait.WaitForVisible(By.CssSelector(".p-toast-detail"));
        return el.Text;
    }

    // ── PrimeReact Dialog ──────────────────────────────────────────────────

    /// <summary>Dialog görünene kadar bekler.</summary>
    protected void WaitForDialog()
    {
        Wait.WaitForVisible(By.CssSelector(".p-dialog"));
    }

    /// <summary>Dialog içindeki bir input'a değer girer.</summary>
    protected void FillDialogField(By locator, string value)
    {
        var el = FindClickable(locator);
        el.Clear();
        el.SendKeys(value);
    }
}
