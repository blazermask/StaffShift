using StaffShift.Core.Entities;

namespace StaffShift.Repository.Interfaces;

/// <summary>
/// Repository interface for Shift-specific operations
/// </summary>
public interface IShiftRepository : IRepository<Shift>
{
    Task<IEnumerable<Shift>> GetShiftsByUserAsync(int userId);
    Task<IEnumerable<Shift>> GetShiftsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Shift>> GetShiftsByUserAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Shift?> GetShiftByUserAndDateAsync(int userId, DateTime date);
    Task<IEnumerable<Shift>> GetUpcomingShiftsAsync(int userId, int days = 7);
    Task<IEnumerable<Shift>> GetTeamShiftsAsync(int managerId, DateTime startDate, DateTime endDate);
}