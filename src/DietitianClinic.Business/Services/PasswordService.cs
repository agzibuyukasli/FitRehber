using System.Security.Cryptography;
using System.Text;
using DietitianClinic.Business.Interfaces;

namespace DietitianClinic.Business.Services
{
    /// <summary>
    /// Şifre hashing ve doğrulama - BCrypt (work factor 12).
    /// Eski SHA-256 hash'leriyle geri uyumludur: ilk girişte otomatik yükseltir.
    /// </summary>
    public class PasswordService : IPasswordService
    {
        private const int WorkFactor = 12; // BCrypt maliyet faktörü (2^12 iterasyon)
        private const string LegacySalt = "DietitianClinicSalt2024"; // eski SHA-256 tuzu

        /// <summary>
        /// Yeni BCrypt hash üretir.
        /// </summary>
        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        /// <summary>
        /// BCrypt veya eski SHA-256 hash'ini doğrular.
        /// </summary>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword)) return false;

            // BCrypt hash'leri "$2" ile başlar
            if (hashedPassword.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);

            // Geriye dönük uyumluluk: eski SHA-256
            return LegacyHash(password) == hashedPassword;
        }

        /// <summary>
        /// Verilen hash'in BCrypt ile yeniden hashlenmeye ihtiyacı var mı?
        /// (Eski SHA-256 hash'leri için true döner)
        /// </summary>
        public bool NeedsRehash(string hashedPassword)
            => !hashedPassword.StartsWith("$2");

        /// <summary>
        /// Şifre güç kuralları:
        /// min 8 karakter, büyük/küçük harf, rakam, özel karakter.
        /// </summary>
        public Task<(bool isValid, List<string> errors)> ValidatePasswordStrengthAsync(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Şifre boş olamaz.");
                return Task.FromResult((false, errors));
            }

            if (password.Length < 8)
                errors.Add("Şifre en az 8 karakter olmalıdır.");

            if (!password.Any(char.IsUpper))
                errors.Add("Şifre en az bir büyük harf içermelidir.");

            if (!password.Any(char.IsLower))
                errors.Add("Şifre en az bir küçük harf içermelidir.");

            if (!password.Any(char.IsDigit))
                errors.Add("Şifre en az bir rakam içermelidir.");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Şifre en az bir özel karakter içermelidir (!@#$%^&* vb.).");

            return Task.FromResult((!errors.Any(), errors));
        }

        // ── Özel: eski SHA-256 hesabı ──────────────────────────────────────────
        private static string LegacyHash(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + LegacySalt);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}
