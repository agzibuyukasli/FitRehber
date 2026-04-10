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

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var (currentUserId, isDietitian) = GetCurrentUserContext();
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var now = DateTime.Now;

            IQueryable<Patient> patientsQuery = _context.Patients.Where(p => !p.IsDeleted);
            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => !a.IsDeleted);
            IQueryable<MealPlan> mealPlansQuery = _context.MealPlans.Where(m => !m.IsDeleted);

            if (isDietitian && currentUserId.HasValue)
            {
                patientsQuery = patientsQuery.Where(p => p.UserId == currentUserId.Value);
                appointmentsQuery = appointmentsQuery.Where(a => a.UserId == currentUserId.Value);
                mealPlansQuery = mealPlansQuery.Where(m => m.UserId == currentUserId.Value);
            }

            var summary = new
            {
                totalDietitians = isDietitian ? 0 : await _context.Users.CountAsync(u => !u.IsDeleted && u.Role == UserRole.Dietitian),
                totalPatients = await patientsQuery.CountAsync(),
                totalAppointments = await appointmentsQuery.CountAsync(),
                todayAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow),
                upcomingAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate >= now),
                pastAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate < now),
                activeMealPlans = await mealPlansQuery.CountAsync(m => m.Status == MealPlanStatus.Active),
                todaySchedule = await appointmentsQuery
                    .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
                    .OrderBy(a => a.AppointmentDate)
                    .Select(a => new
                    {
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

        private (int? currentUserId, bool isDietitian) GetCurrentUserContext()
        {
            var currentUserIdRaw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isDietitian = User.IsInRole(UserRole.Dietitian.ToString()) || User.IsInRole("1");

            return (int.TryParse(currentUserIdRaw, out var currentUserId) ? currentUserId : null, isDietitian);
        }
    }
}
