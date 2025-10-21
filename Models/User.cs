using System.ComponentModel.DataAnnotations;

namespace MediConnectAPI.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Email { get; set; } = null!;

    [MaxLength(30)]
    public string? Phone { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string Password { get; set; } = null!;

    // Role relationship
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // Navigation
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
}
