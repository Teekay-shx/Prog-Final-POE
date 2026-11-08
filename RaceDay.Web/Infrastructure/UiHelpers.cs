using System.Globalization;

namespace RaceDay.Web.Infrastructure;

/// <summary>Small presentation helpers shared by the views (formatting and colour-coded statuses).</summary>
public static class UiHelpers
{
    public static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };
    public static readonly string[] EventStatuses = { "Planned", "Open", "Closed", "Completed", "Cancelled" };
    public static readonly string[] ResultStatuses = { "Finished", "DNF", "DSQ" };

    /// <summary>47 → "47th", 22 → "22nd", 11 → "11th".</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return $"{number}th";
        }

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    public static string TypeIcon(string type) => type switch
    {
        "Run" => "🏃",
        "Walk" => "🚶",
        "Cycle" => "🚴",
        _ => "🏁"
    };

    /// <summary>Enrolment status colours: amber = waiting, green = checked in, red = cancelled.</summary>
    public static (string Css, string Label) EnrolmentBadge(string status) => status switch
    {
        "Registered" => ("rd-badge-amber", "Registered"),
        "CheckedIn" => ("rd-badge-green", "Checked in"),
        "Cancelled" => ("rd-badge-red", "Cancelled"),
        _ => ("rd-badge-grey", status)
    };

    public static (string Css, string Label) EventBadge(string status) => status switch
    {
        "Open" => ("rd-badge-green", "Open"),
        "Planned" => ("rd-badge-amber", "Planned"),
        "Closed" => ("rd-badge-grey", "Closed"),
        "Completed" => ("rd-badge-blue", "Completed"),
        "Cancelled" => ("rd-badge-red", "Cancelled"),
        _ => ("rd-badge-grey", status)
    };

    public static (string Css, string Label) ResultBadge(string status) => status switch
    {
        "Finished" => ("rd-badge-green", "Finished"),
        "DNF" => ("rd-badge-amber", "Did not finish"),
        "DSQ" => ("rd-badge-red", "Disqualified"),
        _ => ("rd-badge-grey", status)
    };

    public static string Money(decimal amount) => "R" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string FinishTime(TimeOnly? time) => time?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "—";

    public static string AgeRange(int? min, int? max) => (min, max) switch
    {
        (null, null) => "All ages",
        (int lo, null) => $"{lo}+ years",
        (null, int hi) => $"Up to {hi} years",
        (int lo, int hi) => $"{lo}–{hi} years"
    };

    public static string LongDate(DateOnly date) => date.ToString("ddd, d MMM yyyy", CultureInfo.InvariantCulture);
}
