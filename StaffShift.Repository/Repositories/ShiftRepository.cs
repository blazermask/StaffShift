using Microsoft.EntityFrameworkCore;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Interfaces;

namespace StaffShift.Repository.Repositories;

/// <summary>
/// Repository implementation for Shift-specific operations
/// </summary>
public class ShiftRepository : Repository<Shift>, IShiftRepository
{
    public ShiftRepository(StaffShiftDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Shift>> GetShiftsByUserAsync(int userId)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ShiftDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shift>> GetShiftsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.ShiftDate >= startDate && s.ShiftDate <= endDate)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shift>> GetShiftsByUserAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.UserId == userId && s.ShiftDate >= startDate && s.ShiftDate <= endDate)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<Shift?> GetShiftByUserAndDateAsync(int userId, DateTime date)
    {
        return await _dbSet
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ShiftDate.Date == date.Date);
    }

    public async Task<IEnumerable<Shift>> GetUpcomingShiftsAsync(int userId, int days = 7)
    {
        var today = DateTime.UtcNow.Date;
        var endDate = today.AddDays(days);
        
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.UserId == userId && s.ShiftDate >= today && s.ShiftDate <= endDate)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shift>> GetTeamShiftsAsync(int managerId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.User.ManagerId == managerId && s.ShiftDate >= startDate && s.ShiftDate <= endDate)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }
}