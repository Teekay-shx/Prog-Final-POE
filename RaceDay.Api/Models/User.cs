using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// A registered account. Covers Admins, Organisers and Participants in a single table,
/// distinguished by <see cref="Role"/>, matching the Part 1 ERD.
/// </summary>
public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Public URL of the profile picture in Azure Blob Storage. Added in Part 3.</summary>
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
