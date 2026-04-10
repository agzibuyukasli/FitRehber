namespace DietitianClinic.API.Models.Requests
{
    /// <summary>
    /// Randevu oluşturma isteği
    /// </summary>
    public class CreateAppointmentRequest
    {
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; }
        public int Status { get; set; } // 0=Planlanmış, 1=Tamamlanmış, 2=İptal
        public string Reason { get; set; }
    }

    /// <summary>
    /// Randevu güncelleme isteği
    /// </summary>
    public class UpdateAppointmentRequest
    {
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; }
        public int Status { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Danışan randevu talebi
    /// </summary>
    public class RequestAppointmentRequest
    {
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; } = 30;
        public string Reason { get; set; }
    }
}
