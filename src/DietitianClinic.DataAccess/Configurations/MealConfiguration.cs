using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DietitianClinic.Entity.Models;

namespace DietitianClinic.DataAccess.Configurations
{
    public class MealConfiguration : IEntityTypeConfiguration<Meal>
    {
        public void Configure(EntityTypeBuilder<Meal> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.Notes)
                .HasColumnType("text");

            builder.Property(x => x.MealType)
                .HasConversion<int>();

            builder.Property(x => x.TotalCalories).HasPrecision(10, 2);
            builder.Property(x => x.Protein).HasPrecision(10, 2);
            builder.Property(x => x.Carbs).HasPrecision(10, 2);
            builder.Property(x => x.Fat).HasPrecision(10, 2);

            builder.HasOne(x => x.MealPlanDay)
                .WithMany(x => x.Meals)
                .HasForeignKey(x => x.MealPlanDayId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.FoodItems)
                .WithOne(x => x.Meal)
                .HasForeignKey(x => x.MealId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.MealPlanDayId);

            builder.ToTable("Meals");
        }
    }
}
