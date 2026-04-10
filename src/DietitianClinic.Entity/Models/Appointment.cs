using System;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    /// <summary>
    /// Randevu entity'si
    /// </summary>
    public class Appointment : BaseEntity
    {
        public int PatientId { get; set; }
        public int UserId { get; set; } // Diyetisyen
        public DateTime AppointmentDate { get; set; }
        public int DurationInMinutes { get; set; } = 30;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public double? Rating { get; set; }
        public string? Feedback { get; set; }

        // İlişkiler
        public virtual Patient Patient { get; set; }
        public virtual User User { get; set; }
    }

    public enum AppointmentStatus
    {
        Scheduled = 0,
        Completed = 1,
        Cancelled = 2,
        NoShow = 3,
        Rescheduled = 4,
        Requested = 5
    }
}
