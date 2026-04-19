using Microsoft.EntityFrameworkCore;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Interfaces;

namespace StaffShift.Repository.Repositories;

/// <summary>
/// Repository implementation for TimeOffRequest-specific operations
/// </summary>
public class TimeOffRepository : Repository<TimeOffRequest>, ITimeOffRepository
{
    public TimeOffRepository(StaffShiftDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TimeOffRequest>> GetRequestsByUserAsync(int userId)
    {
        return await _dbSet
            .Include(r => r.User)
            .Include(r => r.ReviewedByUser)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TimeOffRequest>> GetPendingRequestsAsync()
    {
        return await _dbSet
            .Include(r => r.User)
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TimeOffRequest>> GetPendingRequestsByManagerAsync(int managerId)
    {
        return await _dbSet
            .Include(r => r.User)
            .Where(r => r.Status == "Pending" && r.User.ManagerId == managerId)
            .OrderBy(r => r.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TimeOffRequest>> GetRequestsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(r => r.User)
            .Where(r => r.StartDate <= endDate && r.EndDate >= startDate)
            .OrderBy(r => r.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TimeOffRequest>> GetApprovedTimeOffByUserAsync(int userId, int year)
    {
        return await _dbSet
            .Where(r => r.UserId == userId && 
                       r.Status == "Approved" && 
                       r.StartDate.Year == year)
            .OrderBy(r => r.StartDate)
            .ToListAsync();
    }
}