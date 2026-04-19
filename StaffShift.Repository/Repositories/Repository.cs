using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StaffShift.Data;
using StaffShift.Repository.Interfaces;

namespace StaffShift.Repository.Repositories;

/// <summary>
/// Generic repository implementation for basic CRUD operations
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly StaffShiftDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(StaffShiftDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}