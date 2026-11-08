using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// A race meeting created and owned by an Organiser.
/// </summary>
/// <remarks>
/// <see cref="Distance"/> and <see cref="Type"/> are additions beyond the Part 1 ERD,
/// added to satisfy Part 2's explicit functional requirement that every event capture
/// "a name, description, date, location, distance, and event type (run, walk, or
/// cycle)". This is a deliberate, documented deviation — see the Part 2 README.
/// </remarks>
public class Event
{
    [Key]
    public int EventId { get; set; }

    [Required]
    [ForeignKey(nameof(Organiser))]
    public int OrganiserId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public string? Description { get; set; }

    /// <summary>Overall event distance in kilometres (e.g. the headline distance advertised for the event).</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Distance { get; set; }

    /// <summary>One of <see cref="Models.EventType"/>.</summary>
    [Required, MaxLength(20)]
    public string Type { get; set; } = EventType.Run;

    /// <summary>One of <see cref="Models.EventStatus"/>.</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = EventStatus.Planned;

    /// <summary>Public URL of the banner image in Azure Blob Storage. Added in Part 3.</summary>
    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
