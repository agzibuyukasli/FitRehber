using DietitianClinic.Business.Interfaces;
using DietitianClinic.Business.Exceptions;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.DataAccess.Repositories;
using DietitianClinic.Entity.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DietitianClinic.Business.Services
{
    public class PatientService
    {
        private readonly DietitianClinic.DataAccess.Repositories.IUnitOfWork _unitOfWork;
        private readonly DietitianClinicDbContext _context;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            DietitianClinic.DataAccess.Repositories.IUnitOfWork unitOfWork,
            DietitianClinicDbContext context,
            ILogger<PatientService> logger)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _logger = logger;
        }

        public async Task<IList<Patient>> GetAllPatientsAsync(int? userId = null, bool dietitianOnly = false)
        {
            try
            {
                var query = _context.Patients
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.User)
                    .Include(p => p.Measurements.OrderByDescending(m => m.MeasurementDate).Take(1))
                    .AsQueryable();

                if (dietitianOnly && userId.HasValue)
                {
                    query = query.Where(p => p.UserId == userId.Value);
                }

                return await query
                    .OrderBy(p => p.FirstName)
                    .ThenBy(p => p.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hastaları getirme başarısız");
                throw;
            }
        }

        public async Task<Patient> GetPatientByIdAsync(int patientId, int? userId = null, bool dietitianOnly = false)
        {
            try
            {
                var query = _context.Patients
                    .Where(p => !p.IsDeleted && p.Id == patientId)
                    .Include(p => p.User)
                    .Include(p => p.Measurements.OrderByDescending(m => m.MeasurementDate))
                    .AsQueryable();

                if (dietitianOnly && userId.HasValue)
                {
                    query = query.Where(p => p.UserId == userId.Value);
                }

                var patient = await query.FirstOrDefaultAsync();
                if (patient == null)
                {
                    throw new NotFoundException($"Hasta (ID: {patientId}) bulunamadı.");
                }
                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Hasta getirme başarısız (ID: {patientId})");
                throw;
            }
        }

        public async Task<int> CreatePatientAsync(Patient patient, int? userId = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(patient.Email))
                {
                    var existingPatient = await _unitOfWork.PatientRepository
                        .FirstOrDefaultAsync(p => p.Email == patient.Email);
                    if (existingPatient != null)
                    {
                        throw new ValidationException($"Email '{patient.Email}' zaten kullanılıyor.");
                    }
                }

                patient.IsActive = true;
                if (userId.HasValue)
                {
                    patient.UserId = userId;
                }
                var result = await _unitOfWork.PatientRepository.AddAsync(patient);
                _logger.LogInformation($"Hasta oluşturuldu: {patient.FirstName} {patient.LastName}");
                return patient.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hasta oluşturma başarısız");
                throw;
            }
        }

        public async Task<bool> UpdatePatientAsync(int patientId, Patient patientDetails, int? userId = null, bool dietitianOnly = false)
        {
            try
            {
                var patient = await GetPatientByIdAsync(patientId, userId, dietitianOnly);

                patient.FirstName = patientDetails.FirstName;
                patient.LastName = patientDetails.LastName;
                patient.Email = patientDetails.Email;
                patient.Phone = patientDetails.Phone;
                patient.Address = patientDetails.Address;
                patient.City = patientDetails.City;
                patient.MedicalHistory = patientDetails.MedicalHistory;
                patient.Allergies = patientDetails.Allergies;
                patient.Notes = patientDetails.Notes;
                patient.UserId = patientDetails.UserId;

                await _unitOfWork.PatientRepository.UpdateAsync(patient);
                _logger.LogInformation($"Hasta güncellendi: {patient.FirstName} {patient.LastName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Hasta güncelleme başarısız (ID: {patientId})");
                throw;
            }
        }

        public async Task<bool> DeletePatientAsync(int patientId, int? userId = null, bool dietitianOnly = false)
        {
            try
            {
                var patient = await GetPatientByIdAsync(patientId, userId, dietitianOnly);

                var archived = new DeletedPatient
                {
                    OriginalPatientId = patient.Id,
                    OwnerUserId = patient.UserId,
                    FirstName = patient.FirstName ?? string.Empty,
                    LastName = patient.LastName ?? string.Empty,
                    Email = patient.Email ?? string.Empty,
                    Phone = patient.Phone ?? string.Empty,
                    BirthDate = patient.BirthDate,
                    Gender = patient.Gender,
                    City = patient.City ?? string.Empty,
                    Address = patient.Address ?? string.Empty,
                    MedicalHistory = patient.MedicalHistory ?? string.Empty,
                    Allergies = patient.Allergies ?? string.Empty,
                    Notes = patient.Notes ?? string.Empty,
                    DeletedAt = DateTime.UtcNow,
                    CreatedDate = patient.CreatedDate
                };
                await _context.DeletedPatients.AddAsync(archived);
                await _context.SaveChangesAsync();

                await _unitOfWork.PatientRepository.DeleteAsync(patient);

                if (!string.IsNullOrWhiteSpace(patient.Email))
                {
                    var patientUser = await _context.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Email == patient.Email && u.Role == UserRole.Patient && !u.IsDeleted);
                    if (patientUser != null)
                    {
                        patientUser.IsDeleted = true;
                        patientUser.IsActive = false;
                        patientUser.DeletedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation($"Hasta silindi ve arşivlendi (ID: {patientId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Hasta silme başarısız (ID: {patientId})");
                throw;
            }
        }

        public async Task<IList<PatientMeasurement>> GetPatientMeasurementsAsync(int patientId, int? userId = null, bool dietitianOnly = false)
        {
            await GetPatientByIdAsync(patientId, userId, dietitianOnly);

            return await _context.PatientMeasurements
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.PatientId == patientId)
                .OrderByDescending(m => m.MeasurementDate)
                .ToListAsync();
        }

        public async Task<int> AddPatientMeasurementAsync(int patientId, PatientMeasurement measurement, int? userId = null, bool dietitianOnly = false)
        {
            await GetPatientByIdAsync(patientId, userId, dietitianOnly);

            measurement.PatientId = patientId;
            measurement.CalculateBMI();
            await _context.PatientMeasurements.AddAsync(measurement);
            await _context.SaveChangesAsync();
            return measurement.Id;
        }

        public async Task<(IList<Patient> patients, int totalCount)> GetPatientsPaginatedAsync(int pageNumber, int pageSize)
        {
            try
            {
                var patients = await _unitOfWork.PatientRepository.GetPagedAsync(pageNumber, pageSize);
                var totalCount = await _unitOfWork.PatientRepository.CountAsync();
                return (patients, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sayfalamalı hasta listesi başarısız");
                throw;
            }
        }

        public async Task<Patient?> GetPatientByUserIdAsync(int userId)
        {
            try
            {
                return await _context.Patients
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && p.UserId == userId)
                    .Include(p => p.User)
                    .Include(p => p.Measurements.OrderByDescending(m => m.MeasurementDate))
                    .Include(p => p.Appointments.OrderByDescending(a => a.AppointmentDate))
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"User ID'ye göre hasta getirme başarısız (UserID: {userId})");
                throw;
            }
        }

        public async Task<Patient?> GetPatientByEmailAsync(string email)
        {
            try
            {
                return await _context.Patients
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && p.Email == email)
                    .Include(p => p.User)
                    .Include(p => p.Measurements.OrderByDescending(m => m.MeasurementDate))
                    .Include(p => p.Appointments.OrderByDescending(a => a.AppointmentDate))
                    .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Email'e gore hasta getirme basarisiz (Email: {email})");
                throw;
            }
        }
    }
}
