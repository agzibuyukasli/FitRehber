using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.Entity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietitianClinic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DietitianClinicDbContext _context;

        public DashboardController(DietitianClinicDbContext context)
        {
            _context = context;
        }

        // GET /api/Dashboard/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var (currentUserId, isDietitian) = GetCurrentUserContext();
            var today    = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var now      = DateTime.Now;

            IQueryable<Patient>     patientsQ     = _context.Patients.Where(p => !p.IsDeleted);
            IQueryable<Appointment> appointmentsQ = _context.Appointments.Include(a => a.Patient).Where(a => !a.IsDeleted);
            IQueryable<MealPlan>    mealPlansQ    = _context.MealPlans.Where(m => !m.IsDeleted);

            if (isDietitian && currentUserId.HasValue)
            {
                patientsQ     = patientsQ.Where(p => p.UserId == currentUserId.Value);
                appointmentsQ = appointmentsQ.Where(a => a.UserId == currentUserId.Value);
                mealPlansQ    = mealPlansQ.Where(m => m.UserId == currentUserId.Value);
            }

            var pendingRequests = await appointmentsQ.CountAsync(a => a.Status == AppointmentStatus.Requested);

            var summary = new
            {
                totalDietitians      = isDietitian ? 0 : await _context.Users.CountAsync(u => !u.IsDeleted && u.Role == UserRole.Dietitian),
                totalPatients        = await patientsQ.CountAsync(),
                totalAppointments    = await appointmentsQ.CountAsync(),
                todayAppointments    = await appointmentsQ.CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow),
                upcomingAppointments = await appointmentsQ.CountAsync(a => a.AppointmentDate >= now),
                pastAppointments     = await appointmentsQ.CountAsync(a => a.AppointmentDate < now),
                activeMealPlans      = await mealPlansQ.CountAsync(m => m.Status == MealPlanStatus.Active),
                pendingRequests,
                todaySchedule = await appointmentsQ
                    .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
                    .OrderBy(a => a.AppointmentDate)
                    .Select(a => new {
                        a.Id,
                        patientName = a.Patient.FirstName + " " + a.Patient.LastName,
                        a.AppointmentDate,
                        a.DurationInMinutes,
                        status = (int)a.Status
                    })
                    .ToListAsync()
            };

            return Ok(summary);
        }

        // GET /api/Dashboard/analytics  — grafik verileri
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var (currentUserId, isDietitian) = GetCurrentUserContext();

            // Son 12 ay
            var startDate = new DateTime(DateTime.Today.AddMonths(-11).Year, DateTime.Today.AddMonths(-11).Month, 1);

            IQueryable<Patient>     patientsQ     = _context.Patients.Where(p => !p.IsDeleted);
            IQueryable<Appointment> appointmentsQ = _context.Appointments.Where(a => !a.IsDeleted);

            if (isDietitian && currentUserId.HasValue)
            {
                patientsQ     = patientsQ.Where(p => p.UserId == currentUserId.Value);
                appointmentsQ = appointmentsQ.Where(a => a.UserId == currentUserId.Value);
            }

            // Aylık hasta kaydı
            var monthlyPatients = await patientsQ
                .Where(p => p.CreatedDate >= startDate)
                .GroupBy(p => new { p.CreatedDate.Year, p.CreatedDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, count = g.Count() })
                .ToListAsync();

            // Aylık randevu
            var monthlyAppointments = await appointmentsQ
                .Where(a => a.AppointmentDate >= startDate)
                .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, count = g.Count() })
                .ToListAsync();

            // Diyetisyen dağılımı
            var dietitianDist = await (
                from p in _context.Patients.Where(p => !p.IsDeleted && p.UserId.HasValue)
                join u in _context.Users on p.UserId equals u.Id
                where !u.IsDeleted && u.Role == UserRole.Dietitian
                group p by new { u.Id, u.FirstName, u.LastName } into g
                select new { name = g.Key.FirstName + " " + g.Key.LastName, count = g.Count() }
            ).ToListAsync();

            return Ok(new { monthlyPatients, monthlyAppointments, dietitianDist });
        }

        // GET /api/Dashboard/reports  — rapor tablosu
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var (currentUserId, isDietitian) = GetCurrentUserContext();

            var query = _context.Patients
                .Where(p => !p.IsDeleted)
                .Include(p => p.User)
                .Include(p => p.Measurements)
                .Include(p => p.Appointments)
                .AsQueryable();

            if (isDietitian && currentUserId.HasValue)
                query = query.Where(p => p.UserId == currentUserId.Value);

            var data = await query
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => new {
                    patientName           = p.FirstName + " " + p.LastName,
                    email                 = p.Email,
                    dietitianName         = p.User != null ? p.User.FirstName + " " + p.User.LastName : "—",
                    totalAppointments     = p.Appointments.Count(a => !a.IsDeleted),
                    completedAppointments = p.Appointments.Count(a => !a.IsDeleted && a.Status == AppointmentStatus.Completed),
                    measurementCount      = p.Measurements.Count(),
                    latestWeight          = p.Measurements
                        .OrderByDescending(m => m.MeasurementDate)
                        .Select(m => (double?)m.Weight)
                        .FirstOrDefault(),
                    latestBmi             = p.Measurements
                        .OrderByDescending(m => m.MeasurementDate)
                        .Select(m => (double?)m.BMI)
                        .FirstOrDefault(),
                    registeredDate        = p.CreatedDate,
                    isActive              = p.IsActive
                })
                .ToListAsync();

            return Ok(data);
        }

        private (int? currentUserId, bool isDietitian) GetCurrentUserContext()
        {
            var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isDietitian = User.IsInRole(UserRole.Dietitian.ToString()) || User.IsInRole("1");
            return (int.TryParse(raw, out var id) ? id : null, isDietitian);
        }
    }
}
