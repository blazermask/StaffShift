using StaffShift.Core.DTOs;

namespace StaffShift.Tests;

/// <summary>
/// XUnit tests for DTOs and computed properties
/// </summary>
public class DtoTests
{
    // ---- TimeOffRequestDto ----

    [Fact]
    public void TimeOffRequestDto_TotalDays_EqualsDaysRequested()
    {
        var dto = new TimeOffRequestDto { DaysRequested = 5 };
        Assert.Equal(5, dto.TotalDays);
    }

    [Fact]
    public void TimeOffRequestDto_IsPaid_DefaultsToTrue()
    {
        var dto = new TimeOffRequestDto();
        Assert.True(dto.IsPaid);
    }

    [Fact]
    public void TimeOffRequestDto_ReviewerName_EqualsReviewedByName()
    {
        var dto = new TimeOffRequestDto { ReviewedByName = "John Manager" };
        Assert.Equal("John Manager", dto.ReviewerName);
    }

    [Fact]
    public void TimeOffRequestDto_ManagerNotes_IsNullByDefault()
    {
        var dto = new TimeOffRequestDto();
        Assert.Null(dto.ManagerNotes);
    }

    // ---- TimeOffSummaryDto ----

    [Fact]
    public void TimeOffSummaryDto_VacationDaysRemaining_CalculatedCorrectly()
    {
        var summary = new TimeOffSummaryDto
        {
            VacationDaysTotal = 20,
            VacationPaidDaysTotal = 20,
            VacationPaidDaysUsed = 5,
            VacationUnpaidDaysUsed = 2
        };
        // VacationDaysRemaining = VacationPaidDaysTotal - VacationPaidDaysUsed = 20 - 5 = 15
        Assert.Equal(15, summary.VacationDaysRemaining);
    }

    [Fact]
    public void TimeOffSummaryDto_VacationPaidDaysRemaining_CalculatedCorrectly()
    {
        var summary = new TimeOffSummaryDto
        {
            VacationPaidDaysTotal = 20,
            VacationPaidDaysUsed = 5
        };
        Assert.Equal(15, summary.VacationPaidDaysRemaining);
    }

    [Fact]
    public void TimeOffSummaryDto_VacationDaysUsed_CombinesPaidAndUnpaid()
    {
        var summary = new TimeOffSummaryDto
        {
            VacationPaidDaysUsed = 8,
            VacationUnpaidDaysUsed = 3
        };
        Assert.Equal(11, summary.VacationDaysUsed);
    }

    [Fact]
    public void TimeOffSummaryDto_TotalDaysUsed_SumsAllTypes()
    {
        var summary = new TimeOffSummaryDto
        {
            VacationPaidDaysUsed = 5,
            VacationUnpaidDaysUsed = 2,
            SickDaysUsed = 3,
            PersonalDaysUsed = 1
        };
        Assert.Equal(11, summary.TotalDaysUsed);
    }

    [Fact]
    public void TimeOffSummaryDto_PersonalDaysRemaining_CalculatedCorrectly()
    {
        var summary = new TimeOffSummaryDto
        {
            PersonalDaysTotal = 5,
            PersonalDaysUsed = 2
        };
        Assert.Equal(3, summary.PersonalDaysRemaining);
    }

    [Fact]
    public void TimeOffSummaryDto_SickDaysRemaining_CalculatedCorrectly()
    {
        var summary = new TimeOffSummaryDto
        {
            SickDaysTotal = 10,
            SickDaysUsed = 3
        };
        Assert.Equal(7, summary.SickDaysRemaining);
    }

    // ---- ForumPostDto computed properties ----

    [Fact]
    public void ForumPostDto_AuthorId_EqualsUserId()
    {
        var dto = new ForumPostDto { UserId = 42 };
        Assert.Equal(42, dto.AuthorId);
    }

    [Fact]
    public void ForumPostDto_AuthorName_EqualsUserName()
    {
        var dto = new ForumPostDto { UserName = "john_doe" };
        Assert.Equal("john_doe", dto.AuthorName);
    }

    [Fact]
    public void ForumPostDto_Department_FallsBackToTargetDepartment()
    {
        var dto = new ForumPostDto { TargetDepartment = "Engineering", UserDepartment = "HR" };
        Assert.Equal("Engineering", dto.Department);
    }

    [Fact]
    public void ForumPostDto_Department_FallsBackToUserDepartment_WhenTargetNull()
    {
        var dto = new ForumPostDto { UserDepartment = "Marketing" };
        Assert.Equal("Marketing", dto.Department);
    }

    // ---- UserDto ----

    [Fact]
    public void UserDto_Roles_DefaultsToEmptyList()
    {
        var dto = new UserDto();
        Assert.NotNull(dto.Roles);
        Assert.Empty(dto.Roles);
    }

    [Fact]
    public void UserDto_IsCEO_DefaultsFalse()
    {
        var dto = new UserDto();
        Assert.False(dto.IsCEO);
    }

    [Fact]
    public void UserDto_IsManager_DefaultsFalse()
    {
        var dto = new UserDto();
        Assert.False(dto.IsManager);
    }

    [Fact]
    public void UserDto_IsWorker_DefaultsFalse()
    {
        var dto = new UserDto();
        Assert.False(dto.IsWorker);
    }

    // ---- AssignManagerDto ----

    [Fact]
    public void AssignManagerDto_HasWorkerIdAndManagerId()
    {
        var dto = new AssignManagerDto { WorkerId = 5, ManagerId = 10 };
        Assert.Equal(5, dto.WorkerId);
        Assert.Equal(10, dto.ManagerId);
    }

    // ---- LoginDto ----

    [Fact]
    public void LoginDto_UsernameOrEmail_DefaultsEmpty()
    {
        var dto = new LoginDto();
        Assert.Equal(string.Empty, dto.UsernameOrEmail);
    }

    [Fact]
    public void LoginDto_Email_CanBeSet()
    {
        var dto = new LoginDto { Email = "test@example.com" };
        Assert.Equal("test@example.com", dto.Email);
    }

    [Fact]
    public void LoginDto_RememberMe_DefaultsFalse()
    {
        var dto = new LoginDto();
        Assert.False(dto.RememberMe);
    }

    // ---- CreateTimeOffRequestDto ----

    [Fact]
    public void CreateTimeOffRequestDto_RequestType_DefaultsVacation()
    {
        var dto = new CreateTimeOffRequestDto();
        Assert.Equal("Vacation", dto.RequestType);
    }

    [Fact]
    public void CreateTimeOffRequestDto_IsPaid_DefaultsTrue()
    {
        var dto = new CreateTimeOffRequestDto();
        Assert.True(dto.IsPaid);
    }

    // ---- ForumCommentDto ----

    [Fact]
    public void ForumCommentDto_AuthorName_EqualsUserName()
    {
        var dto = new ForumCommentDto { UserName = "janedoe" };
        Assert.Equal("janedoe", dto.AuthorName);
    }
}