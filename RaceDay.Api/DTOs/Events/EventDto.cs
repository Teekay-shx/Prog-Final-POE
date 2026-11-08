namespace RaceDay.Api.DTOs.Events;

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public decimal? Distance { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>Public URL of the event banner stored in Azure Blob Storage, if one was uploaded.</summary>
    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
