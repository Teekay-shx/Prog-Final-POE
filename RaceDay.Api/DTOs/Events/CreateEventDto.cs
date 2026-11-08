using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Events;

/// <summary>Request body for POST /api/events.</summary>
public class CreateEventDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly EventDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public string? Description { get; set; }

    public decimal? Distance { get; set; }

    /// <summary>"Run", "Walk" or "Cycle".</summary>
    [Required]
    public string Type { get; set; } = string.Empty;
}
