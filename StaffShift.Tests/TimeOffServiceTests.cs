using Microsoft.EntityFrameworkCore;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Repositories;
using StaffShift.Services.Services;

namespace StaffShift.Tests;

/// <summary>
/// XUnit tests for TimeOffService
/// </summary>
public class TimeOffServiceTests : IDisposable
{
    private readonly StaffShiftDbContext _context;
    private readonly TimeOffService _timeOffService;
    private readonly TimeOffRepository _timeOffRepository;
    private readonly UserRepository _userRepository;

    public TimeOffServiceTests()
    {
        var options = new DbContextOptionsBuilder<StaffShiftDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new StaffShiftDbContext(options);
        _timeOffRepository = new TimeOffRepository(_context);
        _userRepository = new UserRepository(_context);
        _timeOffService = new TimeOffService(_timeOffRepository, _userRepository);

        SeedData();
    }

    private void SeedData()
    {
        var manager = new User
        {
            Id = 1,
            UserName = "manager",
            Email = "manager@test.com",
            FirstName = "John",
            LastName = "Manager",
            IsActive = true,
            HireDate = DateTime.UtcNow.AddYears(-2)
        };
        var worker = new User
        {
            Id = 2,
            UserName = "worker",
            Email = "worker@test.com",
            FirstName = "Jane",
            LastName = "Worker",
            IsActive = true,
            ManagerId = 1,
            HireDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Users.AddRange(manager, worker);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateRequestAsync_ValidVacationRequest_ReturnsSuccess()
    {
        var model = new CreateTimeOffRequestDto
        {
            StartDate = DateTime.Today.AddDays(7),
            EndDate = DateTime.Today.AddDays(9),
            RequestType = "Vacation",
            Reason = "Family trip"
        };

        var result = await _timeOffService.CreateRequestAsync(model, 2);

        Assert.True(result.Success);
        Assert.NotNull(result.Request);
        Assert.Equal("Pending", result.Request.Status);
        Assert.Equal("Vacation", result.Request.RequestType);
        Assert.Equal(3, result.Request.DaysRequested);
    }

    [Fact]
    public async Task CreateRequestAsync_InvalidDateRange_ReturnsFail()
    {
        var model = new CreateTimeOffRequestDto
        {
            StartDate = DateTime.Today.AddDays(10),
            EndDate = DateTime.Today.AddDays(5),
            RequestType = "Vacation"
        };

        var result = await _timeOffService.CreateRequestAsync(model, 2);

        Assert.False(result.Success);
        Assert.Contains("End date", result.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_UserNotFound_ReturnsFail()
    {
        var model = new CreateTimeOffRequestDto
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(2),
            RequestType = "Vacation"
        };

        var result = await _timeOffService.CreateRequestAsync(model, 999);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRequestAsync_SickPaid_SetsPaidFlagTrue()
    {
        var model = new CreateTimeOffRequestDto
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(1),
            RequestType = "Sick",
            IsPaid = true
        };

        var result = await _timeOffService.CreateRequestAsync(model, 2);

        Assert.True(result.Success);
        Assert.True(result.Request!.IsPaid);
        Assert.Equal("Sick", result.Request.RequestType);
    }

    [Fact]
    public async Task CreateRequestAsync_SickUnpaid_SetsPaidFlagFalse()
    {
        var model = new CreateTimeOffRequestDto
        {
            StartDate = DateTime.Today.AddDays(2),
            EndDate = DateTime.Today.AddDays(2),
            RequestType = "Sick",
            IsPaid = false
        };

        var result = await _timeOffService.CreateRequestAsync(model, 2);

        Assert.True(result.Success);
        Assert.False(result.Request!.IsPaid);
    }

    [Fact]
    public async Task GetRequestsByUserAsync_ReturnsOnlyUserRequests()
    {
        await _context.TimeOffRequests.AddRangeAsync(
            new TimeOffRequest { UserId = 2, StartDate = DateTime.Today, EndDate = DateTime.Today, RequestType = "Vacation", Status = "Pending" },
            new TimeOffRequest { UserId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today, RequestType = "Vacation", Status = "Pending" }
        );
        await _context.SaveChangesAsync();

        var result = await _timeOffService.GetRequestsByUserAsync(2);

        Assert.All(result, r => Assert.Equal(2, r.UserId));
    }

    [Fact]
    public async Task ReviewRequestAsync_ApproveRequest_UpdatesStatusToApproved()
    {
        var request = new TimeOffRequest
        {
            UserId = 2,
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(7),
            RequestType = "Vacation",
            Status = "Pending"
        };
        _context.TimeOffRequests.Add(request);
        await _context.SaveChangesAsync();

        var reviewModel = new ReviewTimeOffRequestDto
        {
            RequestId = request.Id,
            Status = "Approved",
            ReviewNotes = "Have a great vacation!"
        };

        var result = await _timeOffService.ReviewRequestAsync(reviewModel, 1);

        Assert.True(result.Success);
        Assert.Equal("Approved", result.Request!.Status);
    }

    [Fact]
    public async Task ReviewRequestAsync_RejectRequest_UpdatesStatusToRejected()
    {
        var request = new TimeOffRequest
        {
            UserId = 2,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(2),
            RequestType = "Personal",
            Status = "Pending"
        };
        _context.TimeOffRequests.Add(request);
        await _context.SaveChangesAsync();

        var reviewModel = new ReviewTimeOffRequestDto
        {
            RequestId = request.Id,
            Status = "Rejected",
            ReviewNotes = "Insufficient notice."
        };

        var result = await _timeOffService.ReviewRequestAsync(reviewModel, 1);

        Assert.True(result.Success);
        Assert.Equal("Rejected", result.Request!.Status);
    }

    [Fact]
    public async Task ReviewRequestAsync_AlreadyReviewed_ReturnsFail()
    {
        var request = new TimeOffRequest
        {
            UserId = 2,
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(7),
            RequestType = "Vacation",
            Status = "Approved"
        };
        _context.TimeOffRequests.Add(request);
        await _context.SaveChangesAsync();

        var reviewModel = new ReviewTimeOffRequestDto { RequestId = request.Id, Status = "Rejected" };

        var result = await _timeOffService.ReviewRequestAsync(reviewModel, 1);

        Assert.False(result.Success);
        Assert.Contains("already been reviewed", result.Message);
    }

    [Fact]
    public async Task CancelRequestAsync_OwnPendingRequest_ReturnsSuccess()
    {
        var request = new TimeOffRequest
        {
            UserId = 2,
            StartDate = DateTime.Today.AddDays(10),
            EndDate = DateTime.Today.AddDays(12),
            RequestType = "Vacation",
            Status = "Pending"
        };
        _context.TimeOffRequests.Add(request);
        await _context.SaveChangesAsync();

        var result = await _timeOffService.CancelRequestAsync(request.Id, 2);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CancelRequestAsync_OtherUsersRequest_ReturnsFail()
    {
        var request = new TimeOffRequest
        {
            UserId = 2,
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(7),
            RequestType = "Vacation",
            Status = "Pending"
        };
        _context.TimeOffRequests.Add(request);
        await _context.SaveChangesAsync();

        var result = await _timeOffService.CancelRequestAsync(request.Id, 1);

        Assert.False(result.Success);
        Assert.Contains("your own", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTimeOffSummaryAsync_ReturnsCorrectPaidUnpaidBreakdown()
    {
        var year = DateTime.UtcNow.Year;
        await _context.TimeOffRequests.AddRangeAsync(
            new TimeOffRequest { UserId = 2, StartDate = new DateTime(year, 3, 1), EndDate = new DateTime(year, 3, 5), RequestType = "Vacation", Status = "Approved", IsPaid = true },
            new TimeOffRequest { UserId = 2, StartDate = new DateTime(year, 4, 1), EndDate = new DateTime(year, 4, 2), RequestType = "Sick", Status = "Approved", IsPaid = true },
            new TimeOffRequest { UserId = 2, StartDate = new DateTime(year, 5, 1), EndDate = new DateTime(year, 5, 1), RequestType = "Sick", Status = "Approved", IsPaid = false }
        );
        await _context.SaveChangesAsync();

        var summary = await _timeOffService.GetTimeOffSummaryAsync(2, year);

        Assert.Equal(2, summary.UserId);
        Assert.Equal(5, summary.VacationDaysUsed);
        Assert.Equal(2, summary.SickPaidDaysUsed);
        Assert.Equal(1, summary.SickUnpaidDaysUsed);
        Assert.Equal(3, summary.SickDaysUsed);
        Assert.Equal(15, summary.VacationDaysRemaining);
        Assert.Equal(8, summary.SickPaidDaysRemaining);
    }

    [Fact]
    public async Task GetPendingRequestsByManagerAsync_ReturnsPendingTeamRequests()
    {
        await _context.TimeOffRequests.AddAsync(new TimeOffRequest
        {
            UserId = 2,
            StartDate = DateTime.Today.AddDays(3),
            EndDate = DateTime.Today.AddDays(4),
            RequestType = "Vacation",
            Status = "Pending"
        });
        await _context.SaveChangesAsync();

        var result = await _timeOffService.GetPendingRequestsByManagerAsync(1);

        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.Equal("Pending", r.Status));
    }

    [Fact]
    public async Task GetRequestByIdAsync_ExistingRequest_ReturnsDto()
    {
        var request = new TimeOffRequest
        {
            UserId = 2,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(3),
            RequestType = "Personal",
            Status = "Pending"
        };
        _context.TimeOffRequests.Add(request);
        await _context.SaveChangesAsync();

        var result = await _timeOffService.GetRequestByIdAsync(request.Id, 2);

        Assert.NotNull(result);
        Assert.Equal("Personal", result.RequestType);
    }

    [Fact]
    public async Task GetRequestByIdAsync_NonExistentRequest_ReturnsNull()
    {
        var result = await _timeOffService.GetRequestByIdAsync(9999, 1);
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}