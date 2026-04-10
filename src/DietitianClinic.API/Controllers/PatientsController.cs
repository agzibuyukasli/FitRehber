using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using DietitianClinic.API.Models.Requests;
using DietitianClinic.API.Models.Response;
using DietitianClinic.API.Models.Responses;
using DietitianClinic.Business.Exceptions;
using DietitianClinic.Business.Interfaces;
using DietitianClinic.Business.Services;
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
    public class PatientsController : ControllerBase
    {
        private readonly PatientService _patientService;
        private readonly UserService _userService;
        private readonly IPasswordService _passwordService;
        private readonly DietitianClinicDbContext _context;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(
            PatientService patientService,
            UserService userService,
            IPasswordService passwordService,
            DietitianClinicDbContext context,
            ILogger<PatientsController> logger)
        {
            _patientService = patientService;
            _userService = userService;
            _passwordService = passwordService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPatients()
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var patients = await _patientService.GetAllPatientsAsync(currentUserId, isDietitian);
                var dietitianMap = await _context.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted && u.Role == UserRole.Dietitian)
                    .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

                // Hasta email → login User.Id eşleşmesi (C# tarafında yapılır, EF çevirisi sorun çıkarmaz)
                var allUserIdsByEmail = await _context.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted)
                    .Select(u => new { u.Id, u.Email })
                    .ToListAsync();
                var patientUserMap = allUserIdsByEmail
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .GroupBy(u => u.Email.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var response = patients.Select(p =>
                {
                    var latestMeasurement = p.Measurements
                        .OrderByDescending(m => m.MeasurementDate)
                        .FirstOrDefault();
                    var dietitianName = p.User == null
                        ? (p.UserId.HasValue && dietitianMap.TryGetValue(p.UserId.Value, out var mappedName) ? mappedName : null)
                        : $"{p.User.FirstName} {p.User.LastName}";

                    var emailKey = p.Email?.Trim().ToLower() ?? string.Empty;
                    int? patientUserId = !string.IsNullOrEmpty(emailKey) && patientUserMap.TryGetValue(emailKey, out var id) ? id : (int?)null;

                    return new PatientListItemResponse
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        DietitianId = p.UserId,
                        PatientUserId = patientUserId,
                        FullName = $"{p.FirstName} {p.LastName}",
                        DietitianName = dietitianName,
                        Email = p.Email,
                        Phone = p.Phone,
                        City = p.City,
                        Age = CalculateAge(p.BirthDate),
                        LatestWeight = latestMeasurement?.Weight,
                        LatestHeight = latestMeasurement?.Height,
                        LatestBmi = latestMeasurement?.BMI,
                        LastMeasurementDate = latestMeasurement?.MeasurementDate,
                        IsActive = p.IsActive
                    };
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hastalari getirme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatientById(int id)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var patient = await _patientService.GetPatientByIdAsync(id, currentUserId, isDietitian);
                var dietitianName = patient.User == null && patient.UserId.HasValue
                    ? await _context.Users
                        .AsNoTracking()
                        .Where(u => !u.IsDeleted && u.Role == UserRole.Dietitian && u.Id == patient.UserId.Value)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefaultAsync()
                    : patient.User == null ? null : $"{patient.User.FirstName} {patient.User.LastName}";

                var response = new PatientResponse
                {
                    Id = patient.Id,
                    UserId = patient.UserId,
                    DietitianId = patient.UserId,
                    DietitianName = dietitianName,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    BirthDate = patient.BirthDate,
                    Gender = (int)patient.Gender,
                    Address = patient.Address,
                    City = patient.City,
                    MedicalHistory = patient.MedicalHistory,
                    Allergies = patient.Allergies,
                    Notes = patient.Notes,
                    IsActive = patient.IsActive,
                    CreatedDate = patient.CreatedDate
                };

                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Hasta bulunamadi");
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta getirme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .FirstOrDefault();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = firstError ?? "Gecersiz istek."
                    });
                }

                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var ownerUserId = isDietitian ? currentUserId : (request.UserId ?? request.DietitianId);

                if (!isDietitian && !ownerUserId.HasValue)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Lutfen bir diyetisyen secin" });
                }

                if (ownerUserId.HasValue)
                {
                    var dietitianExists = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => !u.IsDeleted && u.Id == ownerUserId.Value && u.Role == UserRole.Dietitian);
                    if (!dietitianExists)
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "Secilen diyetisyen bulunamadi" });
                    }
                }

                // Diyetisyen/admin rolünde aktif kullanıcı varsa kesin engelle
                var existingNonPatientUser = await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted && u.Role != UserRole.Patient);
                if (existingNonPatientUser != null)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Bu e-posta ile kayitli bir kullanici zaten var" });
                }

                // Aktif hasta kaydı VE aktif hasta kullanıcısı varsa engelle (gerçek duplicate)
                var activePatientUser = await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(u => u.Email == request.Email && !u.IsDeleted && u.Role == UserRole.Patient);
                var activePatientRecord = await _context.Patients
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(p => p.Email == request.Email && !p.IsDeleted);
                if (activePatientRecord && activePatientUser)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Bu e-posta ile kayitli bir hasta zaten var" });
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Danisan girisi icin sifre zorunludur" });
                }

                // Sahipsiz hasta kaydı varsa (kullanıcısı silinmiş/yok ama hasta kaydı aktif) temizle
                if (activePatientRecord && !activePatientUser)
                {
                    var orphan = await _context.Patients
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Email == request.Email && !p.IsDeleted);
                    if (orphan != null)
                    {
                        orphan.IsDeleted = true;
                        orphan.DeletedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                // Kullanıcı kaydını oluştur veya yeniden aktif et
                var anyExistingUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                if (anyExistingUser != null)
                {
                    anyExistingUser.FirstName = request.FirstName;
                    anyExistingUser.LastName = request.LastName;
                    anyExistingUser.Phone = request.Phone ?? string.Empty;
                    anyExistingUser.PasswordHash = _passwordService.HashPassword(request.Password);
                    anyExistingUser.IsDeleted = false;
                    anyExistingUser.IsActive = true;
                    anyExistingUser.DeletedDate = null;
                    anyExistingUser.Role = UserRole.Patient;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _userService.RegisterUserAsync(
                        request.FirstName,
                        request.LastName,
                        request.Email,
                        request.Password,
                        request.Phone,
                        string.Empty,
                        string.Empty,
                        UserRole.Patient.ToString());
                }

                var patient = new Patient
                {
                    UserId = ownerUserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    BirthDate = request.BirthDate,
                    Gender = (Gender)request.Gender,
                    Address = request.Address,
                    City = request.City,
                    MedicalHistory = request.MedicalHistory,
                    Allergies = request.Allergies,
                    Notes = request.Notes
                };

                await _context.Patients.AddAsync(patient);
                await _context.SaveChangesAsync();

                var response = new PatientResponse
                {
                    Id = patient.Id,
                    UserId = ownerUserId,
                    DietitianId = ownerUserId,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    BirthDate = patient.BirthDate,
                    Gender = (int)patient.Gender,
                    Address = patient.Address,
                    City = patient.City,
                    MedicalHistory = patient.MedicalHistory,
                    Allergies = patient.Allergies,
                    Notes = patient.Notes,
                    IsActive = patient.IsActive,
                    CreatedDate = patient.CreatedDate
                };

                return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta olusturma hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] UpdatePatientRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .FirstOrDefault();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = firstError ?? "Gecersiz istek."
                    });
                }

                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var isAdmin = User.IsInRole(UserRole.Admin.ToString())
                    || User.IsInRole(UserRole.SuperAdmin.ToString())
                    || User.IsInRole("0")
                    || User.IsInRole("2");

                var patientQuery = _context.Patients.Where(p => !p.IsDeleted && p.Id == id);
                if (isDietitian && currentUserId.HasValue)
                {
                    patientQuery = patientQuery.Where(p => p.UserId == currentUserId.Value);
                }

                var patient = await patientQuery.FirstOrDefaultAsync();
                if (patient == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Hasta bulunamadi" });
                }

                int? ownerUserId;
                if (!isDietitian && !isAdmin)
                {
                    var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                        ?? User.FindFirst(ClaimTypes.Email)?.Value;
                    if (string.IsNullOrWhiteSpace(currentUserEmail) || !string.Equals(patient.Email, currentUserEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        return Forbid();
                    }
                    ownerUserId = patient.UserId;
                    if (!ownerUserId.HasValue)
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "Diyetisyen atanmadığı için güncelleme yapılamıyor" });
                    }
                }
                else
                {
                    ownerUserId = request.UserId ?? request.DietitianId ?? (isDietitian ? currentUserId : null);
                    if (!ownerUserId.HasValue)
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "Lutfen bir diyetisyen secin" });
                    }

                    var dietitianExists = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => !u.IsDeleted && u.Id == ownerUserId.Value && u.Role == UserRole.Dietitian);
                    if (!dietitianExists)
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "Secilen diyetisyen bulunamadi" });
                    }
                }

                // Kullanıcı girişi 'Users' tablosu üzerinden yapılıyor.
                // Admin panelinde e-posta/şifre güncellendiğinde, burada da aynı bilgileri
                // patient'in kullanıcı kaydına yansıtıyoruz.
                var oldPatientEmail = patient.Email;
                var requestPassword = request.Password; // boş/null => güncelleme yapma

                var emailExists = await _context.Patients
                    .AsNoTracking()
                    .AnyAsync(p => !p.IsDeleted && p.Id != id && p.Email == request.Email);
                if (emailExists)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Bu e-posta ile kayitli bir hasta zaten var" });
                }

                // Users tablosunda (role=Patient) e-posta tekil olmalı.
                // Şayet e-posta değiştiriliyorsa ve aynı e-posta başka bir patient'e aitse engelle.
                var existingPatientUser = await _context.Users
                    .FirstOrDefaultAsync(u => !u.IsDeleted && u.Role == UserRole.Patient && u.Email == oldPatientEmail);

                if (existingPatientUser == null)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Hasta kullanicisi bulunamadi." });
                }

                if (!string.Equals(oldPatientEmail, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var userEmailExists = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => !u.IsDeleted && u.Role == UserRole.Patient && u.Email == request.Email && u.Id != existingPatientUser.Id);

                    if (userEmailExists)
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "Bu e-posta ile kayitli bir kullanici zaten var." });
                    }
                }

                patient.UserId = ownerUserId;
                patient.FirstName = request.FirstName;
                patient.LastName = request.LastName;
                patient.Email = request.Email;
                patient.Phone = request.Phone;
                patient.Address = request.Address;
                patient.City = request.City;
                patient.MedicalHistory = request.MedicalHistory;
                patient.Allergies = request.Allergies;
                patient.Notes = request.Notes;

                // Login için Users tablosundaki e-posta/şifre bilgisini de güncelle.
                existingPatientUser.Email = request.Email;
                existingPatientUser.Phone = request.Phone;
                if (!string.IsNullOrWhiteSpace(requestPassword))
                {
                    existingPatientUser.PasswordHash = _passwordService.HashPassword(requestPassword);
                }

                await _context.SaveChangesAsync();
                return Ok(new ApiResponse { Success = true, Message = "Hasta guncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta guncelleme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePatient(int id)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                await _patientService.DeletePatientAsync(id, currentUserId, isDietitian);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Hasta bulunamadi");
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta silme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpGet("{id}/measurements")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPatientMeasurements(int id)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var measurements = await _patientService.GetPatientMeasurementsAsync(id, currentUserId, isDietitian);

                return Ok(measurements.Select(m => new PatientMeasurementResponse
                {
                    Id = m.Id,
                    PatientId = m.PatientId,
                    MeasurementDate = m.MeasurementDate,
                    Weight = m.Weight,
                    Height = m.Height,
                    Bmi = m.BMI,
                    WaistCircumference = m.WaistCircumference,
                    HipCircumference = m.HipCircumference,
                    BodyFatPercentage = m.BodyFatPercentage,
                    Notes = m.Notes
                }));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Hasta bulunamadi");
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta olcumleri getirme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpPost("{id}/measurements")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> CreatePatientMeasurement(int id, [FromBody] CreatePatientMeasurementRequest request)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                var measurement = new PatientMeasurement
                {
                    MeasurementDate = request.MeasurementDate,
                    Weight = request.Weight,
                    Height = request.Height,
                    WaistCircumference = request.WaistCircumference,
                    HipCircumference = request.HipCircumference,
                    BodyFatPercentage = request.BodyFatPercentage,
                    Notes = request.Notes ?? string.Empty
                };

                var measurementId = await _patientService.AddPatientMeasurementAsync(id, measurement, currentUserId, isDietitian);

                return CreatedAtAction(nameof(GetPatientMeasurements), new { id }, new
                {
                    id = measurementId,
                    patientId = id,
                    bmi = measurement.BMI
                });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Hasta bulunamadi");
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta olcumu ekleme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpGet("deleted")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeletedPatients()
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                // DeletedPatients tablosu BaseEntity'den türetilmediği için global query filter yok
                var query = _context.DeletedPatients.AsNoTracking();
                if (isDietitian && currentUserId.HasValue)
                    query = query.Where(p => p.OwnerUserId == currentUserId.Value);

                var patients = await query.OrderByDescending(p => p.DeletedAt).ToListAsync();
                var response = patients.Select(p => new
                {
                    Id = p.OriginalPatientId,
                    p.FirstName, p.LastName, p.Email, p.Phone,
                    p.City, p.Address, p.MedicalHistory, p.Allergies, p.Notes,
                    p.CreatedDate,
                    Gender = (int)p.Gender,
                    BirthDate = p.BirthDate
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Silinen hastaları getirme hatası");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpDelete("purge-deleted")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> PurgeDeletedPatients()
        {
            try
            {
                var archivedPatients = await _context.DeletedPatients.ToListAsync();
                if (!archivedPatients.Any())
                    return Ok(new ApiResponse { Success = true, Message = "Silinecek hasta bulunamadi" });

                var archivedEmails = archivedPatients.Select(p => p.Email).Distinct().ToList();

                // Patients tablosundaki soft-delete kayıtları hard-delete et
                var softDeletedPatients = await _context.Patients
                    .IgnoreQueryFilters()
                    .Where(p => p.IsDeleted && archivedEmails.Contains(p.Email))
                    .ToListAsync();

                // Users tablosundaki deaktif hasta kullanıcılarını hard-delete et
                var deletedUsers = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.IsDeleted && u.Role == UserRole.Patient && archivedEmails.Contains(u.Email))
                    .ToListAsync();

                _context.DeletedPatients.RemoveRange(archivedPatients);
                _context.Patients.RemoveRange(softDeletedPatients);
                _context.Users.RemoveRange(deletedUsers);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = $"{archivedPatients.Count} hasta veritabanından temizlendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta temizleme hatası");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        private (int? currentUserId, bool isDietitian) GetCurrentUserContext()
        {
            var currentUserIdRaw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isDietitian = User.IsInRole(UserRole.Dietitian.ToString()) || User.IsInRole("1");

            return (int.TryParse(currentUserIdRaw, out var currentUserId) ? currentUserId : null, isDietitian);
        }

        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!currentUserId.HasValue)
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz token" });
                var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz kullanici e-postasi" });

                var patient = await _patientService.GetPatientByEmailAsync(currentUserEmail);
                if (patient == null)
                    return NotFound(new ApiResponse { Success = false, Message = "Hasta kaydiniz bulunamadi" });

                var latestMeasurement = patient.Measurements
                    .OrderByDescending(m => m.MeasurementDate)
                    .FirstOrDefault();

                // patient.User is the dietitian when created via PatientsController.
                // Also check appointments as fallback.
                var dietitianUser = (patient.User != null && patient.User.Role == UserRole.Dietitian)
                    ? patient.User
                    : patient.Appointments
                        .OrderByDescending(a => a.AppointmentDate)
                        .Select(a => a.User)
                        .FirstOrDefault(u => u != null && u.Role == UserRole.Dietitian);

                // Direct DB fallback: if UserId is set but User wasn't loaded with Dietitian role
                if (dietitianUser == null && patient.UserId.HasValue)
                {
                    dietitianUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == patient.UserId.Value && u.Role == UserRole.Dietitian && !u.IsDeleted);
                }

                var response = new PatientDetailResponse
                {
                    Id = patient.Id,
                    UserId = patient.UserId,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    BirthDate = patient.BirthDate,
                    Gender = (int)patient.Gender,
                    Address = patient.Address,
                    City = patient.City,
                    MedicalHistory = patient.MedicalHistory,
                    Allergies = patient.Allergies,
                    Notes = patient.Notes,
                    IsActive = patient.IsActive,
                    EmailNotificationsEnabled = patient.EmailNotificationsEnabled,
                    CreatedDate = patient.CreatedDate,
                    DietitianName = dietitianUser == null ? null : $"{dietitianUser.FirstName} {dietitianUser.LastName}",
                    DietitianEmail = dietitianUser?.Email,
                    DietitianPhone = dietitianUser?.Phone,
                    DietitianSpecialization = dietitianUser?.Specialization,
                    DietitianLicense = dietitianUser?.License,
                    LatestMeasurement = latestMeasurement == null ? null : new PatientMeasurementResponse
                    {
                        Id = latestMeasurement.Id,
                        PatientId = latestMeasurement.PatientId,
                        MeasurementDate = latestMeasurement.MeasurementDate,
                        Weight = latestMeasurement.Weight,
                        Height = latestMeasurement.Height,
                        Bmi = latestMeasurement.BMI,
                        WaistCircumference = latestMeasurement.WaistCircumference,
                        HipCircumference = latestMeasurement.HipCircumference,
                        BodyFatPercentage = latestMeasurement.BodyFatPercentage,
                        Notes = latestMeasurement.Notes
                    },
                    Measurements = patient.Measurements?.Select(m => new PatientMeasurementResponse
                    {
                        Id = m.Id,
                        PatientId = m.PatientId,
                        MeasurementDate = m.MeasurementDate,
                        Weight = m.Weight,
                        Height = m.Height,
                        Bmi = m.BMI,
                        WaistCircumference = m.WaistCircumference,
                        HipCircumference = m.HipCircumference,
                        BodyFatPercentage = m.BodyFatPercentage,
                        Notes = m.Notes
                    }).OrderByDescending(m => m.MeasurementDate).ToList(),
                    Appointments = patient.Appointments?.Select(a => new PatientAppointmentResponse
                    {
                        Id = a.Id,
                        AppointmentDate = a.AppointmentDate,
                        DurationInMinutes = a.DurationInMinutes,
                        Status = (int)a.Status,
                        Reason = a.Reason,
                        DietitianName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : null
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta profili getirme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpPut("appointments/{appointmentId}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!currentUserId.HasValue)
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz token" });

                if (isDietitian)
                    return Forbid();

                var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz kullanici e-postasi" });

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Patient.Email == currentUserEmail && !a.IsDeleted);

                if (appointment == null)
                    return NotFound(new ApiResponse { Success = false, Message = "Randevu bulunamadi" });

                if (appointment.AppointmentDate <= DateTime.Now)
                    return BadRequest(new ApiResponse { Success = false, Message = "Gecmis randevular iptal edilemez" });

                appointment.Status = AppointmentStatus.Cancelled;
                appointment.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = "Randevu iptal edildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu iptal hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpPut("appointments/{appointmentId}/reschedule")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] RescheduleAppointmentRequest request)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!currentUserId.HasValue)
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz token" });

                if (isDietitian)
                    return Forbid();

                var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz kullanici e-postasi" });

                if (request.NewAppointmentDate <= DateTime.Now)
                    return BadRequest(new ApiResponse { Success = false, Message = "Yeni randevu tarihi gecmiste olamaz" });

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Patient.Email == currentUserEmail && !a.IsDeleted);

                if (appointment == null)
                    return NotFound(new ApiResponse { Success = false, Message = "Randevu bulunamadi" });

                if (appointment.AppointmentDate <= DateTime.Now)
                    return BadRequest(new ApiResponse { Success = false, Message = "Gecmis randevular ertelenemez" });

                appointment.AppointmentDate = request.NewAppointmentDate.Kind == DateTimeKind.Unspecified
                    ? request.NewAppointmentDate
                    : request.NewAppointmentDate.ToLocalTime();
                appointment.Status = AppointmentStatus.Rescheduled;
                appointment.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = "Randevu ertelendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu erteleme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        [HttpPatch("email-notifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmailNotifications([FromBody] UpdateEmailNotificationsRequest request)
        {
            try
            {
                var (currentUserId, isDietitian) = GetCurrentUserContext();
                if (!currentUserId.HasValue)
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz token" });

                if (isDietitian)
                    return Forbid();

                var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                    return Unauthorized(new ApiResponse { Success = false, Message = "Gecersiz kullanici e-postasi" });

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => !p.IsDeleted && p.Email == currentUserEmail);
                if (patient == null)
                    return NotFound(new ApiResponse { Success = false, Message = "Hasta kaydiniz bulunamadi" });

                patient.EmailNotificationsEnabled = request.Enabled;
                patient.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = request.Enabled ? "E-posta bildirimleri acildi" : "E-posta bildirimleri kapatildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta bildirim tercihi guncelleme hatasi");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata olustu" });
            }
        }

        private static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}
