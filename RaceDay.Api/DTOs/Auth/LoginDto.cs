using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Auth;

/// <summary>Request body for POST /api/auth/login.</summary>
public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
