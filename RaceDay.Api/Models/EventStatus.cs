namespace RaceDay.Api.Models;

/// <summary>
/// The fixed set of event statuses. Mirrors the CHECK constraint on Events.Status
/// in the Part 1 SQL script.
/// </summary>
public static class EventStatus
{
    public const string Planned = "Planned";
    public const string Open = "Open";
    public const string Closed = "Closed";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Planned, Open, Closed, Completed, Cancelled };

    public static bool IsValid(string status) => All.Contains(status);
}
