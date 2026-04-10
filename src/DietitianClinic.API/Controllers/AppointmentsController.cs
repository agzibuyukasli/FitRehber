using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DietitianClinic.API.Models.Requests;
using DietitianClinic.API.Models.Response;
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
    public class AppointmentsController : ControllerBase
    {
        private readonly DietitianClinicDbContext _context;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(DietitianClinicDbContext context, ILogger<AppointmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAppointments()
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;
                var query = _context.Appointments
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted)
                    .Include(a => a.Patient)
                    .Include(a => a.User)
                    .AsQueryable();

                if (isDietitian && currentUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == currentUserId.Value && a.Patient.UserId == currentUserId.Value);
                }

                var now = DateTime.Now;
                var appointments = await query
                    .OrderBy(a => a.AppointmentDate)
                    .Select(a => new
                    {
                        a.Id,
                        a.PatientId,
                        patientName = a.Patient.FirstName + " " + a.Patient.LastName,
                        a.UserId,
                        dietitianName = a.User.FirstName + " " + a.User.LastName,
                        a.AppointmentDate,
                        a.DurationInMinutes,
                        status = (int)a.Status,
                        a.Reason,
                        isPast = a.AppointmentDate < now
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevuları getirme hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpGet("my-appointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAppointments()
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!currentUserId.HasValue)
                    return Unauthorized(new ApiResponse { Success = false, Message = "Geçersiz token" });

                var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;

                var query = _context.Appointments
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted)
                    .Include(a => a.Patient)
                    .Include(a => a.User)
                    .AsQueryable();

                if (isDietitian)
                {
                    query = query.Where(a => a.UserId == currentUserId.Value);
                }
                else
                {
                    query = query.Where(a => a.Patient.Email == currentUserEmail);
                }

                var now = DateTime.Now;
                var appointments = await query
                    .OrderBy(a => a.AppointmentDate)
                    .Select(a => new
                    {
                        a.Id,
                        a.PatientId,
                        patientName = a.Patient.FirstName + " " + a.Patient.LastName,
                        a.UserId,
                        dietitianName = a.User.FirstName + " " + a.User.LastName,
                        a.AppointmentDate,
                        a.DurationInMinutes,
                        status = (int)a.Status,
                        a.Reason,
                        isPast = a.AppointmentDate < now
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcının randevularını getirme hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpGet("booked-slots")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookedSlots([FromQuery] int dietitianId)
        {
            try
            {
                if (dietitianId <= 0)
                    return BadRequest(new { success = false, message = "Geçersiz diyetisyen ID" });

                var slots = await _context.Appointments
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted && a.UserId == dietitianId && a.Status != AppointmentStatus.Cancelled)
                    .Select(a => new { a.AppointmentDate, a.DurationInMinutes })
                    .ToListAsync();

                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dolu slotları getirme hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentById(int id)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var query = _context.Appointments
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted && a.Id == id)
                    .Include(a => a.Patient)
                    .Include(a => a.User)
                    .AsQueryable();

                if (isDietitian && currentUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == currentUserId.Value && a.Patient.UserId == currentUserId.Value);
                }

                var appointment = await query
                    .Select(a => new
                    {
                        a.Id,
                        a.PatientId,
                        patientName = a.Patient.FirstName + " " + a.Patient.LastName,
                        a.AppointmentDate,
                        a.DurationInMinutes,
                        status = (int)a.Status,
                        a.Reason
                    })
                    .FirstOrDefaultAsync();

                if (appointment == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Randevu bulunamadı" });
                }

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu getirme hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!isDietitian)
                {
                    return Forbid();
                }
                if (!currentUserId.HasValue)
                {
                    return Unauthorized(new ApiResponse { Success = false, Message = "Geçersiz kullanıcı oturumu" });
                }
                if (request.PatientId <= 0 || request.DurationInMinutes <= 0 || request.AppointmentDate == default || string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Randevu bilgileri eksik veya geçersiz" });
                }

                var patientQuery = _context.Patients.Where(p => !p.IsDeleted && p.Id == request.PatientId);
                if (isDietitian)
                {
                    patientQuery = patientQuery.Where(p => p.UserId == currentUserId.Value);
                }

                var patientExists = await patientQuery.AnyAsync();
                if (!patientExists)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Hasta bulunamadı" });
                }

                var appointment = new Appointment
                {
                    PatientId = request.PatientId,
                    UserId = currentUserId.Value,
                    AppointmentDate = request.AppointmentDate.Kind == DateTimeKind.Unspecified
                        ? request.AppointmentDate
                        : request.AppointmentDate.ToLocalTime(),
                    DurationInMinutes = request.DurationInMinutes,
                    Status = (AppointmentStatus)request.Status,
                    Reason = request.Reason.Trim()
                };

                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, new
                {
                    id = appointment.Id,
                    patientId = appointment.PatientId,
                    appointmentDate = appointment.AppointmentDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu oluşturma hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpPost("request")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestAppointment([FromBody] RequestAppointmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (isDietitian)
                    return Forbid();

                var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                    return Unauthorized(new ApiResponse { Success = false, Message = "Geçersiz kullanıcı e-postası" });

                if (request.DurationInMinutes <= 0 || request.AppointmentDate == default || string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Randevu bilgileri eksik veya geçersiz" });
                }

                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => !p.IsDeleted && p.Email == currentUserEmail);
                if (patient == null)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Hasta kaydınız bulunamadı" });
                }

                if (!patient.UserId.HasValue)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Diyetisyen atanmadı. Lütfen önce diyetisyen seçiniz." });
                }

                var appointmentDate = request.AppointmentDate.Kind == DateTimeKind.Unspecified
                    ? request.AppointmentDate
                    : request.AppointmentDate.ToLocalTime();

                if (appointmentDate <= DateTime.Now)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Randevu tarihi geçmişte olamaz" });
                }

                var appointment = new Appointment
                {
                    PatientId = patient.Id,
                    UserId = patient.UserId.Value,
                    AppointmentDate = appointmentDate,
                    DurationInMinutes = request.DurationInMinutes,
                    Status = AppointmentStatus.Requested,
                    Reason = request.Reason.Trim()
                };

                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, new
                {
                    id = appointment.Id,
                    patientId = appointment.PatientId,
                    appointmentDate = appointment.AppointmentDate,
                    status = (int)appointment.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu talep hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpPut("{id}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!isDietitian || !currentUserId.HasValue)
                    return Forbid();

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => !a.IsDeleted && a.Id == id && a.UserId == currentUserId.Value);

                if (appointment == null)
                    return NotFound(new ApiResponse { Success = false, Message = "Randevu bulunamadı" });

                if (appointment.Status != AppointmentStatus.Requested)
                    return BadRequest(new ApiResponse { Success = false, Message = "Sadece talep edilen randevular onaylanabilir" });

                if (appointment.AppointmentDate <= DateTime.Now)
                    return BadRequest(new ApiResponse { Success = false, Message = "Geçmiş tarihli randevular onaylanamaz" });

                var hasConflict = await HasAppointmentConflict(currentUserId.Value, appointment.AppointmentDate, appointment.DurationInMinutes, appointment.Id);
                if (hasConflict)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Bu tarih ve saatte zaten bir randevu var." });
                }

                appointment.Status = AppointmentStatus.Scheduled;
                appointment.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = "Randevu onaylandı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu onaylama hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!isDietitian)
                {
                    return Forbid();
                }
                if (request.DurationInMinutes <= 0 || request.AppointmentDate == default || string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Randevu bilgileri eksik veya geçersiz" });
                }
                var query = _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => !a.IsDeleted && a.Id == id)
                    .AsQueryable();

                if (isDietitian && currentUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == currentUserId.Value && a.Patient.UserId == currentUserId.Value);
                }

                var appointment = await query.FirstOrDefaultAsync();
                if (appointment == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Randevu bulunamadı" });
                }

                appointment.AppointmentDate = request.AppointmentDate.Kind == DateTimeKind.Unspecified
                    ? request.AppointmentDate
                    : request.AppointmentDate.ToLocalTime();
                appointment.DurationInMinutes = request.DurationInMinutes;
                appointment.Status = (AppointmentStatus)request.Status;
                appointment.Reason = request.Reason.Trim();

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = "Randevu güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu güncelleme hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!isDietitian)
                {
                    return Forbid();
                }
                var query = _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => !a.IsDeleted && a.Id == id)
                    .AsQueryable();

                if (isDietitian && currentUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == currentUserId.Value && a.Patient.UserId == currentUserId.Value);
                }

                var appointment = await query.FirstOrDefaultAsync();
                if (appointment == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Randevu bulunamadı" });
                }

                appointment.IsDeleted = true;
                appointment.DeletedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu silme hatası");
                return StatusCode(500, new { success = false, message = "Hata oluştu" });
            }
        }

        private (int? currentUserId, bool isDietitian) GetCurrentUserContext()
        {
            var currentUserIdRaw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isDietitian = User.IsInRole(UserRole.Dietitian.ToString()) || User.IsInRole("1");

            return (int.TryParse(currentUserIdRaw, out var currentUserId) ? currentUserId : null, isDietitian);
        }

        private async Task<bool> HasAppointmentConflict(int dietitianId, DateTime appointmentDate, int durationInMinutes, int? excludeAppointmentId = null)
        {
            var start = appointmentDate;
            var end = appointmentDate.AddMinutes(durationInMinutes);

            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.UserId == dietitianId && a.Status != AppointmentStatus.Cancelled);

            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            return await query.AnyAsync(a =>
                a.AppointmentDate < end &&
                a.AppointmentDate.AddMinutes(a.DurationInMinutes) > start
            );
        }
    }
}
