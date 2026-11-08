using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Auth;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and logout. Issues the session cookie every other endpoint relies on.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IPasswordHasher<User> _hasher;

    public AuthController(RaceDayDbContext context, IPasswordHasher<User> hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    /// <summary>Creates a new Organiser or Participant account.</summary>
    /// <response code="201">Account created and the user is signed in.</response>
    /// <response code="400">Validation failed, or the chosen role isn't allowed at registration.</response>
    /// <response code="409">That email is already registered.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        // Only Organiser and Participant can self-register; Admin accounts are
        // provisioned directly in the database, never through the public API.
        if (dto.Role != UserRole.Organiser && dto.Role != UserRole.Participant)
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        var emailTaken = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailTaken)
        {
            return Conflict(new { message = "That email is already registered." });
        }

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Role = dto.Role,
            Phone = dto.Phone,
            DateOfBirth = dto.DateOfBirth
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SignInAsync(user);

        var response = new AuthResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
        return CreatedAtAction(nameof(Register), response);
    }

    /// <summary>Authenticates a user and issues the session cookie.</summary>
    /// <response code="200">Login succeeded; the session cookie is set on the response.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        await SignInAsync(user);

        return Ok(new AuthResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    /// <summary>Ends the current session by clearing the authentication cookie.</summary>
    /// <response code="200">Logged out.</response>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out." });
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
