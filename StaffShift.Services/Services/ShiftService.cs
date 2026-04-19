using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Repository.Interfaces;
using StaffShift.Services.Interfaces;

namespace StaffShift.Services.Services;

/// <summary>
/// Service implementation for shift management operations
/// </summary>
public class ShiftService : IShiftService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IUserRepository _userRepository;

    public ShiftService(IShiftRepository shiftRepository, IUserRepository userRepository)
    {
        _shiftRepository = shiftRepository;
        _userRepository = userRepository;
    }

    public async Task<ShiftDto?> GetShiftByIdAsync(int shiftId, int? currentUserId = null)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId);
        if (shift == null) return null;

        return await MapToShiftDto(shift, currentUserId);
    }

    public async Task<IEnumerable<ShiftDto>> GetShiftsByUserAsync(int userId, int? currentUserId = null)
    {
        var shifts = await _shiftRepository.GetShiftsByUserAsync(userId);
        var shiftDtos = new List<ShiftDto>();
        foreach (var shift in shifts)
        {
            shiftDtos.Add(await MapToShiftDto(shift, currentUserId));
        }
        return shiftDtos;
    }

    public async Task<IEnumerable<ShiftDto>> GetUpcomingShiftsAsync(int userId, int? currentUserId = null)
    {
        var shifts = await _shiftRepository.GetUpcomingShiftsAsync(userId);
        var shiftDtos = new List<ShiftDto>();
        foreach (var shift in shifts)
        {
            shiftDtos.Add(await MapToShiftDto(shift, currentUserId));
        }
        return shiftDtos;
    }

    public async Task<IEnumerable<ShiftDto>> GetTeamShiftsAsync(int managerId, DateTime startDate, DateTime endDate, int? currentUserId = null)
    {
        var shifts = await _shiftRepository.GetTeamShiftsAsync(managerId, startDate, endDate);
        var shiftDtos = new List<ShiftDto>();
        foreach (var shift in shifts)
        {
            shiftDtos.Add(await MapToShiftDto(shift, currentUserId));
        }
        return shiftDtos;
    }

    public async Task<IEnumerable<ShiftDto>> GetShiftsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate, int? currentUserId = null)
    {
        var shifts = await _shiftRepository.GetShiftsByUserAndDateRangeAsync(userId, startDate, endDate);
        var shiftDtos = new List<ShiftDto>();
        foreach (var shift in shifts)
        {
            shiftDtos.Add(await MapToShiftDto(shift, currentUserId));
        }
        return shiftDtos;
    }

    public async Task<(bool Success, string Message, ShiftDto? Shift)> CreateShiftAsync(CreateShiftDto model, int createdBy)
    {
        var user = await _userRepository.GetByIdAsync(model.UserId);
        if (user == null)
        {
            return (false, "User not found.", null);
        }

        // Check if shift already exists for this user on this date
        var existingShift = await _shiftRepository.GetShiftByUserAndDateAsync(model.UserId, model.ShiftDate);
        if (existingShift != null)
        {
            return (false, "A shift already exists for this user on this date.", null);
        }

        var shift = new Shift
        {
            UserId = model.UserId,
            ShiftDate = model.ShiftDate,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Notes = model.Notes,
            HoursWorked = (model.EndTime - model.StartTime).TotalHours,
            Status = "Scheduled",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        await _shiftRepository.AddAsync(shift);
        await _shiftRepository.SaveChangesAsync();

        var shiftDto = await MapToShiftDto(shift, createdBy);
        return (true, "Shift created successfully!", shiftDto);
    }

    public async Task<(bool Success, string Message, ShiftDto? Shift)> UpdateShiftAsync(UpdateShiftDto model, int modifiedBy)
    {
        var shift = await _shiftRepository.GetByIdAsync(model.Id);
        if (shift == null)
        {
            return (false, "Shift not found.", null);
        }

        if (model.ShiftDate.HasValue) shift.ShiftDate = model.ShiftDate.Value;
        if (model.StartTime.HasValue) shift.StartTime = model.StartTime.Value;
        if (model.EndTime.HasValue) shift.EndTime = model.EndTime.Value;
        if (model.ActualStartTime.HasValue) shift.ActualStartTime = model.ActualStartTime.Value;
        if (model.ActualEndTime.HasValue) shift.ActualEndTime = model.ActualEndTime.Value;
        if (model.Status != null) shift.Status = model.Status;
        if (model.Notes != null) shift.Notes = model.Notes;

        // Recalculate hours worked
        if (model.ActualStartTime.HasValue && model.ActualEndTime.HasValue)
        {
            shift.HoursWorked = (model.ActualEndTime.Value - model.ActualStartTime.Value).TotalHours;
        }
        else if (model.StartTime.HasValue && model.EndTime.HasValue)
        {
            shift.HoursWorked = (model.EndTime.Value - model.StartTime.Value).TotalHours;
        }

        shift.UpdatedAt = DateTime.UtcNow;

        await _shiftRepository.UpdateAsync(shift);
        await _shiftRepository.SaveChangesAsync();

        var shiftDto = await MapToShiftDto(shift, modifiedBy);
        return (true, "Shift updated successfully!", shiftDto);
    }

    public async Task<(bool Success, string Message)> DeleteShiftAsync(int shiftId)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId);
        if (shift == null)
        {
            return (false, "Shift not found.");
        }

        await _shiftRepository.DeleteAsync(shift);
        await _shiftRepository.SaveChangesAsync();

        return (true, "Shift deleted successfully!");
    }

    public async Task<(bool Success, string Message, ShiftDto? Shift)> ClockInAsync(int shiftId, TimeSpan? actualTime = null)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId);
        if (shift == null)
        {
            return (false, "Shift not found.", null);
        }

        if (shift.ActualStartTime.HasValue)
        {
            return (false, "Already clocked in.", null);
        }

        shift.ActualStartTime = actualTime ?? DateTime.Now.TimeOfDay;
        shift.Status = "In Progress";
        shift.UpdatedAt = DateTime.UtcNow;

        await _shiftRepository.UpdateAsync(shift);
        await _shiftRepository.SaveChangesAsync();

        var shiftDto = await MapToShiftDto(shift, null);
        return (true, "Clocked in successfully!", shiftDto);
    }

    public async Task<(bool Success, string Message, ShiftDto? Shift)> ClockOutAsync(int shiftId, TimeSpan? actualTime = null)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId);
        if (shift == null)
        {
            return (false, "Shift not found.", null);
        }

        if (!shift.ActualStartTime.HasValue)
        {
            return (false, "Not clocked in yet.", null);
        }

        if (shift.ActualEndTime.HasValue)
        {
            return (false, "Already clocked out.", null);
        }

        shift.ActualEndTime = actualTime ?? DateTime.Now.TimeOfDay;
        shift.HoursWorked = (shift.ActualEndTime.Value - shift.ActualStartTime.Value).TotalHours;
        shift.Status = "Completed";
        shift.UpdatedAt = DateTime.UtcNow;

        await _shiftRepository.UpdateAsync(shift);
        await _shiftRepository.SaveChangesAsync();

        var shiftDto = await MapToShiftDto(shift, null);
        return (true, "Clocked out successfully!", shiftDto);
    }

    public async Task<Dictionary<string, double>> GetWeeklyHoursAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);

        var shifts = await _shiftRepository.GetShiftsByUserAndDateRangeAsync(userId, startOfWeek, endOfWeek);
        var weeklyHours = new Dictionary<string, double>();

        for (int i = 0; i < 7; i++)
        {
            var day = startOfWeek.AddDays(i);
            var dayShifts = shifts.Where(s => s.ShiftDate.Date == day.Date);
            var totalHours = dayShifts.Sum(s => s.HoursWorked);
            weeklyHours[day.DayOfWeek.ToString()] = totalHours;
        }

        return weeklyHours;
    }

    private async Task<ShiftDto> MapToShiftDto(Shift shift, int? currentUserId)
    {
        var user = await _userRepository.GetByIdAsync(shift.UserId);
        var createdByUser = shift.CreatedBy.HasValue ? await _userRepository.GetByIdAsync(shift.CreatedBy.Value) : null;

        return new ShiftDto
        {
            Id = shift.Id,
            UserId = shift.UserId,
            UserName = user?.UserName,
            UserDepartment = user?.Department,
            ShiftDate = shift.ShiftDate,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            HoursWorked = shift.HoursWorked,
            ActualStartTime = shift.ActualStartTime,
            ActualEndTime = shift.ActualEndTime,
            Status = shift.Status,
            Notes = shift.Notes,
            CreatedAt = shift.CreatedAt,
            IsCurrentUser = currentUserId.HasValue && currentUserId.Value == shift.UserId
        };
    }
}