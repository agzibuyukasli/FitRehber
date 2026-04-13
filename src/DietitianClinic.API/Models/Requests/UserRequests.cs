namespace DietitianClinic.API.Models.Requests
{
    public class CreateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Phone { get; set; }
        public string? Specialization { get; set; }
        public string? License { get; set; }
        public int Role { get; set; }

        public DateTime? BirthDate { get; set; }
        public int? Gender { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Allergies { get; set; }
        public string? Notes { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Phone { get; set; }
        public string? Specialization { get; set; }
        public string? License { get; set; }
    }
}
