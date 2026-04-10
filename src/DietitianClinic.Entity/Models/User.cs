using System;
using System.Collections.Generic;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    /// <summary>
    /// Kullanıcı entity'si - Diyetisyen ve Admin için
    /// </summary>
    public class User : BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; } = UserRole.Dietitian;
        public string Specialization { get; set; } // Uzmanlık alanı
        public string License { get; set; } // Lisans numarası
        public bool IsActive { get; set; } = true;

        // ── Hesap kilitleme (brute-force koruması) ──────────────────────────────
        /// <summary>Ardışık başarısız giriş sayısı</summary>
        public int AccessFailedCount { get; set; } = 0;
        /// <summary>Hesabın kilide açılacağı UTC zamanı (null = kilitli değil)</summary>
        public DateTime? LockoutEndUtc { get; set; }

        // İlişkiler
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
