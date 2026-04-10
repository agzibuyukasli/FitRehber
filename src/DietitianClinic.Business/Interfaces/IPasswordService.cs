using System.Threading.Tasks;

namespace DietitianClinic.Business.Interfaces
{
    /// <summary>
    /// Password Service Interface - Şifre yönetimi için
    /// Ileride implement edilecek
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Şifreyi hash'le
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Şifreyi doğrula
        /// </summary>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// Güçlü şifre kontrolü yap
        /// </summary>
        Task<(bool isValid, List<string> errors)> ValidatePasswordStrengthAsync(string password);

        /// <summary>
        /// Hash'in yeniden üretilmesi gerekip gerekmediğini döner (eski algoritma)
        /// </summary>
        bool NeedsRehash(string hashedPassword);
    }
}
