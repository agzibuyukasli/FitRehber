namespace DietitianClinic.API.Models.Requests
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class SendResetCodeRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyResetCodeRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
