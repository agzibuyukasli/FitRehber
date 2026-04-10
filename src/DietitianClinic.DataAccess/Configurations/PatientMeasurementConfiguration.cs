using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DietitianClinic.Entity.Models;

namespace DietitianClinic.DataAccess.Configurations
{
    public class PatientMeasurementConfiguration : IEntityTypeConfiguration<PatientMeasurement>
    {
        public void Configure(EntityTypeBuilder<PatientMeasurement> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Weight).HasPrecision(10, 2);
            builder.Property(x => x.Height).HasPrecision(10, 2);
            builder.Property(x => x.BMI).HasPrecision(5, 2);
            builder.Property(x => x.WaistCircumference).HasPrecision(10, 2);
            builder.Property(x => x.HipCircumference).HasPrecision(10, 2);
            builder.Property(x => x.ChestCircumference).HasPrecision(10, 2);
            builder.Property(x => x.ArmCircumference).HasPrecision(10, 2);
            builder.Property(x => x.ThighCircumference).HasPrecision(10, 2);
            builder.Property(x => x.BodyFatPercentage).HasPrecision(5, 2);

            builder.Property(x => x.Notes)
                .HasColumnType("text");

            builder.HasOne(x => x.Patient)
                .WithMany(x => x.Measurements)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PatientId);
            builder.HasIndex(x => x.MeasurementDate);

            builder.ToTable("PatientMeasurements");
        }
    }
}
