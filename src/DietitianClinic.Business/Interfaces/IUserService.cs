using System.Threading.Tasks;

namespace DietitianClinic.Business.Interfaces
{
    /// <summary>
    /// User Service Interface - Kullanıcı işlemleri için
    /// Ileride implement edilecek
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Kullanıcı kaydı oluştur
        /// </summary>
        Task<int> RegisterUserAsync(string firstName, string lastName, string email, string password, string phone, string specialization, string license, string role);

        /// <summary>
        /// Kullanıcı giriş yap
        /// </summary>
        Task<(string token, string refreshToken)> LoginAsync(string email, string password);

        /// <summary>
        /// Refresh token ile yeni token al
        /// </summary>
        Task<string> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Email ile kullanıcı bul
        /// </summary>
        Task<dynamic> GetUserByEmailAsync(string email);

        /// <summary>
        /// ID ile kullanıcı bul
        /// </summary>
        Task<dynamic> GetUserByIdAsync(int userId);

        /// <summary>
        /// Kullanıcı şifresini güncelle
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    }
}
