using System.ComponentModel.DataAnnotations;

namespace StaffShift.Core.DTOs;

/// <summary>
/// Data transfer object for shift information
/// </summary>
public class ShiftDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserDepartment { get; set; }
    public DateTime ShiftDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public double HoursWorked { get; set; }
    public TimeSpan? ActualStartTime { get; set; }
    public TimeSpan? ActualEndTime { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCurrentUser { get; set; }
}

/// <summary>
/// Data transfer object for creating a shift
/// </summary>
public class CreateShiftDto
{
    [Required(ErrorMessage = "User is required")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Shift date is required")]
    [DataType(DataType.Date)]
    public DateTime ShiftDate { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public TimeSpan EndTime { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Data transfer object for updating a shift
/// </summary>
public class UpdateShiftDto
{
    [Required]
    public int Id { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ShiftDate { get; set; }

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public TimeSpan? ActualStartTime { get; set; }

    public TimeSpan? ActualEndTime { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Data transfer object for clock in/out
/// </summary>
public class ClockDto
{
    [Required]
    public int ShiftId { get; set; }

    public TimeSpan? ActualTime { get; set; }
}