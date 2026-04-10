using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DietitianClinic.Entity.Models;

namespace DietitianClinic.DataAccess.Configurations
{
    public class MealPlanDayConfiguration : IEntityTypeConfiguration<MealPlanDay>
    {
        public void Configure(EntityTypeBuilder<MealPlanDay> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Notes)
                .HasColumnType("text");

            builder.HasOne(x => x.MealPlan)
                .WithMany(x => x.MealPlanDays)
                .HasForeignKey(x => x.MealPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Meals)
                .WithOne(x => x.MealPlanDay)
                .HasForeignKey(x => x.MealPlanDayId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.MealPlanId);

            builder.ToTable("MealPlanDays");
        }
    }
}
