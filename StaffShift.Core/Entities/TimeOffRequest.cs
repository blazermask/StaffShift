using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffShift.Core.Entities;

/// <summary>
/// Represents a time-off request (vacation, sick day, personal day)
/// </summary>
public class TimeOffRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Type of request: Vacation, Sick, SickPaid, SickUnpaid, Personal, Other
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string RequestType { get; set; } = "Vacation";

    /// <summary>
    /// Whether the sick day is paid (true) or unpaid (false). Only applies to Sick type requests.
    /// </summary>
    public bool IsPaid { get; set; } = true;

    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Status: Pending, Approved, Rejected, Cancelled
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Manager who approved/rejected the request
    /// </summary>
    public int? ReviewedBy { get; set; }

    [ForeignKey("ReviewedBy")]
    public virtual User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Number of days requested
    /// </summary>
    public int DaysRequested => (int)(EndDate - StartDate).TotalDays + 1;
}