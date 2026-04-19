using System.ComponentModel.DataAnnotations;

namespace StaffShift.Core.DTOs;

/// <summary>
/// Data transfer object for user information
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int SubordinateCount { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsCEO { get; set; }
    public bool IsManager { get; set; }
    public bool IsWorker { get; set; }
    public int PendingTimeOffRequests { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// Data transfer object for user profile update
/// </summary>
public class UpdateProfileDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? EmployeeId { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? Position { get; set; }

    public string? ProfileImageUrl { get; set; }

    public int? ManagerId { get; set; }
}

/// <summary>
/// Data transfer object for assigning a manager to a worker
/// </summary>
public class AssignManagerDto
{
    [Required]
    public int WorkerId { get; set; }
    
    public int UserId { get; set; }

    [Required]
    public int ManagerId { get; set; }
}

/// <summary>
/// Data transfer object for user activation/deactivation
/// </summary>
public class ToggleUserStatusDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public bool IsActive { get; set; }
}