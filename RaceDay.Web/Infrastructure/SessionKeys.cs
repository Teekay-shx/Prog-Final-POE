namespace RaceDay.Web.Infrastructure;

/// <summary>Keys for the values the web app keeps in the server-side session.</summary>
public static class SessionKeys
{
    /// <summary>The API's own session cookie value, replayed on every call the web app makes for the user.</summary>
    public const string ApiCookie = "ApiSessionCookie";

    public const string Role = "Role";
    public const string FullName = "FullName";
}

public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
