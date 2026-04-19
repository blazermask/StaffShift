using System.ComponentModel.DataAnnotations;

namespace StaffShift.Core.DTOs;

/// <summary>
/// Data transfer object for time off request information
/// </summary>
public class TimeOffRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserDepartment { get; set; }
    public string? UserPosition { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRequested { get; set; }
    public string RequestType { get; set; } = "Vacation";
    public bool IsPaid { get; set; } = true;
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ManagerNotes { get; set; }
    public string? ReviewerName => ReviewedByName;
    public DateTime CreatedAt { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool CanApprove { get; set; }
    public int TotalDays => DaysRequested;
}

/// <summary>
/// Data transfer object for creating a time off request
/// </summary>
public class CreateTimeOffRequestDto
{
    [Required(ErrorMessage = "Start date is required")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Request type is required")]
    [StringLength(20)]
    public string RequestType { get; set; } = "Vacation";

    [StringLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Whether the vacation is paid (true) or unpaid (false). Only for Vacation requests.
    /// Sick leave is always paid.
    /// </summary>
    public bool IsPaid { get; set; } = true;
}

/// <summary>
/// Data transfer object for reviewing a time off request
/// </summary>
public class ReviewTimeOffRequestDto
{
    [Required]
    public int RequestId { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Approved";

    [StringLength(500)]
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Data transfer object for time off summary statistics
/// </summary>
public class TimeOffSummaryDto
{
    public int UserId { get; set; }

    // Vacation - paid
    public int VacationPaidDaysUsed { get; set; }
    public int VacationPaidDaysTotal { get; set; }
    public int VacationPaidDaysRemaining => VacationPaidDaysTotal - VacationPaidDaysUsed;

    // Vacation - unpaid
    public int VacationUnpaidDaysUsed { get; set; }

    // Combined vacation (for backward compat)
    public int VacationDaysUsed => VacationPaidDaysUsed + VacationUnpaidDaysUsed;
    public int VacationDaysTotal { get; set; }
    public int VacationDaysRemaining => Math.Max(0, VacationPaidDaysTotal - VacationPaidDaysUsed);

    // Sick days (always paid)
    public int SickDaysUsed { get; set; }
    public int SickDaysTotal { get; set; }
    public int SickDaysRemaining => SickDaysTotal - SickDaysUsed;

    // Personal
    public int PersonalDaysUsed { get; set; }
    public int PersonalDaysTotal { get; set; }
    public int PersonalDaysRemaining => PersonalDaysTotal - PersonalDaysUsed;

    // Totals
    public int TotalDaysUsed => VacationDaysUsed + SickDaysUsed + PersonalDaysUsed;
    public int TotalDaysRemaining => VacationDaysRemaining + SickDaysRemaining + PersonalDaysRemaining;
}