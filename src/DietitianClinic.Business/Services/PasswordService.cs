using System.Security.Cryptography;
using System.Text;
using DietitianClinic.Business.Interfaces;

namespace DietitianClinic.Business.Services
{
    public class PasswordService : IPasswordService
    {
        private const int WorkFactor = 12;
        private const string LegacySalt = "DietitianClinicSalt2024";

        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword)) return false;

            if (hashedPassword.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);

            return LegacyHash(password) == hashedPassword;
        }

        public bool NeedsRehash(string hashedPassword)
            => !hashedPassword.StartsWith("$2");

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

        private static string LegacyHash(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + LegacySalt);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}
