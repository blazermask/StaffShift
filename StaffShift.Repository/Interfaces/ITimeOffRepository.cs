using StaffShift.Core.Entities;

namespace StaffShift.Repository.Interfaces;

/// <summary>
/// Repository interface for TimeOffRequest-specific operations
/// </summary>
public interface ITimeOffRepository : IRepository<TimeOffRequest>
{
    Task<IEnumerable<TimeOffRequest>> GetRequestsByUserAsync(int userId);
    Task<IEnumerable<TimeOffRequest>> GetPendingRequestsAsync();
    Task<IEnumerable<TimeOffRequest>> GetPendingRequestsByManagerAsync(int managerId);
    Task<IEnumerable<TimeOffRequest>> GetRequestsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<TimeOffRequest>> GetApprovedTimeOffByUserAsync(int userId, int year);
}