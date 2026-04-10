using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using DietitianClinic.Business.Services;
using DietitianClinic.API.Models.Requests;
using DietitianClinic.API.Models.Responses;
using DietitianClinic.API.Models.Response;
using DietitianClinic.Business.Exceptions;
using DietitianClinic.Entity.Models;
using DietitianClinic.API.Services;

namespace DietitianClinic.API.Controllers
{
    /// <summary>
    /// User yönetimi endpoint'leri
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly PatientService _patientService;
        private readonly PasswordResetService _resetService;
        private readonly EmailService _emailService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserService userService,
            PatientService patientService,
            PasswordResetService resetService,
            EmailService emailService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _patientService = patientService;
            _resetService = resetService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Yeni kullanÄ±cÄ± kaydÄ± (Diyetisyen/Admin)
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var role = (UserRole)request.Role;
                var userId = await _userService.RegisterUserAsync(
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Password,
                    request.Phone ?? string.Empty,
                    request.Specialization ?? string.Empty,
                    request.License ?? string.Empty,
                    role.ToString()
                );

                if (role == UserRole.Patient)
                {
                    await _patientService.CreatePatientAsync(new Patient
                    {
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        Phone = request.Phone,
                        BirthDate = request.BirthDate ?? DateTime.UtcNow,
                        Gender = request.Gender.HasValue ? (Gender)request.Gender.Value : Gender.Other,
                        Address = request.Address ?? string.Empty,
                        City = request.City ?? string.Empty,
                        MedicalHistory = request.MedicalHistory ?? string.Empty,
                        Allergies = request.Allergies ?? string.Empty,
                        Notes = request.Notes ?? string.Empty
                    }, null);
                }

                var response = new RegisterResponse
                {
                    UserId = userId,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Message = "KayÄ±t baÅŸarÄ±lÄ±"
                };

                return CreatedAtAction(nameof(GetUserById), new { id = userId }, response);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "KayÄ±t validasyon hatasÄ±");
                return BadRequest(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KayÄ±t hatasÄ±");
                return StatusCode(500, new ApiResponse { Success = false, Message = "KayÄ±t baÅŸarÄ±sÄ±z" });
            }
        }

        /// <summary>
        /// KullanÄ±cÄ± giriÅŸi
        /// </summary>
        [HttpPost("login")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var (token, refreshToken) = await _userService.LoginAsync(request.Email, request.Password);
                var user = await _userService.GetUserByEmailAsync(request.Email) as User;

                var response = new LoginResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = (int)user.Role,
                    Token = token,
                    RefreshToken = refreshToken
                };

                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Giriş - kullanıcı bulunamadı");
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Giriş başarısız");
                return Unauthorized(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giriş hatası");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Giriş başarısız" });
            }
        }

        /// <summary>
        /// Şifre değiştirme (oturum açık kullanıcı)
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userIdRaw = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdRaw, out var userId))
                    return Unauthorized(new ApiResponse { Success = false, Message = "Geçersiz oturum." });

                await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
                return Ok(new ApiResponse { Success = true, Message = "Şifre başarıyla değiştirildi." });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre değiştirme hatası");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Şifre değiştirilemedi." });
            }
        }

        /// <summary>
        /// Adım 1 — E-posta adresine 6 haneli doğrulama kodu gönder.
        /// </summary>
        [HttpPost("send-reset-code")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SendResetCode([FromBody] SendResetCodeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new ApiResponse { Success = false, Message = "E-posta zorunludur." });

            var user = await _userService.GetUserByEmailAsync(request.Email.Trim()) as User;
            if (user == null)
            {
                // Güvenli: e-posta bulunamasa bile aynı mesajı ver (enum saldırısı önleme)
                return Ok(new ApiResponse { Success = true, Message = "Kod gönderildi." });
            }

            var code = _resetService.GenerateCode(request.Email.Trim());
            try
            {
                await _emailService.SendPasswordResetCodeAsync(user.Email, user.FirstName, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi: {Email}", request.Email);
                return StatusCode(500, new ApiResponse { Success = false, Message = "E-posta gönderilemedi. SMTP ayarlarını kontrol edin." });
            }

            return Ok(new ApiResponse { Success = true, Message = "Kod gönderildi." });
        }

        /// <summary>
        /// Adım 2 — Kodu doğrula; başarılıysa 5 dakika geçerli tek kullanımlık token döner.
        /// </summary>
        [HttpPost("verify-reset-code")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult VerifyResetCode([FromBody] VerifyResetCodeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new ApiResponse { Success = false, Message = "E-posta ve kod zorunludur." });

            if (!_resetService.VerifyCode(request.Email.Trim(), request.Code.Trim(), out var token))
                return BadRequest(new ApiResponse { Success = false, Message = "Kod hatalı veya süresi dolmuş." });

            return Ok(new { success = true, token });
        }

        /// <summary>
        /// Adım 3 — Token + yeni şifre ile şifreyi sıfırla.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Token)
                || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "E-posta, token ve yeni şifre zorunludur." });
            }

            if (!_resetService.ConsumeToken(request.Email.Trim(), request.Token))
                return BadRequest(new ApiResponse { Success = false, Message = "Geçersiz veya süresi dolmuş oturum. Lütfen tekrar kod isteyin." });

            try
            {
                await _userService.ResetPasswordByEmailAsync(request.Email.Trim(), request.NewPassword);
                return Ok(new ApiResponse { Success = true, Message = "Şifre güncellendi." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama hatası");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Şifre güncellenemedi." });
            }
        }

        /// <summary>
        /// Tüm kullanıcıları getir (role filtrelemesi ile)
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers([FromQuery] int? role = null)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(role);
                
                var response = users.Select(u => new UserResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Specialization = u.Specialization,
                    License = u.License,
                    Role = (int)u.Role,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KullanÄ±cÄ±larÄ± getirme hatasÄ±");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata oluÅŸtu" });
            }
        }

        /// <summary>
        /// ID'ye gÃ¶re kullanÄ±cÄ± getir
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id) as User;
                if (user == null)
                    return NotFound(new ApiResponse { Success = false, Message = "KullanÄ±cÄ± bulunamadÄ±" });

                var response = new UserResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Specialization = user.Specialization,
                    License = user.License,
                    Role = (int)user.Role,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KullanÄ±cÄ± getirme hatasÄ±");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata oluÅŸtu" });
            }
        }

        /// <summary>
        /// KullanÄ±cÄ± gÃ¼ncelle
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _userService.UpdateUserAsync(
                    id,
                    request.FirstName,
                    request.LastName,
                    request.Phone,
                    request.Specialization,
                    request.License);

                return Ok(new ApiResponse { Success = true, Message = "KullanÄ±cÄ± gÃ¼ncellendi" });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "KullanÄ±cÄ± bulunamadÄ±");
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KullanÄ±cÄ± gÃ¼ncelleme hatasÄ±");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata oluÅŸtu" });
            }
        }

        /// <summary>
        /// KullanÄ±cÄ± sil
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "KullanÄ±cÄ± bulunamadÄ±");
                return NotFound(new ApiResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KullanÄ±cÄ± silme hatasÄ±");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata oluÅŸtu" });
            }
        }

        /// <summary>
        /// Mevcut kullanÄ±cÄ±nÄ±n profili
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // JWT token'dan user ID'yi al
                var userIdClaim = User.FindFirst("userId")
                    ?? User.FindFirst(JwtRegisteredClaimNames.Sub)
                    ?? User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized(new ApiResponse { Success = false, Message = "GeÃ§ersiz token" });

                var user = await _userService.GetUserByIdAsync(userId) as User;
                if (user == null)
                    return NotFound(new ApiResponse { Success = false, Message = "KullanÄ±cÄ± bulunamadÄ±" });

                var response = new UserResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Specialization = user.Specialization,
                    License = user.License,
                    Role = (int)user.Role,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil getirme hatasÄ±");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Hata oluÅŸtu" });
            }
        }
    }
}

