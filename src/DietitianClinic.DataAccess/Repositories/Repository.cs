using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.Entity.Base;

namespace DietitianClinic.DataAccess.Repositories
{
    /// <summary>
    /// Generic Repository implementation
    /// </summary>
    /// <typeparam name="T">Entity tipi</typeparam>
    public class Repository<T> : IRepository<T> where T : BaseEntity, new()
    {
        protected readonly DietitianClinicDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DietitianClinicDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public virtual async Task<IList<T>> GetAllAsync()
        {
            return await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(x => !x.IsDeleted).AnyAsync(predicate);
        }

        public virtual async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(x => !x.IsDeleted).FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<IList<T>> GetByPredicateAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(x => !x.IsDeleted).Where(predicate).ToListAsync();
        }

        public virtual async Task<int> AddAsync(T entity)
        {
            entity.CreatedDate = DateTime.UtcNow;
            await _dbSet.AddAsync(entity);
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<int> AddRangeAsync(IList<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.CreatedDate = DateTime.UtcNow;
            }
            await _dbSet.AddRangeAsync(entities);
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<int> UpdateAsync(T entity)
        {
            entity.ModifiedDate = DateTime.UtcNow;
            _dbSet.Update(entity);
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<int> UpdateRangeAsync(IList<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.ModifiedDate = DateTime.UtcNow;
            }
            _dbSet.UpdateRange(entities);
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<int> DeleteAsync(T entity)
        {
            entity.IsDeleted = true;
            entity.DeletedDate = DateTime.UtcNow;
            _dbSet.Update(entity);
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<int> DeleteRangeAsync(IList<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.DeletedDate = DateTime.UtcNow;
            }
            _dbSet.UpdateRange(entities);
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<int> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                return await DeleteAsync(entity);
            }
            return 0;
        }

        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.Where(x => !x.IsDeleted).CountAsync();
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(x => !x.IsDeleted).CountAsync(predicate);
        }

        public virtual async Task<IList<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var skip = (pageNumber - 1) * pageSize;
            return await _dbSet.Where(x => !x.IsDeleted)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public virtual async Task<IList<T>> GetPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize)
        {
            var skip = (pageNumber - 1) * pageSize;
            return await _dbSet.Where(x => !x.IsDeleted)
                .Where(predicate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
