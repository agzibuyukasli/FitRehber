namespace DietitianClinic.API.Models.Requests
{
    public class CreateAppointmentRequest
    {
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; }
        public int Status { get; set; }
        public string Reason { get; set; }
    }

    public class UpdateAppointmentRequest
    {
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; }
        public int Status { get; set; }
        public string Reason { get; set; }
    }

    public class RequestAppointmentRequest
    {
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; } = 30;
        public string Reason { get; set; }
    }
}
