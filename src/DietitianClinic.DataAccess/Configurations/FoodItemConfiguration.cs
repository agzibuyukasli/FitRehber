using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DietitianClinic.Entity.Models;

namespace DietitianClinic.DataAccess.Configurations
{
    public class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
    {
        public void Configure(EntityTypeBuilder<FoodItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.Unit)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Quantity).HasPrecision(10, 2);
            builder.Property(x => x.CaloriesPer100g).HasPrecision(10, 2);
            builder.Property(x => x.ProteinPer100g).HasPrecision(10, 2);
            builder.Property(x => x.CarbsPer100g).HasPrecision(10, 2);
            builder.Property(x => x.FatPer100g).HasPrecision(10, 2);
            builder.Property(x => x.TotalCalories).HasPrecision(10, 2);
            builder.Property(x => x.TotalProtein).HasPrecision(10, 2);
            builder.Property(x => x.TotalCarbs).HasPrecision(10, 2);
            builder.Property(x => x.TotalFat).HasPrecision(10, 2);

            builder.HasOne(x => x.Meal)
                .WithMany(x => x.FoodItems)
                .HasForeignKey(x => x.MealId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.MealId);

            builder.ToTable("FoodItems");
        }
    }
}
