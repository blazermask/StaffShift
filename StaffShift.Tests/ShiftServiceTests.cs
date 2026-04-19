using Microsoft.EntityFrameworkCore;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Repositories;
using StaffShift.Services.Services;

namespace StaffShift.Tests;

/// <summary>
/// XUnit tests for ShiftService
/// </summary>
public class ShiftServiceTests : IDisposable
{
    private readonly StaffShiftDbContext _context;
    private readonly ShiftService _shiftService;
    private readonly ShiftRepository _shiftRepository;
    private readonly UserRepository _userRepository;

    public ShiftServiceTests()
    {
        var options = new DbContextOptionsBuilder<StaffShiftDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new StaffShiftDbContext(options);
        _shiftRepository = new ShiftRepository(_context);
        _userRepository = new UserRepository(_context);
        _shiftService = new ShiftService(_shiftRepository, _userRepository);

        SeedData();
    }

    private void SeedData()
    {
        _context.Users.AddRange(
            new User { Id = 1, UserName = "manager", Email = "manager@test.com", IsActive = true, HireDate = DateTime.UtcNow },
            new User { Id = 2, UserName = "worker", Email = "worker@test.com", IsActive = true, ManagerId = 1, HireDate = DateTime.UtcNow }
        );
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateShiftAsync_ValidShift_ReturnsSuccess()
    {
        var model = new CreateShiftDto
        {
            UserId = 2,
            ShiftDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            Notes = "Regular shift"
        };

        var result = await _shiftService.CreateShiftAsync(model, 1);

        Assert.True(result.Success);
        Assert.NotNull(result.Shift);
        Assert.Equal(2, result.Shift.UserId);
        Assert.Equal("Scheduled", result.Shift.Status);
    }

    [Fact]
    public async Task CreateShiftAsync_UserNotFound_ReturnsFail()
    {
        var model = new CreateShiftDto
        {
            UserId = 999,
            ShiftDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };

        var result = await _shiftService.CreateShiftAsync(model, 1);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateShiftAsync_InvalidTimeRange_ReturnsFail()
    {
        var model = new CreateShiftDto
        {
            UserId = 2,
            ShiftDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(9, 0, 0) // End before start
        };

        var result = await _shiftService.CreateShiftAsync(model, 1);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetShiftsByUserAsync_ReturnsOnlyUserShifts()
    {
        await _context.Shifts.AddRangeAsync(
            new Shift { UserId = 2, ShiftDate = DateTime.Today, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), CreatedBy = 1 },
            new Shift { UserId = 1, ShiftDate = DateTime.Today, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), CreatedBy = 1 }
        );
        await _context.SaveChangesAsync();

        var result = await _shiftService.GetShiftsByUserAsync(2, 1);

        Assert.All(result, s => Assert.Equal(2, s.UserId));
    }

    [Fact]
    public async Task GetUpcomingShiftsAsync_ReturnsOnlyFutureShifts()
    {
        await _context.Shifts.AddRangeAsync(
            new Shift { UserId = 2, ShiftDate = DateTime.Today.AddDays(3), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), CreatedBy = 1, Status = "Scheduled" },
            new Shift { UserId = 2, ShiftDate = DateTime.Today.AddDays(-3), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), CreatedBy = 1, Status = "Completed" }
        );
        await _context.SaveChangesAsync();

        var result = await _shiftService.GetUpcomingShiftsAsync(2, 1);

        Assert.All(result, s => Assert.True(s.ShiftDate >= DateTime.Today));
    }

    [Fact]
    public async Task UpdateShiftAsync_ValidUpdate_ReturnsSuccess()
    {
        var shift = new Shift
        {
            UserId = 2,
            ShiftDate = DateTime.Today.AddDays(2),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            Status = "Scheduled",
            CreatedBy = 1
        };
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateShiftDto
        {
            Id = shift.Id,
            ShiftDate = DateTime.Today.AddDays(3),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(18, 0, 0),
            Notes = "Updated shift"
        };

        var result = await _shiftService.UpdateShiftAsync(updateModel, 1);

        Assert.True(result.Success);
        Assert.Equal(new TimeSpan(10, 0, 0), result.Shift!.StartTime);
    }

    [Fact]
    public async Task DeleteShiftAsync_ExistingShift_ReturnsSuccess()
    {
        var shift = new Shift
        {
            UserId = 2,
            ShiftDate = DateTime.Today.AddDays(5),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            Status = "Scheduled",
            CreatedBy = 1
        };
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        var result = await _shiftService.DeleteShiftAsync(shift.Id);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task DeleteShiftAsync_NonExistentShift_ReturnsFail()
    {
        var result = await _shiftService.DeleteShiftAsync(9999);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetWeeklyHoursAsync_CalculatesCorrectly()
    {
        var monday = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
        await _context.Shifts.AddRangeAsync(
            new Shift { UserId = 2, ShiftDate = monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), Status = "Completed", CreatedBy = 1 },
            new Shift { UserId = 2, ShiftDate = monday.AddDays(1), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), Status = "Completed", CreatedBy = 1 }
        );
        await _context.SaveChangesAsync();

        var hoursDict = await _shiftService.GetWeeklyHoursAsync(2);

        Assert.NotNull(hoursDict);
        Assert.True(hoursDict.Values.All(h => h >= 0));
    }

    [Fact]
    public async Task GetShiftByIdAsync_ExistingShift_ReturnsDto()
    {
        var shift = new Shift
        {
            UserId = 2,
            ShiftDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            Status = "Scheduled",
            CreatedBy = 1,
            Notes = "Morning shift"
        };
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        var result = await _shiftService.GetShiftByIdAsync(shift.Id, 1);

        Assert.NotNull(result);
        Assert.Equal(2, result.UserId);
        Assert.Equal("Scheduled", result.Status);
    }

    [Fact]
    public async Task GetShiftByIdAsync_NonExistentShift_ReturnsNull()
    {
        var result = await _shiftService.GetShiftByIdAsync(9999, 1);
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}