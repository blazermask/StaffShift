using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StaffShift.Core.Entities;

/// <summary>
/// Represents a comment on a forum post
/// </summary>
public class ForumComment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ForumPostId { get; set; }

    [ForeignKey("ForumPostId")]
    public virtual ForumPost ForumPost { get; set; } = null!;

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}