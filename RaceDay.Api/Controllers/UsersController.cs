using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Users;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Viewing and editing a user's own profile, plus Admin-only account management.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBlobStorageService _blobStorage;

    public UsersController(RaceDayDbContext context, ICurrentUserService currentUser, IBlobStorageService blobStorage)
    {
        _context = context;
        _currentUser = currentUser;
        _blobStorage = blobStorage;
    }

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    /// <response code="200">The caller's own profile.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _context.Users.FindAsync(_currentUser.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(ToDto(user));
    }

    /// <summary>Updates the logged-in user's own profile details.</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Validation error.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(_currentUser.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        user.FullName = dto.FullName;
        user.Phone = dto.Phone;
        user.DateOfBirth = dto.DateOfBirth;

        await _context.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    /// <summary>
    /// Uploads or replaces the logged-in user's profile picture. The file is stored in Azure
    /// Blob Storage and its public URL is saved on the user's profile.
    /// </summary>
    /// <param name="file">The picture (JPEG, PNG, WebP or GIF, up to 5 MB), sent as multipart/form-data.</param>
    /// <response code="200">The updated profile, with its new ProfilePictureUrl.</response>
    /// <response code="400">The file is missing, too large or not an image.</response>
    /// <response code="503">Azure Blob Storage has not been configured on the server.</response>
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object?[] { _currentUser.UserId }, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var error = ImageUploadRules.Validate(file);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            user.ProfilePictureUrl = await _blobStorage.UploadImageAsync(stream, file.ContentType, "profile-pictures", cancellationToken);
        }
        catch (BlobStorageNotConfiguredException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured on the server." });
        }
        catch (Azure.RequestFailedException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "The image could not be stored. Please try again." });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(user));
    }

    /// <summary>Returns a specific user's profile. Admin only.</summary>
    /// <response code="200">The requested user.</response>
    /// <response code="404">No user with that id.</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(ToDto(user));
    }

    /// <summary>Lists all users, for Admin account management.</summary>
    /// <response code="200">All users.</response>
    [HttpGet]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(IEnumerable<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? role)
    {
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var users = await query.OrderBy(u => u.FullName).ToListAsync();
        return Ok(users.Select(ToDto));
    }

    /// <summary>Deletes a user account. Admin only.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No user with that id.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static UserProfileDto ToDto(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        Phone = user.Phone,
        DateOfBirth = user.DateOfBirth,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt = user.CreatedAt
    };
}
