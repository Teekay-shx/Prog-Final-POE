namespace RaceDay.Api.DTOs.Results;

/// <summary>Request body for PUT /api/results/{id}.</summary>
public class UpdateResultDto
{
    public TimeOnly? FinishTime { get; set; }

    public int? Position { get; set; }

    /// <summary>"Finished", "DNF" or "DSQ".</summary>
    public string Status { get; set; } = "Finished";
}
