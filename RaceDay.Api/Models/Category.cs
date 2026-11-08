using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// A specific age or distance category within an event (e.g. "10km", "Under 20").
/// </summary>
public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    [ForeignKey(nameof(Event))]
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? DistanceKm { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal EntryFee { get; set; } = 0;

    public int? MaxParticipants { get; set; }

    // Navigation
    public Event? Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
