using System;
using System.Collections.Generic;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    /// <summary>
    /// Beslenme planının günlük bilgileri
    /// </summary>
    public class MealPlanDay : BaseEntity
    {
        public int MealPlanId { get; set; }
        public int DayNumber { get; set; } // 1-7 veya daha fazla
        public DateTime Date { get; set; }
        public string Notes { get; set; }

        // İlişkiler
        public virtual MealPlan MealPlan { get; set; }
        public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();
    }
}
