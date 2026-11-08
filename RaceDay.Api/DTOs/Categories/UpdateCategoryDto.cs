using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Categories;

/// <summary>Request body for PUT /api/categories/{id}.</summary>
public class UpdateCategoryDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public decimal? DistanceKm { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EntryFee { get; set; }

    public int? MaxParticipants { get; set; }
}
