using System.Linq.Expressions;

namespace DietitianClinic.DataAccess.Repositories
{
    /// <summary>
    /// Generic Repository Interface - Tüm repository'lerin kalıtım alacağı interface
    /// </summary>
    /// <typeparam name="T">Entity tipini belirten Type parameter</typeparam>
    public interface IRepository<T> where T : class
    {
        // Sorgulama işlemleri
        Task<T> GetByIdAsync(int id);
        Task<IList<T>> GetAllAsync();
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<IList<T>> GetByPredicateAsync(Expression<Func<T, bool>> predicate);

        // Ekleme işlemleri
        Task<int> AddAsync(T entity);
        Task<int> AddRangeAsync(IList<T> entities);

        // Güncelleme işlemleri
        Task<int> UpdateAsync(T entity);
        Task<int> UpdateRangeAsync(IList<T> entities);

        // Silme işlemleri
        Task<int> DeleteAsync(T entity);
        Task<int> DeleteRangeAsync(IList<T> entities);
        Task<int> DeleteAsync(int id);

        // Sayma işlemleri
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        // Paging
        Task<IList<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IList<T>> GetPagedAsync(
            Expression<Func<T, bool>> predicate, 
            int pageNumber, 
            int pageSize);
    }
}
