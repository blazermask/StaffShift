namespace StaffShift.Core.DTOs;

// View Models for Manager Controller
public class TeamMemberViewModel
{
    public UserDto User { get; set; } = null!;
    public int TotalShifts { get; set; }
    public int PendingTimeOffRequests { get; set; }
}

public class MemberDetailViewModel
{
    public UserDto User { get; set; } = null!;
    public IEnumerable<ShiftDto> Shifts { get; set; } = new List<ShiftDto>();
    public IEnumerable<TimeOffRequestDto> TimeOffRequests { get; set; } = new List<TimeOffRequestDto>();
    public TimeOffSummaryDto? TimeOffSummary { get; set; }
}

// View Models for CEO Controller
public class CEODashboardViewModel
{
    public int TotalEmployees { get; set; }
    public int TotalManagers { get; set; }
    public int TotalWorkers { get; set; }
    public int UnassignedWorkers { get; set; }
    public int PendingTimeOffRequests { get; set; }
    public int RecentShifts { get; set; }
}

public class DepartmentViewModel
{
    public string Name { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int ManagerCount { get; set; }
    public int WorkersWithoutManager { get; set; }
}

public class ReportsViewModel
{
    public int TotalEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public DateTime ReportDate { get; set; }
    public int TotalTimeOffRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
}