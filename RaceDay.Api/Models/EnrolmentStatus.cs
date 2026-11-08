namespace RaceDay.Api.Models;

/// <summary>
/// The fixed set of enrolment statuses. Mirrors the CHECK constraint on Enrolments.Status
/// in the Part 1 SQL script.
/// </summary>
public static class EnrolmentStatus
{
    public const string Registered = "Registered";
    public const string CheckedIn = "CheckedIn";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Registered, CheckedIn, Cancelled };

    public static bool IsValid(string status) => All.Contains(status);
}
