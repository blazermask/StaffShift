using StaffShift.Core.DTOs;
using StaffShift.Repository.Interfaces;
using StaffShift.Services.Interfaces;

namespace StaffShift.Services.Services;

/// <summary>
/// Service implementation for time off request operations
/// </summary>
public class TimeOffService : ITimeOffService
{
    private readonly ITimeOffRepository _timeOffRepository;
    private readonly IUserRepository _userRepository;

    public TimeOffService(ITimeOffRepository timeOffRepository, IUserRepository userRepository)
    {
        _timeOffRepository = timeOffRepository;
        _userRepository = userRepository;
    }

    public async Task<TimeOffRequestDto?> GetRequestByIdAsync(int requestId, int? currentUserId = null)
    {
        var request = await _timeOffRepository.GetByIdAsync(requestId);
        if (request == null) return null;

        return await MapToTimeOffRequestDto(request, currentUserId);
    }

    public async Task<IEnumerable<TimeOffRequestDto>> GetRequestsByUserAsync(int userId, int? currentUserId = null)
    {
        var requests = await _timeOffRepository.GetRequestsByUserAsync(userId);
        var requestDtos = new List<TimeOffRequestDto>();
        foreach (var request in requests)
        {
            requestDtos.Add(await MapToTimeOffRequestDto(request, currentUserId));
        }
        return requestDtos;
    }

    public async Task<IEnumerable<TimeOffRequestDto>> GetPendingRequestsAsync(int? currentUserId = null)
    {
        var requests = await _timeOffRepository.GetPendingRequestsAsync();
        var requestDtos = new List<TimeOffRequestDto>();
        foreach (var request in requests)
        {
            requestDtos.Add(await MapToTimeOffRequestDto(request, currentUserId));
        }
        return requestDtos;
    }

    public async Task<IEnumerable<TimeOffRequestDto>> GetPendingRequestsByManagerAsync(int managerId, int? currentUserId = null)
    {
        var requests = await _timeOffRepository.GetPendingRequestsByManagerAsync(managerId);
        var requestDtos = new List<TimeOffRequestDto>();
        foreach (var request in requests)
        {
            requestDtos.Add(await MapToTimeOffRequestDto(request, currentUserId));
        }
        return requestDtos;
    }

    public async Task<(bool Success, string Message, TimeOffRequestDto? Request)> CreateRequestAsync(CreateTimeOffRequestDto model, int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.", null);
        }

        // Validate dates
        if (model.EndDate < model.StartDate)
        {
            return (false, "End date must be after start date.", null);
        }

        // Check for overlapping requests
        var overlappingRequests = await _timeOffRepository.GetRequestsByDateRangeAsync(model.StartDate, model.EndDate);
        var hasOverlap = overlappingRequests.Any(r => 
            r.UserId == userId && 
            r.Status != "Rejected" && 
            r.Status != "Cancelled" &&
            ((model.StartDate >= r.StartDate && model.StartDate <= r.EndDate) ||
             (model.EndDate >= r.StartDate && model.EndDate <= r.EndDate) ||
             (model.StartDate <= r.StartDate && model.EndDate >= r.EndDate)));

        if (hasOverlap)
        {
            return (false, "You already have a time off request for this period.", null);
        }

        var request = new Core.Entities.TimeOffRequest
        {
            UserId = userId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            RequestType = model.RequestType,
            Reason = model.Reason,
            IsPaid = model.RequestType == "Sick" ? model.IsPaid : true,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _timeOffRepository.AddAsync(request);
        await _timeOffRepository.SaveChangesAsync();

        var requestDto = await MapToTimeOffRequestDto(request, userId);
        return (true, "Time off request submitted successfully!", requestDto);
    }

    public async Task<(bool Success, string Message, TimeOffRequestDto? Request)> ReviewRequestAsync(ReviewTimeOffRequestDto model, int reviewerId)
    {
        var request = await _timeOffRepository.GetByIdAsync(model.RequestId);
        if (request == null)
        {
            return (false, "Request not found.", null);
        }

        if (request.Status != "Pending")
        {
            return (false, "This request has already been reviewed.", null);
        }

        request.Status = model.Status;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = model.ReviewNotes;
        request.UpdatedAt = DateTime.UtcNow;

        await _timeOffRepository.UpdateAsync(request);
        await _timeOffRepository.SaveChangesAsync();

        var requestDto = await MapToTimeOffRequestDto(request, reviewerId);
        return (true, $"Request {model.Status.ToLower()} successfully!", requestDto);
    }

    public async Task<(bool Success, string Message)> CancelRequestAsync(int requestId, int userId)
    {
        var request = await _timeOffRepository.GetByIdAsync(requestId);
        if (request == null)
        {
            return (false, "Request not found.");
        }

        if (request.UserId != userId)
        {
            return (false, "You can only cancel your own requests.");
        }

        if (request.Status != "Pending")
        {
            return (false, "You can only cancel pending requests.");
        }

        request.Status = "Cancelled";
        request.UpdatedAt = DateTime.UtcNow;

        await _timeOffRepository.UpdateAsync(request);
        await _timeOffRepository.SaveChangesAsync();

        return (true, "Request cancelled successfully!");
    }

    public async Task<TimeOffSummaryDto> GetTimeOffSummaryAsync(int userId, int year)
    {
        var requests = await _timeOffRepository.GetApprovedTimeOffByUserAsync(userId, year);

        int vacationDays = 0, sickPaidDays = 0, sickUnpaidDays = 0, personalDays = 0;

        foreach (var request in requests)
        {
            switch (request.RequestType)
            {
                case "Vacation":
                    vacationDays += request.DaysRequested;
                    break;
                case "Sick":
                    if (request.IsPaid)
                        sickPaidDays += request.DaysRequested;
                    else
                        sickUnpaidDays += request.DaysRequested;
                    break;
                case "Personal":
                    personalDays += request.DaysRequested;
                    break;
            }
        }

        return new TimeOffSummaryDto
        {
            UserId = userId,
            VacationDaysUsed = vacationDays,
            VacationDaysTotal = 20,       // Default annual allowance
            SickPaidDaysUsed = sickPaidDays,
            SickPaidDaysTotal = 10,       // Default paid sick day allowance
            SickUnpaidDaysUsed = sickUnpaidDays,
            SickDaysTotal = 10,
            PersonalDaysUsed = personalDays,
            PersonalDaysTotal = 5         // Default personal day allowance
        };
    }

    private async Task<TimeOffRequestDto> MapToTimeOffRequestDto(Core.Entities.TimeOffRequest request, int? currentUserId)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        var reviewer = request.ReviewedBy.HasValue ? await _userRepository.GetByIdAsync(request.ReviewedBy.Value) : null;

        return new TimeOffRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            UserName = user?.UserName,
            UserDepartment = user?.Department,
            UserPosition = user?.Position,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DaysRequested = request.DaysRequested,
            RequestType = request.RequestType,
            IsPaid = request.IsPaid,
            Reason = request.Reason,
            Status = request.Status,
            ReviewedBy = request.ReviewedBy,
            ReviewedByName = reviewer?.UserName,
            ReviewerName = reviewer?.UserName,
            ReviewedAt = request.ReviewedAt,
            ReviewNotes = request.ReviewNotes,
            ManagerNotes = request.ReviewNotes,
            CreatedAt = request.CreatedAt,
            IsCurrentUser = currentUserId.HasValue && currentUserId.Value == request.UserId,
            CanApprove = currentUserId.HasValue && user?.ManagerId == currentUserId.Value
        };
    }
}