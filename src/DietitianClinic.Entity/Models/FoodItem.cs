using System;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    public class FoodItem : BaseEntity
    {
        public int MealId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        public double CaloriesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
        public double CarbsPer100g { get; set; }
        public double FatPer100g { get; set; }
        public double TotalCalories { get; set; }
        public double TotalProtein { get; set; }
        public double TotalCarbs { get; set; }
        public double TotalFat { get; set; }

        public virtual Meal Meal { get; set; }
    }
}
