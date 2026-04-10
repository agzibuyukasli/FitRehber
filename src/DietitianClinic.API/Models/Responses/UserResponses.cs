namespace DietitianClinic.API.Models.Responses
{
    /// <summary>
    /// User response modeli
    /// </summary>
    public class UserResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Specialization { get; set; }
        public string License { get; set; }
        public int Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Login yanıtı
    /// </summary>
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Role { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    /// <summary>
    /// Register yanıtı
    /// </summary>
    public class RegisterResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Message { get; set; }
    }
}
