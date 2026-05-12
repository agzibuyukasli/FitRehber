using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DietitianClinic.Tests.UI.Pages;

/// <summary>
/// /patients sayfası (Admin) — Page Object Model
///
/// Bu sayfa yalnızca Admin rolü için erişilebilir.
/// Yeni hasta ekleme formu:
///   id="firstName", id="lastName", id="tcNo", id="patEmail",
///   id="phone", id="birthDate", id="city"
/// Dialog: PrimeReact Dialog, width=520px
/// </summary>
public sealed class PatientsPage : BasePage
{
    // ── Locators ───────────────────────────────────────────────────────────

    // Tablo
    private static readonly By TableBody =
        By.CssSelector(".p-datatable-tbody");
    private static readonly By TableRows =
        By.CssSelector(".p-datatable-tbody tr");

    // Yeni Hasta Ekle butonu
    private static readonly By AddPatientButton =
        By.XPath("//button[.//span[normalize-space()='Yeni Hasta Ekle']]");

    // Dialog
    private static readonly By Dialog =
        By.CssSelector(".p-dialog");

    // Form alanları — LoginPage.tsx içindeki id değerleri
    private static readonly By FirstNameInput = By.Id("firstName");
    private static readonly By LastNameInput  = By.Id("lastName");
    private static readonly By TcNoInput      = By.Id("tcNo");
    private static readonly By EmailInput     = By.Id("patEmail");
    private static readonly By PhoneInput     = By.Id("phone");
    private static readonly By BirthDateInput = By.Id("birthDate");
    private static readonly By CityInput      = By.Id("city");

    // Dialog footer butonları — kapsamı dialog ile sınırlandırıyoruz
    private static readonly By SaveButton =
        By.XPath("//div[contains(@class,'p-dialog-footer')]//button[.//span[normalize-space()='Kaydet']]");
    private static readonly By CancelButton =
        By.XPath("//div[contains(@class,'p-dialog-footer')]//button[.//span[normalize-space()='İptal']]");

    // Toast
    private static readonly By SuccessToast =
        By.CssSelector(".p-toast-message-success");
    private static readonly By ErrorToast =
        By.CssSelector(".p-toast-message-error");

    // ── Kurucu ────────────────────────────────────────────────────────────
    public PatientsPage(IWebDriver driver, WebDriverWait wait) : base(driver, wait) { }

    // ── Navigasyon ────────────────────────────────────────────────────────

    public void NavigateTo(string baseUrl)
    {
        Driver.Navigate().GoToUrl($"{baseUrl}/patients");
        WaitForTableToLoad();
    }

    // ── Tablo Doğrulama ────────────────────────────────────────────────────

    public void WaitForTableToLoad() =>
        Wait.WaitForVisible(TableBody);

    public int GetRowCount() =>
        Driver.FindElements(TableRows).Count;

    public bool IsTableLoaded() =>
        Driver.IsElementPresent(TableBody);

    public bool HasRows()
    {
        Wait.WaitForVisible(TableBody);
        return Driver.FindElements(TableRows).Count > 0;
    }

    // ── Hasta Ekleme ───────────────────────────────────────────────────────

    public void ClickAddPatient()
    {
        FindClickable(AddPatientButton).Click();
        WaitForDialog(); // Dialog açılmasını bekle
    }

    public void FillPatientForm(
        string firstName, string lastName, string tcNo,
        string email, string phone, string birthDate, string city)
    {
        FillDialogField(FirstNameInput,  firstName);
        FillDialogField(LastNameInput,   lastName);
        FillDialogField(TcNoInput,       tcNo);
        FillDialogField(EmailInput,      email);
        FillDialogField(PhoneInput,      phone);
        FillDialogField(BirthDateInput,  birthDate);
        FillDialogField(CityInput,       city);
    }

    public void ClickSave()
    {
        FindClickable(SaveButton).Click();
    }

    public void ClickCancel()
    {
        FindClickable(CancelButton).Click();
    }

    // ── Toast Doğrulama ────────────────────────────────────────────────────

    public bool IsSuccessToastVisible()
    {
        Wait.WaitForVisible(SuccessToast);
        return Driver.FindElement(SuccessToast).Displayed;
    }

    public bool IsErrorToastVisible()
    {
        Wait.WaitForVisible(ErrorToast);
        return Driver.FindElement(ErrorToast).Displayed;
    }

    public string GetToastDetailText()
    {
        return Driver.FindElement(By.CssSelector(".p-toast-detail")).Text;
    }
}
