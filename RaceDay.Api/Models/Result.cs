using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// The finishing outcome for one Enrolment. One-to-one (optional) with Enrolment —
/// a result only exists once the participant has finished (or not finished / been
/// disqualified from) the race.
/// </summary>
public class Result
{
    [Key]
    public int ResultId { get; set; }

    [Required]
    [ForeignKey(nameof(Enrolment))]
    public int EnrolmentId { get; set; }

    public TimeOnly? FinishTime { get; set; }

    public int? Position { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = ResultStatus.Finished;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Enrolment? Enrolment { get; set; }
}
