namespace RaceDay.Api.DTOs.Events;

/// <summary>GET /api/events/mine — an organiser's own event plus the numbers shown on their dashboard.</summary>
public class OrganiserEventDto : EventDto
{
    /// <summary>Enrolments for the event, not counting cancelled ones.</summary>
    public int TotalEnrolments { get; set; }

    public int CategoryCount { get; set; }
}
