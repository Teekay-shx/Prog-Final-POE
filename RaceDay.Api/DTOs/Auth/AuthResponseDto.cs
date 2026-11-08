namespace RaceDay.Api.DTOs.Auth;

/// <summary>Returned by register/login once a session cookie has been issued.</summary>
public class AuthResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
