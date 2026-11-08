namespace RaceDay.Api.DTOs.Enrolments;

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Filled in only by GET /api/events/{eventId}/enrolments, so the organiser's results
    // page can show any result already captured for each participant.
    public int? ResultId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? ResultStatus { get; set; }
}
