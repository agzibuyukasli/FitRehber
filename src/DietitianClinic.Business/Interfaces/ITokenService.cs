using System.Threading.Tasks;

namespace DietitianClinic.Business.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(int userId, string email, string role);

        Task<bool> ValidateTokenAsync(string token);

        Task<string> GenerateRefreshTokenAsync();

        Task<Dictionary<string, object>> GetTokenClaimsAsync(string token);
    }
}
