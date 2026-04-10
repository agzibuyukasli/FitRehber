using System;
using System.Collections.Generic;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    /// <summary>
    /// Beslenme planı entity'si
    /// </summary>
    public class MealPlan : BaseEntity
    {
        public int PatientId { get; set; }
        public int UserId { get; set; } // Diyetisyen
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DurationInDays { get; set; }
        public double TargetCalories { get; set; }
        public double TargetProtein { get; set; }
        public double TargetCarbs { get; set; }
        public double TargetFat { get; set; }
        public string DietaryRestrictions { get; set; }
        public MealPlanStatus Status { get; set; } = MealPlanStatus.Active;
        public string Notes { get; set; }

        // İlişkiler
        public virtual Patient Patient { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<MealPlanDay> MealPlanDays { get; set; } = new List<MealPlanDay>();
    }

    public enum MealPlanStatus
    {
        Draft = 0,
        Active = 1,
        Completed = 2,
        Cancelled = 3
    }
}
