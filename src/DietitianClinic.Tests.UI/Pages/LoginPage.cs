using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DietitianClinic.Tests.UI.Pages;

/// <summary>
/// /auth/login sayfası — Page Object Model
///
/// HTML yapısı (PrimeReact):
///   &lt;input id="email1" type="email" /&gt;
///   &lt;input id="password1" type="password" /&gt;  ← Password component'in inputId'si
///   &lt;button&gt;&lt;span&gt;Giriş Yap&lt;/span&gt;&lt;/button&gt;
///   &lt;div class="p-toast p-toast-top-center"&gt; ... &lt;/div&gt;
/// </summary>
public sealed class LoginPage : BasePage
{
    // ── Locators ───────────────────────────────────────────────────────────
    private static readonly By EmailInput =
        By.Id("email1");

    private static readonly By PasswordInput =
        By.Id("password1");

    private static readonly By LoginButton =
        By.XPath("//button[.//span[normalize-space()='Giriş Yap']]");

    private static readonly By ErrorToast =
        By.CssSelector(".p-toast-message-error");

    private static readonly By WarnToast =
        By.CssSelector(".p-toast-message-warn");

    private static readonly By ToastDetail =
        By.CssSelector(".p-toast-detail");

    // ── Kurucu ────────────────────────────────────────────────────────────
    public LoginPage(IWebDriver driver, WebDriverWait wait) : base(driver, wait) { }

    // ── Eylemler ──────────────────────────────────────────────────────────

    public void EnterEmail(string email)
    {
        var input = FindClickable(EmailInput);
        input.Click();
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(email);
    }

    public void EnterPassword(string password)
    {
        var input = FindClickable(PasswordInput);
        input.Click();
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(password);
    }

    public void ClickLogin() =>
        FindClickable(LoginButton).Click();

    /// <summary>Email ve şifre girip login butonuna basar (bileşik eylem).</summary>
    public void Login(string email, string password)
    {
        EnterEmail(email);
        EnterPassword(password);
        Thread.Sleep(200); // React state güncellemesi için kısa bekleme
        ClickLogin();
    }

    /// <summary>
    /// Login sonucunu bekler.
    /// Döndürür: true = başarılı yönlendirme, false = hata/uyarı toast göründü.
    /// Throws: InvalidOperationException — 25 sn içinde hiçbiri olmazsa.
    /// </summary>
    public bool WaitForLoginOutcome()
    {
        var loginWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(25))
        {
            PollingInterval = TimeSpan.FromMilliseconds(250)
        };
        loginWait.IgnoreExceptionTypes(
            typeof(NoSuchElementException),
            typeof(StaleElementReferenceException));

        bool redirected = false;
        try
        {
            loginWait.Until(driver =>
            {
                if (!driver.Url.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
                {
                    redirected = true;
                    return true;
                }
                return driver.FindElements(
                    By.CssSelector(".p-toast-message-error, .p-toast-message-warn")).Count > 0;
            });
        }
        catch (WebDriverTimeoutException)
        {
            throw new InvalidOperationException(
                "Login sonucu bekleme zaman aşımı (25 sn). " +
                "Backend çalışıyor mu? TestConfig.cs kimlik bilgilerini kontrol edin.");
        }

        return redirected;
    }

    // ── Doğrulama ──────────────────────────────────────────────────────────

    /// <summary>Hata toast'ının görünmesini bekler, true döner.</summary>
    public bool IsErrorToastVisible()
    {
        Wait.WaitForVisible(ErrorToast);
        return Driver.FindElement(ErrorToast).Displayed;
    }

    /// <summary>Uyarı toast'ı da dahil herhangi bir hata mesajı var mı?</summary>
    public bool IsAnyErrorVisible()
    {
        try { return Driver.FindElement(ErrorToast).Displayed; }
        catch (NoSuchElementException) { }

        try { return Driver.FindElement(WarnToast).Displayed; }
        catch (NoSuchElementException) { }

        return false;
    }

    /// <summary>Toast detay metnini döndürür.</summary>
    public string GetErrorMessage()
    {
        Wait.WaitForVisible(ErrorToast);
        return Driver.FindElement(ToastDetail).Text;
    }
}
