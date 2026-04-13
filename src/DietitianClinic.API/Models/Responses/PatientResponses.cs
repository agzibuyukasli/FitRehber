namespace DietitianClinic.API.Models.Responses
{
    public class PatientResponse
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? DietitianId { get; set; }
        public int? PatientUserId { get; set; }
        public string? DietitianName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime BirthDate { get; set; }
        public int Gender { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string MedicalHistory { get; set; }
        public string Allergies { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public bool EmailNotificationsEnabled { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PatientListItemResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int? UserId { get; set; }
        public int? DietitianId { get; set; }
        public int? PatientUserId { get; set; }
        public string? DietitianName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public int Age { get; set; }
        public double? LatestWeight { get; set; }
        public double? LatestHeight { get; set; }
        public double? LatestBmi { get; set; }
        public DateTime? LastMeasurementDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class PatientMeasurementResponse
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime MeasurementDate { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public double? Bmi { get; set; }
        public double? WaistCircumference { get; set; }
        public double? HipCircumference { get; set; }
        public double? BodyFatPercentage { get; set; }
        public string? Notes { get; set; }
    }

    public class PatientAppointmentResponse
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; }
        public int Status { get; set; }
        public string Reason { get; set; }
        public string? DietitianName { get; set; }
    }

    public class PatientDetailResponse : PatientResponse
    {
        public PatientMeasurementResponse? LatestMeasurement { get; set; }
        public IEnumerable<PatientMeasurementResponse>? Measurements { get; set; }
        public IEnumerable<PatientAppointmentResponse>? Appointments { get; set; }
        public string? DietitianEmail { get; set; }
        public string? DietitianPhone { get; set; }
        public string? DietitianSpecialization { get; set; }
        public string? DietitianLicense { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
