using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Starts and ends the signed-in state of a user in the web app.
///
/// Two things are kept together on login: an authentication cookie (so ASP.NET Core can apply
/// [Authorize(Roles = ...)] to every page) and server-side session data holding the API's own
/// session cookie, which the API client replays on every call made on the user's behalf.
/// </summary>
public static class UserSession
{
    public static async Task SignInAsync(HttpContext http, ApiUser user, string? apiCookie = null)
    {
        if (apiCookie is not null)
        {
            http.Session.SetString(SessionKeys.ApiCookie, apiCookie);
        }
        http.Session.SetString(SessionKeys.Role, user.Role);
        http.Session.SetString(SessionKeys.FullName, user.FullName);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    public static async Task SignOutAsync(HttpContext http)
    {
        http.Session.Clear();
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}
