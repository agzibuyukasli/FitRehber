using DietitianClinic.DataAccess.Context;
using DietitianClinic.Entity.Models;

namespace DietitianClinic.DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DietitianClinicDbContext _context;

        private IRepository<User> _userRepository;
        private IRepository<Patient> _patientRepository;
        private IRepository<Appointment> _appointmentRepository;
        private IRepository<MealPlan> _mealPlanRepository;
        private IRepository<MealPlanDay> _mealPlanDayRepository;
        private IRepository<Meal> _mealRepository;
        private IRepository<FoodItem> _foodItemRepository;
        private IRepository<PatientMeasurement> _patientMeasurementRepository;

        public UnitOfWork(DietitianClinicDbContext context)
        {
            _context = context;
        }

        public IRepository<User> UserRepository => _userRepository ??= new Repository<User>(_context);
        public IRepository<Patient> PatientRepository => _patientRepository ??= new Repository<Patient>(_context);
        public IRepository<Appointment> AppointmentRepository => _appointmentRepository ??= new Repository<Appointment>(_context);
        public IRepository<MealPlan> MealPlanRepository => _mealPlanRepository ??= new Repository<MealPlan>(_context);
        public IRepository<MealPlanDay> MealPlanDayRepository => _mealPlanDayRepository ??= new Repository<MealPlanDay>(_context);
        public IRepository<Meal> MealRepository => _mealRepository ??= new Repository<Meal>(_context);
        public IRepository<FoodItem> FoodItemRepository => _foodItemRepository ??= new Repository<FoodItem>(_context);
        public IRepository<PatientMeasurement> PatientMeasurementRepository => _patientMeasurementRepository ??= new Repository<PatientMeasurement>(_context);

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> BeginTransactionAsync()
        {
            try
            {
                await _context.Database.BeginTransactionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CommitAsync()
        {
            try
            {
                await _context.Database.CommitTransactionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RollbackAsync()
        {
            try
            {
                await _context.Database.RollbackTransactionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
