using System.Threading.Tasks;

namespace DietitianClinic.Business.Interfaces
{
    public interface IUserService
    {
        Task<int> RegisterUserAsync(string firstName, string lastName, string email, string password, string phone, string specialization, string license, string role);

        Task<(string token, string refreshToken)> LoginAsync(string email, string password);

        Task<string> RefreshTokenAsync(string refreshToken);

        Task<dynamic> GetUserByEmailAsync(string email);

        Task<dynamic> GetUserByIdAsync(int userId);

        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    }
}
