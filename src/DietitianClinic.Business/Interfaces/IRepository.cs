using System.Collections.Generic;
using System.Threading.Tasks;

namespace DietitianClinic.Business.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IList<T>> GetAllAsync();
        Task<bool> AnyAsync(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate);
        Task<IList<T>> GetByPredicateAsync(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate);

        Task<int> AddAsync(T entity);
        Task<int> AddRangeAsync(IList<T> entities);

        Task<int> UpdateAsync(T entity);
        Task<int> UpdateRangeAsync(IList<T> entities);

        Task<int> DeleteAsync(T entity);
        Task<int> DeleteRangeAsync(IList<T> entities);
        Task<int> DeleteAsync(int id);

        Task<int> CountAsync();
        Task<int> CountAsync(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate);

        Task<IList<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IList<T>> GetPagedAsync(
            System.Linq.Expressions.Expression<System.Func<T, bool>> predicate,
            int pageNumber,
            int pageSize);
    }
}
