namespace RaceDay.Api.DTOs.Payments;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int EnrolmentId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
