namespace RaceDay.Api.Models;

/// <summary>
/// The fixed set of roles a User can hold. Mirrors the CHECK constraint on Users.Role
/// in the Part 1 SQL script.
/// </summary>
public static class UserRole
{
    public const string Admin = "Admin";
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] All = { Admin, Organiser, Participant };

    public static bool IsValid(string role) => All.Contains(role);
}
