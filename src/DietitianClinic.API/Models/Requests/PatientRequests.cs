namespace DietitianClinic.API.Models.Requests
{
    /// <summary>
    /// Patient oluşturma isteği
    /// </summary>
    public class CreatePatientRequest
    {
        public int? UserId { get; set; }
        public int? DietitianId { get; set; }
        public string? Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime BirthDate { get; set; }
        public int Gender { get; set; } // 0=Male, 1=Female, 2=Other
        public string Address { get; set; }
        public string City { get; set; }
        public string MedicalHistory { get; set; }
        public string Allergies { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Patient güncelleme isteği
    /// </summary>
    public class UpdatePatientRequest
    {
        public int? UserId { get; set; }
        public int? DietitianId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        // Admin "düzenle" ekranından şifre de gönderebilsin (boş/NULL ise güncellenmez).
        public string? Password { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string MedicalHistory { get; set; }
        public string Allergies { get; set; }
        public string Notes { get; set; }
    }

    public class CreatePatientMeasurementRequest
    {
        public DateTime MeasurementDate { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public double? WaistCircumference { get; set; }
        public double? HipCircumference { get; set; }
        public double? BodyFatPercentage { get; set; }
        public string? Notes { get; set; }
    }

    public class RescheduleAppointmentRequest
    {
        public DateTime NewAppointmentDate { get; set; }
    }

    public class UpdateEmailNotificationsRequest
    {
        public bool Enabled { get; set; }
    }
}
