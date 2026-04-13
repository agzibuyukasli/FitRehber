using System.Threading.Tasks;

namespace DietitianClinic.Business.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(string password);

        bool VerifyPassword(string password, string hash);

        Task<(bool isValid, List<string> errors)> ValidatePasswordStrengthAsync(string password);

        bool NeedsRehash(string hashedPassword);
    }
}
