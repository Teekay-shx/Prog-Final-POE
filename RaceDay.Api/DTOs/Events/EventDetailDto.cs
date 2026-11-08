using RaceDay.Api.DTOs.Categories;

namespace RaceDay.Api.DTOs.Events;

/// <summary>GET /api/events/{id} — an event plus its categories.</summary>
public class EventDetailDto : EventDto
{
    public List<CategoryDto> Categories { get; set; } = new();
}
