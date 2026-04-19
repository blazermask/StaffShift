using StaffShift.Core.DTOs;

namespace StaffShift.Services.Interfaces;

/// <summary>
/// Service interface for shift management operations
/// </summary>
public interface IShiftService
{
    Task<ShiftDto?> GetShiftByIdAsync(int shiftId, int? currentUserId = null);
    Task<IEnumerable<ShiftDto>> GetShiftsByUserAsync(int userId, int? currentUserId = null);
    Task<IEnumerable<ShiftDto>> GetUpcomingShiftsAsync(int userId, int? currentUserId = null);
    Task<IEnumerable<ShiftDto>> GetTeamShiftsAsync(int managerId, DateTime startDate, DateTime endDate, int? currentUserId = null);
    Task<IEnumerable<ShiftDto>> GetShiftsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate, int? currentUserId = null);
    Task<(bool Success, string Message, ShiftDto? Shift)> CreateShiftAsync(CreateShiftDto model, int createdBy);
    Task<(bool Success, string Message, ShiftDto? Shift)> UpdateShiftAsync(UpdateShiftDto model, int modifiedBy);
    Task<(bool Success, string Message)> DeleteShiftAsync(int shiftId);
    Task<(bool Success, string Message, ShiftDto? Shift)> ClockInAsync(int shiftId, TimeSpan? actualTime = null);
    Task<(bool Success, string Message, ShiftDto? Shift)> ClockOutAsync(int shiftId, TimeSpan? actualTime = null);
    Task<Dictionary<string, double>> GetWeeklyHoursAsync(int userId);
}