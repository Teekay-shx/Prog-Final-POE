using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Results;

/// <summary>Request body for POST /api/enrolments/{enrolmentId}/results.</summary>
public class CreateResultDto
{
    public TimeOnly? FinishTime { get; set; }

    public int? Position { get; set; }

    /// <summary>"Finished", "DNF" or "DSQ". Defaults to "Finished".</summary>
    public string Status { get; set; } = "Finished";
}
