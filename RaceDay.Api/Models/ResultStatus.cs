namespace RaceDay.Api.Models;

/// <summary>
/// The fixed set of result statuses. Mirrors the CHECK constraint on Results.Status
/// in the Part 1 SQL script.
/// </summary>
public static class ResultStatus
{
    public const string Finished = "Finished";
    public const string DNF = "DNF";
    public const string DSQ = "DSQ";

    public static readonly string[] All = { Finished, DNF, DSQ };

    public static bool IsValid(string status) => All.Contains(status);
}
