using System.ComponentModel.DataAnnotations;

namespace StaffShift.Core.Entities;

/// <summary>
/// Tracks registration attempts for rate limiting
/// </summary>
public class RegistrationRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}