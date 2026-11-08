using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// The entry-fee payment tied to one Enrolment. One-to-one (optional) with Enrolment —
/// kept separate so payment status can change independently (Pending → Completed →
/// Refunded) without touching the enrolment record itself.
/// </summary>
public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    [ForeignKey(nameof(Enrolment))]
    public int EnrolmentId { get; set; }

    [Required]
    [Column(TypeName = "decimal(8,2)")]
    public decimal Amount { get; set; }

    [MaxLength(30)]
    public string? PaymentMethod { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string Status { get; set; } = PaymentStatus.Pending;

    // Navigation
    public Enrolment? Enrolment { get; set; }
}
