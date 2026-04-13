using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DietitianClinic.Entity.Models;
using DietitianClinic.DataAccess.Configurations;

namespace DietitianClinic.DataAccess.Context
{
    public class DietitianClinicDbContext : DbContext
    {
        public DietitianClinicDbContext(DbContextOptions<DietitianClinicDbContext> options) : base(options)
        {
        }

        #region DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<MealPlanDay> MealPlanDays { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<PatientMeasurement> PatientMeasurements { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<DeletedPatient> DeletedPatients { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new PatientConfiguration());
            modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
            modelBuilder.ApplyConfiguration(new MealPlanConfiguration());
            modelBuilder.ApplyConfiguration(new MealPlanDayConfiguration());
            modelBuilder.ApplyConfiguration(new MealConfiguration());
            modelBuilder.ApplyConfiguration(new FoodItemConfiguration());
            modelBuilder.ApplyConfiguration(new PatientMeasurementConfiguration());

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(Entity.Base.BaseEntity).IsAssignableFrom(entity.ClrType))
                {
                    modelBuilder.Entity(entity.ClrType)
                        .HasQueryFilter(DynamicFilterQueryableExtensions.GetDeletedFilter(entity.ClrType));
                }
            }
        }

        public override int SaveChanges()
        {
            SetAuditingFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditingFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SetAuditingFields()
        {
            var entries = ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                if (entry.Entity is not Entity.Base.BaseEntity baseEntity)
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedDate = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        baseEntity.ModifiedDate = DateTime.UtcNow;
                        break;
                }
            }
        }
    }

    internal static class DynamicFilterQueryableExtensions
    {
        internal static LambdaExpression GetDeletedFilter(Type targetType)
        {
            var parameter = Expression.Parameter(targetType);
            var property = Expression.Property(parameter, "IsDeleted");
            var falseConstant = Expression.Constant(false);
            var expression = Expression.Equal(property, falseConstant);
            return Expression.Lambda(expression, parameter);
        }
    }
}
