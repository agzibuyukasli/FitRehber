using System.Diagnostics;
using System.Net.Http;
using NUnit.Framework;

namespace DietitianClinic.Tests.UI.Infrastructure;

/// <summary>
/// Assembly seviyesinde tek seferlik kurulum.
/// Testler başlamadan önce frontend ve backend'in çalıştığından emin olur;
/// servisler ayakta değilse docker compose ile otomatik başlatır.
/// </summary>
[SetUpFixture]
public class AssemblySetup
{
    private static bool _startedByUs;

    [OneTimeSetUp]
    public void StartInfrastructure()
    {
        if (BothServicesReady())
        {
            TestContext.Progress.WriteLine("[Setup] Servisler zaten çalışıyor.");
            return;
        }

        TestContext.Progress.WriteLine("[Setup] Servisler çalışmıyor — docker compose başlatılıyor...");

        var repoRoot = FindRepoRoot();
        RunDockerCompose("up -d", repoRoot);
        _startedByUs = true;

        WaitUntilReady(TestConfig.ApiBaseUrl, "Backend API", TimeSpan.FromMinutes(3));
        WaitUntilReady(TestConfig.BaseUrl,    "Frontend",    TimeSpan.FromMinutes(3));

        TestContext.Progress.WriteLine("[Setup] Tüm servisler hazır. Testler başlıyor.");
    }

    [OneTimeTearDown]
    public void StopInfrastructure()
    {
        if (!_startedByUs) return;

        // TEST_TEARDOWN=true verilmişse (genellikle CI'da) servisleri kapat.
        // Varsayılan: çalışır bırak, geliştirici ortamını bozma.
        var teardown = Environment.GetEnvironmentVariable("TEST_TEARDOWN");
        if (!string.Equals(teardown, "true", StringComparison.OrdinalIgnoreCase)) return;

        TestContext.Progress.WriteLine("[Setup] docker compose durduruluyor...");
        RunDockerCompose("down", FindRepoRoot());
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static bool BothServicesReady() =>
        IsReachable(TestConfig.BaseUrl) && IsApiDatabaseReady();

    // Sadece TCP yanıtı değil, DB bağlantısını da doğrular.
    private static bool IsApiDatabaseReady()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = client.GetAsync($"{TestConfig.ApiBaseUrl}/api/health/database")
                                 .GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static bool IsReachable(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            client.GetAsync(url).GetAwaiter().GetResult();
            return true;
        }
        catch { return false; }
    }

    private static void WaitUntilReady(string url, string name, TimeSpan timeout)
    {
        var isApi = url == TestConfig.ApiBaseUrl;
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var ready = isApi ? IsApiDatabaseReady() : IsReachable(url);
            if (ready)
            {
                TestContext.Progress.WriteLine($"[Setup] {name} hazır: {url}");
                return;
            }
            TestContext.Progress.WriteLine($"[Setup] {name} bekleniyor: {url}");
            Thread.Sleep(3000);
        }
        throw new TimeoutException(
            $"{name} ({url}), {timeout.TotalMinutes} dakika içinde başlamadı. " +
            "Hata için: docker compose logs");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Repo kökü bulunamadı: docker-compose.yml aranıyor ancak hiçbir üst dizinde yok.");
    }

    private static void RunDockerCompose(string args, string workDir)
    {
        var psi = new ProcessStartInfo("docker", $"compose {args}")
        {
            WorkingDirectory  = workDir,
            UseShellExecute   = false,
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("docker compose başlatılamadı.");
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"'docker compose {args}' başarısız (exit: {proc.ExitCode}). " +
                "Detay için: docker compose logs");
    }
}
