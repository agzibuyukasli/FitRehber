using System;
using System.Collections.Generic;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    public class User : BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; } = UserRole.Dietitian;
        public string Specialization { get; set; }
        public string License { get; set; }
        public bool IsActive { get; set; } = true;

        public int AccessFailedCount { get; set; } = 0;
        public DateTime? LockoutEndUtc { get; set; }

        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    }

    public enum UserRole
    {
        Admin = 0,
        Dietitian = 1,
        SuperAdmin = 2,
        Patient = 3
    }
}
