using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace DietitianClinic.Tests.UI.Infrastructure;

/// <summary>
/// ChromeDriver örneği oluşturur.
/// Selenium 4.6+ ile gelen Selenium Manager, ChromeDriver'ı otomatik olarak
/// indirir — ayrıca bir ChromeDriver paketi eklemenize gerek yoktur.
/// </summary>
public static class DriverFactory
{
    public static IWebDriver Create(bool headless = false)
    {
        var options = new ChromeOptions();

        // Temel argümanlar
        options.AddArgument("--start-maximized");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--lang=tr-TR");
        options.AddArgument("--disable-extensions");

        // Chrome autofill / şifre yöneticisini kapat — form alanlarını bozmasın
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddArgument("--disable-features=AutofillServerCommunication");

        if (headless)
        {
            // Chrome 112+ headless modu
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1920,1080");
        }

        var driver = new ChromeDriver(options);

        driver.Manage().Timeouts().PageLoad   = TimeSpan.FromSeconds(30);
        driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(10);
        // Implicit wait sıfır — yalnızca explicit wait kullanıyoruz
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;

        return driver;
    }
}
