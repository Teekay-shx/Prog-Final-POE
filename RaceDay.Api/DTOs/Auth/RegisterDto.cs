using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Auth;

/// <summary>Request body for POST /api/auth/register.</summary>
public class RegisterDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>"Organiser" or "Participant". Admin accounts cannot self-register.</summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}
