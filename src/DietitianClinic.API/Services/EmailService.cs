using System.Net;
using System.Net.Mail;

namespace DietitianClinic.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code)
        {
            var smtp = _config.GetSection("Smtp");
            var host     = smtp["Host"]      ?? throw new InvalidOperationException("SMTP Host yapılandırılmamış.");
            var port     = int.Parse(smtp["Port"] ?? "587");
            var username = smtp["Username"]  ?? string.Empty;
            var password = smtp["Password"]  ?? string.Empty;
            var from     = smtp["From"]      ?? username;
            var enableSsl = bool.Parse(smtp["EnableSsl"] ?? "true");

            var body = $@"<!DOCTYPE html>
<html lang=""tr"">
<body style=""font-family:'Segoe UI',sans-serif;background:#f7f2e8;padding:32px;margin:0;"">
  <div style=""max-width:480px;margin:0 auto;background:#fff;border-radius:16px;padding:36px;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
    <div style=""display:flex;align-items:center;gap:10px;margin-bottom:6px;"">
      <div style=""width:40px;height:40px;border-radius:10px;background:linear-gradient(145deg,#fff,#efe6d8);border:1px solid #e2d8c8;display:flex;align-items:center;justify-content:center;"">
        <span style=""font-size:1.3rem;"">🌿</span>
      </div>
      <span style=""font-size:1.4rem;font-weight:800;font-style:italic;color:#3e5f4b;font-family:Georgia,serif;"">FitRehber</span>
    </div>
    <hr style=""border:none;border-top:1px solid #e2d8c8;margin:18px 0;"">
    <p style=""color:#2a3a32;font-size:1rem;"">Merhaba <strong>{System.Net.WebUtility.HtmlEncode(firstName)}</strong>,</p>
    <p style=""color:#516257;margin-top:10px;line-height:1.6;"">
      Şifre sıfırlama talebiniz alındı. Aşağıdaki kodu giriş ekranındaki ilgili alana girin:
    </p>
    <div style=""text-align:center;margin:28px 0;"">
      <span style=""display:inline-block;font-size:2.6rem;font-weight:900;letter-spacing:12px;color:#3e5f4b;background:#eef3ea;padding:18px 32px;border-radius:14px;border:1px solid #c9d6c4;"">
        {code}
      </span>
    </div>
    <p style=""color:#6a7a6f;font-size:0.85rem;line-height:1.6;"">
      Bu kod <strong>10 dakika</strong> süreyle geçerlidir.<br>
      Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın — hesabınız güvende.
    </p>
    <hr style=""border:none;border-top:1px solid #e2d8c8;margin:20px 0;"">
    <p style=""color:#9fb596;font-size:0.78rem;text-align:center;"">© 2026 FitRehber — Tüm Hakları Saklıdır</p>
  </div>
</body>
</html>";

            using var message = new MailMessage(from, toEmail)
            {
                Subject     = "FitRehber - Şifre Sıfırlama Kodu",
                Body        = body,
                IsBodyHtml  = true
            };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl   = enableSsl,
                Credentials = string.IsNullOrEmpty(username)
                    ? null
                    : new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Şifre sıfırlama kodu gönderildi: {Email}", toEmail);
        }

        public async Task SendAppointmentReminderAsync(
            string toEmail,
            string patientFirstName,
            string patientLastName,
            string dietitianName,
            string dietitianEmail,
            string dietitianPhone,
            DateTime appointmentDate,
            int durationInMinutes,
            string reason)
        {
            var smtp = _config.GetSection("Smtp");
            var host      = smtp["Host"]      ?? throw new InvalidOperationException("SMTP Host yapılandırılmamış.");
            var port      = int.Parse(smtp["Port"] ?? "587");
            var username  = smtp["Username"]  ?? string.Empty;
            var password  = smtp["Password"]  ?? string.Empty;
            var from      = smtp["From"]      ?? username;
            var enableSsl = bool.Parse(smtp["EnableSsl"] ?? "true");

            var dateStr = appointmentDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            var dayName = appointmentDate.ToString("dddd", new System.Globalization.CultureInfo("tr-TR"));
            var timeStr = appointmentDate.ToString("HH:mm");

            var patientFullName = System.Net.WebUtility.HtmlEncode($"{patientFirstName} {patientLastName}");
            var safeReason      = System.Net.WebUtility.HtmlEncode(reason);
            var safeDietitian   = System.Net.WebUtility.HtmlEncode(dietitianName);
            var safeDietEmail   = System.Net.WebUtility.HtmlEncode(dietitianEmail ?? "-");
            var safeDietPhone   = System.Net.WebUtility.HtmlEncode(dietitianPhone ?? "-");

            var body = $@"<!DOCTYPE html>
<html lang=""tr"">
<body style=""font-family:'Segoe UI',sans-serif;background:#f7f2e8;padding:32px;margin:0;"">
  <div style=""max-width:520px;margin:0 auto;background:#fff;border-radius:16px;padding:36px;box-shadow:0 4px 24px rgba(0,0,0,0.09);"">

    <div style=""display:flex;align-items:center;gap:10px;margin-bottom:6px;"">
      <div style=""width:40px;height:40px;border-radius:10px;background:linear-gradient(145deg,#fff,#efe6d8);border:1px solid #e2d8c8;display:flex;align-items:center;justify-content:center;"">
        <span style=""font-size:1.3rem;"">🌿</span>
      </div>
      <span style=""font-size:1.4rem;font-weight:800;font-style:italic;color:#3e5f4b;font-family:Georgia,serif;"">FitRehber</span>
    </div>
    <hr style=""border:none;border-top:1px solid #e2d8c8;margin:18px 0;"">

    <p style=""color:#2a3a32;font-size:1rem;margin:0 0 6px;"">Merhaba <strong>{patientFirstName}</strong>,</p>
    <p style=""color:#516257;font-size:0.95rem;line-height:1.65;margin:0 0 22px;"">
      Yarın bir randevunuz bulunmaktadır. Randevunuzu kaçırmamak için lütfen bu hatırlatmayı dikkate alın.
    </p>

    <div style=""background:#f0f7ed;border:1px solid #c9d6c4;border-radius:12px;padding:20px 22px;margin-bottom:24px;"">
      <p style=""margin:0 0 14px;font-size:0.8rem;font-weight:700;text-transform:uppercase;letter-spacing:.08em;color:#3e5f4b;"">Randevu Bilgileri</p>

      <table style=""width:100%;border-collapse:collapse;"">
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;width:40%;"">📅 Tarih</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;font-weight:600;"">{dayName}, {dateStr}</td>
        </tr>
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;"">🕐 Saat</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;font-weight:600;"">{timeStr}</td>
        </tr>
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;"">⏱ Süre</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;font-weight:600;"">{durationInMinutes} dakika</td>
        </tr>
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;"">📋 Tür</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;font-weight:600;"">{safeReason}</td>
        </tr>
      </table>
    </div>

    <div style=""background:#faf8f4;border:1px solid #e2d8c8;border-radius:12px;padding:20px 22px;margin-bottom:24px;"">
      <p style=""margin:0 0 14px;font-size:0.8rem;font-weight:700;text-transform:uppercase;letter-spacing:.08em;color:#3e5f4b;"">Diyetisyen</p>
      <table style=""width:100%;border-collapse:collapse;"">
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;width:40%;"">👤 Ad Soyad</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;font-weight:600;"">{safeDietitian}</td>
        </tr>
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;"">✉ E-posta</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;"">{safeDietEmail}</td>
        </tr>
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;"">📞 Telefon</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;"">{safeDietPhone}</td>
        </tr>
      </table>
    </div>

    <div style=""background:#faf8f4;border:1px solid #e2d8c8;border-radius:12px;padding:20px 22px;margin-bottom:24px;"">
      <p style=""margin:0 0 14px;font-size:0.8rem;font-weight:700;text-transform:uppercase;letter-spacing:.08em;color:#3e5f4b;"">Danışan</p>
      <table style=""width:100%;border-collapse:collapse;"">
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;width:40%;"">👤 Ad Soyad</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;font-weight:600;"">{patientFullName}</td>
        </tr>
        <tr>
          <td style=""padding:6px 0;color:#6a7a6f;font-size:0.83rem;"">✉ E-posta</td>
          <td style=""padding:6px 0;color:#2a3a32;font-size:0.85rem;"">{System.Net.WebUtility.HtmlEncode(toEmail)}</td>
        </tr>
      </table>
    </div>

    <p style=""color:#6a7a6f;font-size:0.85rem;line-height:1.6;margin:0 0 4px;"">
      Randevunuzu iptal etmeniz ya da değiştirmeniz gerekiyorsa lütfen diyetisyeninizle iletişime geçin.
    </p>
    <p style=""color:#9fb596;font-size:0.82rem;"">
      E-posta bildirimlerini kapatmak için FitRehber uygulamasındaki Profil → Ayarlar bölümünü kullanabilirsiniz.
    </p>

    <hr style=""border:none;border-top:1px solid #e2d8c8;margin:20px 0;"">
    <p style=""color:#9fb596;font-size:0.78rem;text-align:center;margin:0;"">© 2026 FitRehber — Tüm Hakları Saklıdır</p>
  </div>
</body>
</html>";

            using var message = new MailMessage(from, toEmail)
            {
                Subject    = $"FitRehber — Randevu Hatırlatması: {dayName}, {dateStr} {timeStr}",
                Body       = body,
                IsBodyHtml = true
            };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl   = enableSsl,
                Credentials = string.IsNullOrEmpty(username)
                    ? null
                    : new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Randevu hatırlatma maili gönderildi: {Email}, Tarih: {Date}", toEmail, appointmentDate);
        }
    }
}
