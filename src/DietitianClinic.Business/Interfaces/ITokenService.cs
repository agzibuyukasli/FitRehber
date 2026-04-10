using System.Threading.Tasks;

namespace DietitianClinic.Business.Interfaces
{
    /// <summary>
    /// JWT Token Service Interface - Authentication/Authorization için
    /// Ileride implement edilecek
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// JWT token oluştur
        /// </summary>
        Task<string> GenerateTokenAsync(int userId, string email, string role);

        /// <summary>
        /// Token doğrula
        /// </summary>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Refresh token oluştur
        /// </summary>
        Task<string> GenerateRefreshTokenAsync();

        /// <summary>
        /// Token claims'i al
        /// </summary>
        Task<Dictionary<string, object>> GetTokenClaimsAsync(string token);
    }
}
