using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DietitianClinic.Tests.UI.Infrastructure;

/// <summary>
/// Tüm test sınıflarının türediği taban sınıf.
/// NUnit, [SetUp] metodunu önce bu sınıfta, ardından türeyen sınıfta çağırır.
/// [TearDown] ise ters sırayla çalışır.
/// Servis erişilebilirlik garantisi AssemblySetup tarafından sağlanır.
/// </summary>
[TestFixture]
public abstract class BaseTest
{
    protected IWebDriver     Driver = null!;
    protected WebDriverWait  Wait   = null!;

    [SetUp]
    public void SetUp()
    {
        Driver = DriverFactory.Create(headless: TestConfig.Headless);

        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(TestConfig.DefaultTimeoutSeconds))
        {
            PollingInterval = TimeSpan.FromMilliseconds(300)
        };
        Wait.IgnoreExceptionTypes(
            typeof(NoSuchElementException),
            typeof(StaleElementReferenceException),
            typeof(ElementNotInteractableException));
    }

    [TearDown]
    public void TearDown()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }

    // ── Ortak Yardımcı Metodlar ────────────────────────────────────────────

    /// <summary>Login sayfasına gider ve formun yüklenmesini bekler.</summary>
    protected void NavigateToLoginPage()
    {
        Driver.Navigate().GoToUrl($"{TestConfig.BaseUrl}/auth/login");
        Wait.WaitForVisible(By.Id("email1"));
    }

    /// <summary>
    /// React controlled input için güvenli değer yazma.
    /// Ctrl+A ile seç → yaz → React onChange tetiklenir.
    /// </summary>
    private static void FillReactInput(IWebElement element, string value)
    {
        element.Click();
        element.SendKeys(Keys.Control + "a");
        element.SendKeys(value);
    }

    /// <summary>Verilen kimlik bilgileriyle giriş yapar ve yönlendirmeyi bekler.</summary>
    protected void LoginAs(string email, string password)
    {
        NavigateToLoginPage();

        var emailInput = Wait.WaitForClickable(By.Id("email1"));
        FillReactInput(emailInput, email);

        // PrimeReact <Password inputId="password1"> → <input id="password1">
        var passwordInput = Wait.WaitForClickable(By.Id("password1"));
        FillReactInput(passwordInput, password);

        // Kısa bekleme — React state güncellemesinin tamamlanması için
        Thread.Sleep(200);

        // PrimeReact <Button label="Giriş Yap"> → <button><span>Giriş Yap</span></button>
        var loginBtn = Wait.WaitForClickable(
            By.XPath("//button[.//span[normalize-space()='Giriş Yap']]"));
        loginBtn.Click();

        // URL değişmesini, hata veya uyarı toastının görünmesini bekle (25 sn)
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
                // Başarılı yönlendirme — login sayfasından çıkıldı
                if (!driver.Url.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
                {
                    redirected = true;
                    return true;
                }
                // Herhangi bir toast (error veya warn) = sunucu yanıt verdi
                return driver.FindElements(
                    By.CssSelector(".p-toast-message-error, .p-toast-message-warn")).Count > 0;
            });
        }
        catch (WebDriverTimeoutException)
        {
            throw new InvalidOperationException(
                $"Giriş zaman aşımına uğradı (e-posta: {email}). " +
                "Backend çalışıyor mu? TestConfig.cs veya ortam değişkenlerindeki " +
                "e-posta ve şifreyi kontrol edin.");
        }

        if (!redirected)
        {
            // Toast içeriğini oku (error veya warn)
            var toastDetail = "";
            try
            {
                toastDetail = Driver
                    .FindElement(By.CssSelector(
                        ".p-toast-message-error .p-toast-detail, " +
                        ".p-toast-message-warn .p-toast-detail"))
                    .Text;
            }
            catch { /* toast detayı okunamadı */ }

            // warn toast = şifre boş gönderildi (React state güncellenmedi)
            var isWarn = Driver.FindElements(
                By.CssSelector(".p-toast-message-warn")).Count > 0;

            var hint = isWarn
                ? "Uyarı toast'u: form alanları boş gönderildi — React state sorunu olabilir."
                : "TestConfig.cs içindeki kimlik bilgilerini veritabanıyla karşılaştırın.";

            throw new InvalidOperationException(
                $"Login başarısız! e-posta: {email}. " +
                (toastDetail.Length > 0 ? $"Sunucu yanıtı: \"{toastDetail}\". " : "") +
                hint);
        }
    }

    protected void LoginAsDietitian() =>
        LoginAs(TestConfig.DietitianEmail, TestConfig.DietitianPassword);

    protected void LoginAsAdmin() =>
        LoginAs(TestConfig.AdminEmail, TestConfig.AdminPassword);
}
