using System;
using System.Collections.Generic;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    public class Patient : BaseEntity
    {
        public int? UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string MedicalHistory { get; set; }
        public string Allergies { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public bool EmailNotificationsEnabled { get; set; } = true;

        public virtual User? User { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<PatientMeasurement> Measurements { get; set; } = new List<PatientMeasurement>();
        public virtual ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    }

    public enum Gender
    {
        Male = 0,
        Female = 1,
        Other = 2
    }
}
