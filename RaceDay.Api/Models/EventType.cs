namespace RaceDay.Api.Models;

/// <summary>
/// The fixed set of event types. Added in Part 2 to satisfy the functional requirement
/// that every event capture "a name, description, date, location, distance, and event
/// type (run, walk, or cycle)" — see the Part 2 README for why this extends the Part 1 ERD.
/// </summary>
public static class EventType
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = { Run, Walk, Cycle };

    public static bool IsValid(string type) => All.Contains(type);
}
