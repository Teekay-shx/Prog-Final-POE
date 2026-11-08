using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Payments;

/// <summary>Request body for POST /api/enrolments/{enrolmentId}/payments.</summary>
public class CreatePaymentDto
{
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(30)]
    public string? PaymentMethod { get; set; }
}
