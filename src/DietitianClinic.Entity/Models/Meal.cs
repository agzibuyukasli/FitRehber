using System;
using System.Collections.Generic;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    /// <summary>
    /// Öğün bilgisi (Kahvaltı, Öğle, Akşam vb.)
    /// </summary>
    public class Meal : BaseEntity
    {
        public int MealPlanDayId { get; set; }
        public MealType MealType { get; set; }
        public string Description { get; set; }
        public double TotalCalories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public string Notes { get; set; }

        // İlişkiler
        public virtual MealPlanDay MealPlanDay { get; set; }
        public virtual ICollection<FoodItem> FoodItems { get; set; } = new List<FoodItem>();
    }

    public enum MealType
    {
        Breakfast = 0,
        MorningSnack = 1,
        Lunch = 2,
        AfternoonSnack = 3,
        Dinner = 4,
        EveningSnack = 5
    }
}
