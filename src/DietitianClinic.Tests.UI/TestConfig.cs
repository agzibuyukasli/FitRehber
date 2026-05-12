namespace DietitianClinic.Tests.UI;

/// <summary>
/// Test yapılandırması. Değerleri ortam değişkenleriyle geçersiz kılabilirsiniz:
///   TEST_BASE_URL, TEST_API_URL, TEST_ADMIN_EMAIL, TEST_ADMIN_PASSWORD,
///   TEST_DIETITIAN_EMAIL, TEST_DIETITIAN_PASSWORD, TEST_HEADLESS, TEST_TIMEOUT_SECONDS
/// </summary>
public static class TestConfig
{
    /// <summary>
    /// Frontend URL.
    /// Docker/Local: http://localhost:3000
    /// </summary>
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "http://localhost:3000";

    /// <summary>
    /// Backend API URL.
    /// Docker/Local: http://localhost:8080
    /// </summary>
    public static string ApiBaseUrl =>
        Environment.GetEnvironmentVariable("TEST_API_URL") ?? "http://localhost:8080";

    // ── Admin (Yönetici) Kimlik Bilgileri ─────────────────────────────────
    // BU DEĞERLERİ KENDİ VERİTABANINDAKİ ADMIN HESABIYLA DEĞİŞTİR
    public static string AdminEmail =>
        Environment.GetEnvironmentVariable("TEST_ADMIN_EMAIL") ?? "admin@fitrehber.com";

    public static string AdminPassword =>
        Environment.GetEnvironmentVariable("TEST_ADMIN_PASSWORD") ?? "Admin123!";

    // ── Diyetisyen Kimlik Bilgileri ────────────────────────────────────────
    // BU DEĞERLERİ KENDİ VERİTABANINDAKİ DİYETİSYEN HESABIYLA DEĞİŞTİR
    public static string DietitianEmail =>
        Environment.GetEnvironmentVariable("TEST_DIETITIAN_EMAIL") ?? "diyetisyen@fitrehber.com";

    public static string DietitianPassword =>
        Environment.GetEnvironmentVariable("TEST_DIETITIAN_PASSWORD") ?? "Dietitian@123";

    // ── Driver Ayarları ────────────────────────────────────────────────────
    /// <summary>CI ortamında headless mod için TEST_HEADLESS=true</summary>
    public static bool Headless =>
        bool.TryParse(Environment.GetEnvironmentVariable("TEST_HEADLESS"), out var val) && val;

    /// <summary>Explicit wait saniye cinsinden (varsayılan 15)</summary>
    public static int DefaultTimeoutSeconds =>
        int.TryParse(Environment.GetEnvironmentVariable("TEST_TIMEOUT_SECONDS"), out var val)
            ? val
            : 15;
}



