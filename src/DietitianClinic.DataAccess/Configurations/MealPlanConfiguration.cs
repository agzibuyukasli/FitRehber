using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DietitianClinic.Entity.Models;

namespace DietitianClinic.DataAccess.Configurations
{
    public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
    {
        public void Configure(EntityTypeBuilder<MealPlan> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasColumnType("text");

            builder.Property(x => x.DietaryRestrictions)
                .HasMaxLength(1000);

            builder.Property(x => x.Notes)
                .HasColumnType("text");

            builder.Property(x => x.Status)
                .HasConversion<int>();

            builder.Property(x => x.TargetCalories).HasPrecision(10, 2);
            builder.Property(x => x.TargetProtein).HasPrecision(10, 2);
            builder.Property(x => x.TargetCarbs).HasPrecision(10, 2);
            builder.Property(x => x.TargetFat).HasPrecision(10, 2);

            builder.HasOne(x => x.Patient)
                .WithMany(x => x.MealPlans)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany(x => x.MealPlans)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.MealPlanDays)
                .WithOne(x => x.MealPlan)
                .HasForeignKey(x => x.MealPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PatientId);

            builder.ToTable("MealPlans");
        }
    }
}
