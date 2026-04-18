using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialApp.DAL.EF.Repositories
{
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        private readonly FinancialDbContext _context;
        private readonly DbSet<T> _dbSet;

        // Dependency Injection provides the DbContext
        public GenericRepository(FinancialDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            // AsNoTracking is a massive performance boost for read-only operations
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            // FindAsync prevents UI freezing during database lookups
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity of type {typeof(T).Name} with ID {id} was not found.");
            }
            return entity;
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}