using System.Net.Mail;
using DietitianClinic.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DietitianClinic.Tests.Unit;

/// <summary>
/// SmtpClient yerine mesajı hafızada tutan test subclass.
/// </summary>
internal sealed class TestEmailService : EmailService
{
    public readonly List<(MailMessage Message, string Host, int Port, bool EnableSsl)> Sent = [];

    public TestEmailService(IConfiguration config)
        : base(config, NullLogger<EmailService>.Instance) { }

    protected override Task DispatchMailAsync(
        MailMessage message,
        string host, int port,
        string username, string password,
        bool enableSsl)
    {
        Sent.Add((message, host, port, enableSsl));
        return Task.CompletedTask;
    }
}

public class EmailServiceTests
{
    private static IConfiguration BuildConfig(bool includeHost = true) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(includeHost
                ? new Dictionary<string, string?>
                {
                    ["Smtp:Host"]      = "smtp.test.com",
                    ["Smtp:Port"]      = "587",
                    ["Smtp:Username"]  = "user@test.com",
                    ["Smtp:Password"]  = "secret",
                    ["Smtp:From"]      = "from@test.com",
                    ["Smtp:EnableSsl"] = "true"
                }
                : new Dictionary<string, string?>())
            .Build();

    // ─── SendPasswordResetCodeAsync ───────────────────────────────────

    [Fact]
    public async Task SendPasswordResetCode_ThrowsInvalidOperation_WhenHostNotConfigured()
    {
        var sut = new EmailService(BuildConfig(includeHost: false), NullLogger<EmailService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendPasswordResetCodeAsync("to@test.com", "Ali", "123456"));
    }

    [Fact]
    public async Task SendPasswordResetCode_SendsToCorrectRecipient()
    {
        var sut = new TestEmailService(BuildConfig());
        await sut.SendPasswordResetCodeAsync("recipient@test.com", "Ali", "654321");

        Assert.Single(sut.Sent);
        Assert.Equal("recipient@test.com", sut.Sent[0].Message.To[0].Address);
    }

    [Fact]
    public async Task SendPasswordResetCode_HasCorrectSubject()
    {
        var sut = new TestEmailService(BuildConfig());
        await sut.SendPasswordResetCodeAsync("to@test.com", "Ali", "999999");

        Assert.Equal("FitRehber - Şifre Sıfırlama Kodu", sut.Sent[0].Message.Subject);
    }

    [Fact]
    public async Task SendPasswordResetCode_BodyContainsCode()
    {
        var sut = new TestEmailService(BuildConfig());
        await sut.SendPasswordResetCodeAsync("to@test.com", "Ali", "112233");

        Assert.Contains("112233", sut.Sent[0].Message.Body);
    }

    [Fact]
    public async Task SendPasswordResetCode_BodyContainsFirstName()
    {
        var sut = new TestEmailService(BuildConfig());
        await sut.SendPasswordResetCodeAsync("to@test.com", "Ayşe", "000000");

        Assert.Contains("Ayşe", sut.Sent[0].Message.Body);
    }

    [Fact]
    public async Task SendPasswordResetCode_HtmlEncodesFirstName()
    {
        var sut = new TestEmailService(BuildConfig());
        await sut.SendPasswordResetCodeAsync("to@test.com", "<script>", "000000");

        Assert.DoesNotContain("<script>", sut.Sent[0].Message.Body);
        Assert.Contains("&lt;script&gt;", sut.Sent[0].Message.Body);
    }

    [Fact]
    public async Task SendPasswordResetCode_UsesSmtpFromConfig()
    {
        var sut = new TestEmailService(BuildConfig());
        await sut.SendPasswordResetCodeAsync("to@test.com", "Ali", "111111");

        Assert.Equal("smtp.test.com", sut.Sent[0].Host);
        Assert.Equal(587,             sut.Sent[0].Port);
    }

    [Fact]
    public async Task SendPasswordResetCode_MessageIsHtml()
    {
        var sut = new TestEmailService(BuildConfig());
        await sut.SendPasswordResetCodeAsync("to@test.com", "Ali", "111111");

        Assert.True(sut.Sent[0].Message.IsBodyHtml);
    }

    // ─── SendAppointmentReminderAsync ─────────────────────────────────

    [Fact]
    public async Task SendAppointmentReminder_ThrowsInvalidOperation_WhenHostNotConfigured()
    {
        var sut = new EmailService(BuildConfig(includeHost: false), NullLogger<EmailService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendAppointmentReminderAsync(
                "to@test.com", "Ali", "Veli",
                "Dr. Beslenme", "diet@test.com", "0500",
                DateTime.Today.AddDays(1), 45, "Takip"));
    }

    [Fact]
    public async Task SendAppointmentReminder_SubjectContainsDateAndTime()
    {
        var appt = new DateTime(2026, 6, 1, 10, 30, 0);
        var sut  = new TestEmailService(BuildConfig());

        await sut.SendAppointmentReminderAsync(
            "to@test.com", "Ali", "Veli",
            "Dr. Beslenme", "diet@test.com", "0500",
            appt, 45, "Takip");

        Assert.Contains("10:30", sut.Sent[0].Message.Subject);
    }

    [Fact]
    public async Task SendAppointmentReminder_BodyContainsPatientFullName()
    {
        var sut = new TestEmailService(BuildConfig());

        await sut.SendAppointmentReminderAsync(
            "to@test.com", "Fatma", "Yılmaz",
            "Dr. Beslenme", "diet@test.com", "0500",
            DateTime.Today.AddDays(1), 45, "Takip");

        Assert.Contains("Fatma", sut.Sent[0].Message.Body);
        Assert.Contains("Yılmaz", sut.Sent[0].Message.Body);
    }

    [Fact]
    public async Task SendAppointmentReminder_BodyContainsDuration()
    {
        var sut = new TestEmailService(BuildConfig());

        await sut.SendAppointmentReminderAsync(
            "to@test.com", "Ali", "Veli",
            "Dr. Beslenme", "diet@test.com", "0500",
            DateTime.Today.AddDays(1), 60, "Takip");

        Assert.Contains("60", sut.Sent[0].Message.Body);
    }

    [Fact]
    public async Task SendAppointmentReminder_HandlesNullDietitianEmail()
    {
        var sut = new TestEmailService(BuildConfig());

        var ex = await Record.ExceptionAsync(() =>
            sut.SendAppointmentReminderAsync(
                "to@test.com", "Ali", "Veli",
                "Dr. Beslenme", null!, "0500",
                DateTime.Today.AddDays(1), 45, "Takip"));

        Assert.Null(ex);
        Assert.Contains("-", sut.Sent[0].Message.Body);
    }

    [Fact]
    public async Task SendAppointmentReminder_HandlesNullDietitianPhone()
    {
        var sut = new TestEmailService(BuildConfig());

        var ex = await Record.ExceptionAsync(() =>
            sut.SendAppointmentReminderAsync(
                "to@test.com", "Ali", "Veli",
                "Dr. Beslenme", "diet@test.com", null!,
                DateTime.Today.AddDays(1), 45, "Takip"));

        Assert.Null(ex);
        Assert.Contains("-", sut.Sent[0].Message.Body);
    }

    [Fact]
    public async Task SendAppointmentReminder_UsesSslFromConfig()
    {
        var sut = new TestEmailService(BuildConfig());

        await sut.SendAppointmentReminderAsync(
            "to@test.com", "Ali", "Veli",
            "Dr. Beslenme", "diet@test.com", "0500",
            DateTime.Today.AddDays(1), 45, "Takip");

        Assert.True(sut.Sent[0].EnableSsl);
    }
}
