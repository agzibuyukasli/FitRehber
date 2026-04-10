using DietitianClinic.Entity.Models;

namespace DietitianClinic.DataAccess.Repositories
{
    /// <summary>
    /// Unit of Work Pattern - İlişkili repository'leri yönetmek için
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> UserRepository { get; }
        IRepository<Patient> PatientRepository { get; }
        IRepository<Appointment> AppointmentRepository { get; }
        IRepository<MealPlan> MealPlanRepository { get; }
        IRepository<MealPlanDay> MealPlanDayRepository { get; }
        IRepository<Meal> MealRepository { get; }
        IRepository<FoodItem> FoodItemRepository { get; }
        IRepository<PatientMeasurement> PatientMeasurementRepository { get; }

        Task<int> SaveAsync();
        Task<bool> BeginTransactionAsync();
        Task<bool> CommitAsync();
        Task<bool> RollbackAsync();
    }
}
