using StaffShift.Core.DTOs;

namespace StaffShift.Services.Interfaces;

/// <summary>
/// Service interface for time off request operations
/// </summary>
public interface ITimeOffService
{
    Task<TimeOffRequestDto?> GetRequestByIdAsync(int requestId, int? currentUserId = null);
    Task<IEnumerable<TimeOffRequestDto>> GetRequestsByUserAsync(int userId, int? currentUserId = null);
    Task<IEnumerable<TimeOffRequestDto>> GetPendingRequestsAsync(int? currentUserId = null);
    Task<IEnumerable<TimeOffRequestDto>> GetPendingRequestsByManagerAsync(int managerId, int? currentUserId = null);
    Task<(bool Success, string Message, TimeOffRequestDto? Request)> CreateRequestAsync(CreateTimeOffRequestDto model, int userId);
    Task<(bool Success, string Message, TimeOffRequestDto? Request)> ReviewRequestAsync(ReviewTimeOffRequestDto model, int reviewerId);
    Task<(bool Success, string Message)> CancelRequestAsync(int requestId, int userId);
    Task<TimeOffSummaryDto> GetTimeOffSummaryAsync(int userId, int year);
}