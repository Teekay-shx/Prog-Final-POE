using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Categories;

/// <summary>Request body for POST /api/events/{eventId}/categories.</summary>
public class CreateCategoryDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public decimal? DistanceKm { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EntryFee { get; set; } = 0;

    public int? MaxParticipants { get; set; }
}
