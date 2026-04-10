using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DietitianClinic.DataAccess.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietitianClinic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DailyTrackingController : ControllerBase
    {
        private readonly DietitianClinicDbContext _context;

        public DailyTrackingController(DietitianClinicDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }

        private string? CurrentUserEmail()
        {
            return User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value;
        }

        private async Task<DietitianClinic.Entity.Models.Patient?> FindPatientForCurrentUser()
        {
            var email = CurrentUserEmail();
            if (string.IsNullOrEmpty(email)) return null;
            return await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email && !p.IsDeleted);
        }

        // Danışan: bugünkü su ve adım bilgisini kaydet (upsert)
        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] DailyTrackingRequest req)
        {
            var patient = await FindPatientForCurrentUser();
            if (patient == null) return NotFound(new { error = "Danışan profili bulunamadı." });

            var today = DateTime.UtcNow.Date;

            await _context.Database.ExecuteSqlAsync(
                $@"IF EXISTS (SELECT 1 FROM DailyTrackings WHERE PatientId = {patient.Id} AND TrackingDate = {today})
                    UPDATE DailyTrackings SET WaterLiters = {req.WaterLiters}, StepsCount = {req.StepsCount}
                    WHERE PatientId = {patient.Id} AND TrackingDate = {today}
                   ELSE
                    INSERT INTO DailyTrackings (PatientId, TrackingDate, WaterLiters, StepsCount, CreatedDate)
                    VALUES ({patient.Id}, {today}, {req.WaterLiters}, {req.StepsCount}, GETUTCDATE())");

            return Ok();
        }

        // Danışan: kendi son 14 günlük takip verisini görür
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var patient = await FindPatientForCurrentUser();
            if (patient == null) return Ok(Array.Empty<DailyTrackingDto>());

            var since = DateTime.UtcNow.Date.AddDays(-13);
            var rows = await _context.Database
                .SqlQuery<DailyTrackingDto>($"SELECT Id, PatientId, TrackingDate, WaterLiters, StepsCount FROM DailyTrackings WHERE PatientId = {patient.Id} AND TrackingDate >= {since}")
                .OrderByDescending(r => r.TrackingDate)
                .ToListAsync();
            return Ok(rows);
        }

        // Diyetisyen: tek bir danışanın takip geçmişini görür
        [HttpGet("patients/{patientId:int}")]
        public async Task<IActionResult> GetForPatient(int patientId)
        {
            var me = CurrentUserId();
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId && !p.IsDeleted);
            if (patient == null) return NotFound();

            var since = DateTime.UtcNow.Date.AddDays(-13);
            var rows = await _context.Database
                .SqlQuery<DailyTrackingDto>($"SELECT Id, PatientId, TrackingDate, WaterLiters, StepsCount FROM DailyTrackings WHERE PatientId = {patientId} AND TrackingDate >= {since}")
                .OrderByDescending(r => r.TrackingDate)
                .ToListAsync();
            return Ok(rows);
        }

        // Diyetisyen: tüm danışanlarının en son takip verisini görür
        [HttpGet("my-patients")]
        public async Task<IActionResult> GetMyPatients()
        {
            var me = CurrentUserId();
            var rows = await _context.Database
                .SqlQuery<PatientTrackingDto>($@"
                    SELECT p.Id AS PatientId,
                           p.FirstName + ' ' + p.LastName AS PatientName,
                           dt.TrackingDate,
                           dt.WaterLiters,
                           dt.StepsCount
                    FROM Patients p
                    LEFT JOIN (
                        SELECT dt1.PatientId, dt1.TrackingDate, dt1.WaterLiters, dt1.StepsCount
                        FROM DailyTrackings dt1
                        INNER JOIN (
                            SELECT PatientId, MAX(TrackingDate) AS MaxDate
                            FROM DailyTrackings
                            GROUP BY PatientId
                        ) dt2 ON dt1.PatientId = dt2.PatientId AND dt1.TrackingDate = dt2.MaxDate
                    ) dt ON p.Id = dt.PatientId
                    WHERE p.UserId = {me} AND p.IsDeleted = 0")
                .OrderBy(r => r.PatientName)
                .ToListAsync();
            return Ok(rows);
        }
    }

    public class DailyTrackingRequest
    {
        public double? WaterLiters { get; set; }
        public int? StepsCount { get; set; }
    }

    public class DailyTrackingDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime TrackingDate { get; set; }
        public double? WaterLiters { get; set; }
        public int? StepsCount { get; set; }
    }

    public class PatientTrackingDto
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? TrackingDate { get; set; }
        public double? WaterLiters { get; set; }
        public int? StepsCount { get; set; }
    }
}
