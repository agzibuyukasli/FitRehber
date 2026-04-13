using System.Linq.Expressions;

namespace DietitianClinic.DataAccess.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IList<T>> GetAllAsync();
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<IList<T>> GetByPredicateAsync(Expression<Func<T, bool>> predicate);

        Task<int> AddAsync(T entity);
        Task<int> AddRangeAsync(IList<T> entities);

        Task<int> UpdateAsync(T entity);
        Task<int> UpdateRangeAsync(IList<T> entities);

        Task<int> DeleteAsync(T entity);
        Task<int> DeleteRangeAsync(IList<T> entities);
        Task<int> DeleteAsync(int id);

        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        Task<IList<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IList<T>> GetPagedAsync(
            Expression<Func<T, bool>> predicate,
            int pageNumber,
            int pageSize);
    }
}
