using System.ComponentModel.DataAnnotations;

namespace StaffShift.Core.DTOs;

/// <summary>
/// Data transfer object for forum post information
/// </summary>
public class ForumPostDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AuthorId => UserId;
    public string? UserName { get; set; }
    public string? AuthorName => UserName;
    public string? UserDepartment { get; set; }
    public string? Department => TargetDepartment ?? UserDepartment;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "Discussion";
    public string? TargetDepartment { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsCurrentUser { get; set; }
    public int CommentCount { get; set; }
}

/// <summary>
/// Data transfer object for creating a forum post
/// </summary>
public class CreateForumPostDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    [StringLength(10000, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 10000 characters")]
    public string Content { get; set; } = string.Empty;

    [StringLength(30)]
    public string Category { get; set; } = "Discussion";

    [StringLength(100)]
    public string? TargetDepartment { get; set; }
    
    public string? Department { get; set; }
}

/// <summary>
/// Data transfer object for updating a forum post
/// </summary>
public class UpdateForumPostDto
{
    [Required]
    public int Id { get; set; }

    [StringLength(200, MinimumLength = 5)]
    public string? Title { get; set; }

    [StringLength(10000, MinimumLength = 10)]
    public string? Content { get; set; }

    [StringLength(30)]
    public string? Category { get; set; }

    [StringLength(100)]
    public string? TargetDepartment { get; set; }
    
    public string? Department { get; set; }
}

/// <summary>
/// Data transfer object for forum comment information
/// </summary>
public class ForumCommentDto
{
    public int Id { get; set; }
    public int ForumPostId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? AuthorName => UserName;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsCurrentUser { get; set; }
}

/// <summary>
/// Data transfer object for creating a forum comment
/// </summary>
public class CreateForumCommentDto
{
    [Required]
    public int ForumPostId { get; set; }
    
    public int PostId { get; set; }

    [Required(ErrorMessage = "Comment content is required")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 2000 characters")]
    public string Content { get; set; } = string.Empty;
}