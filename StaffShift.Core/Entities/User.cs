using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace StaffShift.Core.Entities;

/// <summary>
/// Represents a staff member in the organization
/// </summary>
public class User : IdentityUser<int>
{
    [NotMapped]
    [MaxLength(50)]
    public string? Username 
    { 
        get => UserName; 
        set => UserName = value; 
    }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    [MaxLength(100)]
    public string? Position { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Manager assigned to this worker (for Workers, this is required)
    /// </summary>
    public int? ManagerId { get; set; }

    [ForeignKey("ManagerId")]
    public virtual User? Manager { get; set; }

    /// <summary>
    /// Workers that this user manages (for Managers)
    /// </summary>
    public virtual ICollection<User> Subordinates { get; set; } = new List<User>();

    // Navigation properties
    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    public virtual ICollection<TimeOffRequest> TimeOffRequests { get; set; } = new List<TimeOffRequest>();
    public virtual ICollection<TimeOffRequest> ApprovedRequests { get; set; } = new List<TimeOffRequest>();
    public virtual ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();
    public virtual ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}