using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// Links a Participant to the Category they have entered. The many-to-many resolver
/// between Users and Categories.
/// </summary>
public class Enrolment
{
    [Key]
    public int EnrolmentId { get; set; }

    [Required]
    [ForeignKey(nameof(Participant))]
    public int ParticipantId { get; set; }

    [Required]
    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }

    [MaxLength(10)]
    public string? BibNumber { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string Status { get; set; } = EnrolmentStatus.Registered;

    // Navigation
    public User? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
    public Payment? Payment { get; set; }
}
