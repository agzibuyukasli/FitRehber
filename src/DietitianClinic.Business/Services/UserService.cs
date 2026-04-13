using DietitianClinic.Business.Interfaces;
using DietitianClinic.Business.Exceptions;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.DataAccess.Repositories;
using DietitianClinic.Entity.Models;
using Microsoft.Extensions.Logging;

namespace DietitianClinic.Business.Services
{
    public class UserService : IUserService
    {
        private readonly DietitianClinic.DataAccess.Repositories.IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private const string GenericLoginError = "E-posta veya şifre hatalı.";

        public UserService(
            DietitianClinic.DataAccess.Repositories.IUnitOfWork unitOfWork,
            ILogger<UserService> logger,
            ITokenService tokenService,
            IPasswordService passwordService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _tokenService = tokenService;
            _passwordService = passwordService;
        }

        public async Task<int> RegisterUserAsync(string firstName, string lastName, string email,
            string password, string phone, string specialization, string license, string role)
        {
            try
            {
                var existingUser = await _unitOfWork.UserRepository
                    .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
                if (existingUser != null)
                    throw new ValidationException($"Email '{email}' zaten kullanılıyor.");

                if (string.IsNullOrWhiteSpace(password))
                    throw new ValidationException("Şifre boş olamaz.");

                var user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = phone ?? string.Empty,
                    Specialization = specialization ?? string.Empty,
                    License = license ?? string.Empty,
                    PasswordHash = _passwordService.HashPassword(password),
                    Role = (UserRole)Enum.Parse(typeof(UserRole), role),
                    IsActive = true
                };

                await _unitOfWork.UserRepository.AddAsync(user);
                _logger.LogInformation("Kullanıcı kaydı başarılı: {Email}", email);
                return user.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kaydı başarısız");
                throw;
            }
        }

        public async Task<(string token, string refreshToken)> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _unitOfWork.UserRepository
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    _passwordService.VerifyPassword(password, "$2a$12$dummyhashtopreventtimingattack000000000000000000000000");
                    _logger.LogWarning("Başarısız giriş (kullanıcı yok): {Email}", email);
                    throw new NotFoundException("Bu e-posta adresiyle kayıtlı bir kullanıcı bulunamadı.");
                }

                if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
                {
                    var remaining = (int)(user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes + 1;
                    _logger.LogWarning("Kilitli hesaba giriş denemesi: {Email}", email);
                    throw new UnauthorizedException($"Hesabınız geçici olarak kilitlendi. {remaining} dakika sonra tekrar deneyin.");
                }

                if (user.IsDeleted || !user.IsActive)
                    throw new UnauthorizedException(GenericLoginError);

                if (!_passwordService.VerifyPassword(password, user.PasswordHash))
                {
                    user.AccessFailedCount++;
                    if (user.AccessFailedCount >= MaxFailedAttempts)
                    {
                        user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                        user.AccessFailedCount = 0;
                        _logger.LogWarning("Hesap kilitlendi ({MaxAttempts} başarısız deneme): {Email}", MaxFailedAttempts, email);
                    }
                    await _unitOfWork.UserRepository.UpdateAsync(user);
                    _logger.LogWarning("Başarısız giriş denemesi ({Count}): {Email}", user.AccessFailedCount, email);
                    throw new UnauthorizedException(GenericLoginError);
                }

                user.AccessFailedCount = 0;
                user.LockoutEndUtc = null;

                if (_passwordService.NeedsRehash(user.PasswordHash))
                {
                    user.PasswordHash = _passwordService.HashPassword(password);
                    _logger.LogInformation("Şifre hash'i BCrypt'e yükseltildi: {Email}", email);
                }

                await _unitOfWork.UserRepository.UpdateAsync(user);

                var token = await _tokenService.GenerateTokenAsync(user.Id, user.Email, user.Role.ToString());
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

                _logger.LogInformation("Kullanıcı girişi başarılı: {Email}", email);
                return (token, refreshToken);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı girişi beklenmeyen hata");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                    throw new NotFoundException($"Kullanıcı (ID: {userId}) bulunamadı.");

                if (!_passwordService.VerifyPassword(oldPassword, user.PasswordHash))
                    throw new UnauthorizedException("Mevcut şifre hatalı.");

                if (oldPassword == newPassword)
                    throw new ValidationException("Yeni şifre mevcut şifre ile aynı olamaz.");

                var (isValid, errors) = await _passwordService.ValidatePasswordStrengthAsync(newPassword);
                if (!isValid)
                    throw new ValidationException(string.Join(" ", errors));

                user.PasswordHash = _passwordService.HashPassword(newPassword);
                user.AccessFailedCount = 0;
                user.LockoutEndUtc = null;

                await _unitOfWork.UserRepository.UpdateAsync(user);
                _logger.LogInformation("Şifre değiştirildi: UserId={UserId}", userId);
                return true;
            }
            catch (Exception ex) when (ex is not UnauthorizedException and not ValidationException and not NotFoundException)
            {
                _logger.LogError(ex, "Şifre değiştirme hatası: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ResetPasswordByEmailAsync(string email, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    throw new ValidationException("E-posta boş olamaz.");

                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ValidationException("Şifre boş olamaz.");

                var (isValid, errors) = await _passwordService.ValidatePasswordStrengthAsync(newPassword);
                if (!isValid)
                    throw new ValidationException(string.Join(" ", errors));

                var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("Şifre sıfırlama - kullanıcı bulunamadı: {Email}", email);
                    throw new NotFoundException("Kullanıcı bulunamadı.");
                }

                user.PasswordHash = _passwordService.HashPassword(newPassword);
                user.AccessFailedCount = 0;
                user.LockoutEndUtc = null;
                await _unitOfWork.UserRepository.UpdateAsync(user);
                _logger.LogInformation("Şifre sıfırlandı: {Email}", email);
                return true;
            }
            catch (Exception ex) when (ex is not ValidationException and not NotFoundException)
            {
                _logger.LogError(ex, "Şifre sıfırlama hatası: {Email}", email);
                throw;
            }
        }

        public async Task<string> RefreshTokenAsync(string refreshToken)
            => throw new NotImplementedException();

        public async Task<dynamic> GetUserByEmailAsync(string email)
            => await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        public async Task<dynamic> GetUserByIdAsync(int userId)
            => await _unitOfWork.UserRepository.GetByIdAsync(userId);

        public async Task<List<User>> GetAllUsersAsync(int? roleFilter = null)
        {
            try
            {
                var users = await _unitOfWork.UserRepository.GetAllAsync();
                if (roleFilter.HasValue)
                    users = users.Where(u => (int)u.Role == roleFilter.Value).ToList();
                return users.OrderBy(u => u.FirstName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcıları getirme hatası");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, string firstName, string lastName,
            string phone, string specialization, string license)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                    throw new NotFoundException($"Kullanıcı (ID: {userId}) bulunamadı.");

                user.FirstName = firstName;
                user.LastName = lastName;
                user.Phone = phone;
                user.Specialization = specialization;
                user.License = license;

                await _unitOfWork.UserRepository.UpdateAsync(user);
                _logger.LogInformation("Kullanıcı güncellendi: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncelleme başarısız (ID: {UserId})", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                    throw new NotFoundException($"Kullanıcı (ID: {userId}) bulunamadı.");

                await _unitOfWork.UserRepository.DeleteAsync(user);
                _logger.LogInformation("Kullanıcı silindi: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silme başarısız (ID: {UserId})", userId);
                throw;
            }
        }
    }
}
