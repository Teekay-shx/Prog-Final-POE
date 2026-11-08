namespace RaceDay.Api.DTOs.Users;

/// <summary>A user's own profile — never includes the password hash.</summary>
public class UserProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Public URL of the profile picture stored in Azure Blob Storage, if one was uploaded.</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
