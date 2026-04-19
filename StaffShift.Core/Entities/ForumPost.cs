using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffShift.Core.Entities;

/// <summary>
/// Represents a forum post for work information exchange
/// </summary>
public class ForumPost
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
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Category: Announcement, Question, Discussion, Information
    /// </summary>
    [MaxLength(30)]
    public string Category { get; set; } = "Discussion";

    /// <summary>
    /// Department this post is relevant to (null = all departments)
    /// </summary>
    [MaxLength(100)]
    public string? TargetDepartment { get; set; }

    /// <summary>
    /// Whether this post is pinned by a manager/admin
    /// </summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Whether this post is locked (no more comments)
    /// </summary>
    public bool IsLocked { get; set; } = false;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int? LastEditedBy { get; set; }

    [ForeignKey("LastEditedBy")]
    public virtual User? LastEditedByUser { get; set; }

    // Navigation properties
    public virtual ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();
}