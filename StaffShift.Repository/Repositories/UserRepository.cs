using Microsoft.EntityFrameworkCore;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Interfaces;

namespace StaffShift.Repository.Repositories;

/// <summary>
/// Repository implementation for User-specific operations
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(StaffShiftDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.Subordinates)
            .FirstOrDefaultAsync(u => u.UserName != null && u.UserName.ToLower() == username.ToLower());
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Subordinates)
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetUserWithSubordinatesAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.Subordinates)
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<IEnumerable<User>> GetUsersByDepartmentAsync(string department)
    {
        return await _dbSet
            .Include(u => u.Manager)
            .Where(u => u.Department != null && u.Department.ToLower() == department.ToLower())
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetWorkersByManagerAsync(int managerId)
    {
        return await _dbSet
            .Where(u => u.ManagerId == managerId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllWorkersAsync()
    {
        return await _dbSet
            .Include(u => u.Manager)
            .Where(u => u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllManagersAsync()
    {
        return await _dbSet
            .Include(u => u.Subordinates)
            .Where(u => u.IsActive && u.Subordinates.Any())
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersWithoutManagerAsync()
    {
        return await _dbSet
            .Where(u => u.ManagerId == null && u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }
}