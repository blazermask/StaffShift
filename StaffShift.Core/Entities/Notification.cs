using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffShift.Core.Entities;

/// <summary>
/// Represents a notification for a user
/// </summary>
public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type: Info, Warning, Success, TimeOff, Shift
    /// </summary>
    [MaxLength(20)]
    public string Type { get; set; } = "Info";

    /// <summary>
    /// Related entity type (e.g., TimeOffRequest, Shift)
    /// </summary>
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }

    public int? RelatedEntityId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}