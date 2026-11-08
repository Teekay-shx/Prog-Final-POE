namespace RaceDay.Api.Services;

/// <summary>Reads the current request's authenticated user from the session cookie's claims.</summary>
public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Role { get; }
    bool IsInRole(string role);
}
