using DietitianClinic.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace DietitianClinic.API.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;

        private const int RunAtHourUtc = 7;

        public AppointmentReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AppointmentReminderService başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = CalculateDelayUntilNextRun();
                _logger.LogInformation("Randevu hatırlatma servisi {Delay} sonra çalışacak.", delay);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await SendRemindersAsync(stoppingToken);
                }
            }
        }

        private static TimeSpan CalculateDelayUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, RunAtHourUtc, 0, 0, DateTimeKind.Utc);

            if (now >= nextRun)
                nextRun = nextRun.AddDays(1);

            return nextRun - now;
        }

        private async Task SendRemindersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Randevu hatırlatma maili gönderme işlemi başladı: {Time}", DateTime.UtcNow);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DietitianClinicDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var tomorrow = DateTime.Today.AddDays(1);
                var dayAfter = tomorrow.AddDays(1);

                var appointments = await db.Appointments
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted
                                && a.AppointmentDate >= tomorrow
                                && a.AppointmentDate < dayAfter
                                && a.Status != Entity.Models.AppointmentStatus.Cancelled)
                    .Include(a => a.Patient)
                    .Include(a => a.User)
                    .ToListAsync(ct);

                _logger.LogInformation("Yarın için {Count} randevu bulundu.", appointments.Count);

                foreach (var appt in appointments)
                {
                    if (appt.Patient == null || appt.User == null)
                        continue;

                    if (!appt.Patient.EmailNotificationsEnabled)
                    {
                        _logger.LogInformation("E-posta bildirimi kapalı, atlandı: PatientId={Id}", appt.PatientId);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(appt.Patient.Email))
                        continue;

                    try
                    {
                        await emailService.SendAppointmentReminderAsync(
                            toEmail: appt.Patient.Email,
                            patientFirstName: appt.Patient.FirstName,
                            patientLastName: appt.Patient.LastName,
                            dietitianName: $"{appt.User.FirstName} {appt.User.LastName}",
                            dietitianEmail: appt.User.Email,
                            dietitianPhone: appt.User.Phone,
                            appointmentDate: appt.AppointmentDate,
                            durationInMinutes: appt.DurationInMinutes,
                            reason: appt.Reason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Hatırlatma maili gönderilemedi: PatientId={Id}, Email={Email}",
                            appt.PatientId, appt.Patient.Email);
                    }
                }

                _logger.LogInformation("Randevu hatırlatma maili gönderme işlemi tamamlandı: {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu hatırlatma servisi genel hata.");
            }
        }
    }
}
