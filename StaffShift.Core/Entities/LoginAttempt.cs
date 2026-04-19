using System.ComponentModel.DataAnnotations;

namespace StaffShift.Core.Entities;

/// <summary>
/// Tracks login attempts for security and rate limiting
/// </summary>
public class LoginAttempt
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AttemptedUsername { get; set; }

    public bool WasSuccessful { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}