using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Users;

/// <summary>Request body for PUT /api/users/me.</summary>
public class UpdateProfileDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}
