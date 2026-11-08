namespace RaceDay.Api.DTOs.Results;

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }

    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly? EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>How many participants finished in this category, so a position can read "47th out of 312".</summary>
    public int TotalFinishers { get; set; }
}
