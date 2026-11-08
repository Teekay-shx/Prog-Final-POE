namespace RaceDay.Api.Models;

/// <summary>
/// The fixed set of payment statuses. Mirrors the CHECK constraint on Payments.Status
/// in the Part 1 SQL script.
/// </summary>
public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Refunded = "Refunded";
    public const string Failed = "Failed";

    public static readonly string[] All = { Pending, Completed, Refunded, Failed };

    public static bool IsValid(string status) => All.Contains(status);
}
