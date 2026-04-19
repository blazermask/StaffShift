using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffShift.Core.Entities;

/// <summary>
/// Represents a work shift for a staff member
/// </summary>
public class Shift
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    public DateTime ShiftDate { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Total hours worked for this shift
    /// </summary>
    public double HoursWorked { get; set; }

    /// <summary>
    /// Actual clock-in time (if different from scheduled)
    /// </summary>
    public TimeSpan? ActualStartTime { get; set; }

    /// <summary>
    /// Actual clock-out time (if different from scheduled)
    /// </summary>
    public TimeSpan? ActualEndTime { get; set; }

    /// <summary>
    /// Shift status: Scheduled, Completed, Missed, Cancelled
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Scheduled";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }
}