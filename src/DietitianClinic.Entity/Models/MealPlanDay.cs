using System;
using System.Collections.Generic;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    public class MealPlanDay : BaseEntity
    {
        public int MealPlanId { get; set; }
        public int DayNumber { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }

        public virtual MealPlan MealPlan { get; set; }
        public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();
    }
}
