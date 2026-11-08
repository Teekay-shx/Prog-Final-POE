using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Enrolments;

/// <summary>Request body for PUT /api/enrolments/{id}.</summary>
public class UpdateEnrolmentStatusDto
{
    /// <summary>"Registered", "CheckedIn" or "Cancelled".</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}
