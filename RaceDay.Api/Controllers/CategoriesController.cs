using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Categories;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Categories (distances/age groups) within an event.</summary>
[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CategoriesController(RaceDayDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>Lists all categories belonging to an event.</summary>
    /// <response code="200">Categories for the event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = "Event does not exist." });
        }

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories.Select(ToDto));
    }

    /// <summary>Adds a new category to an event. Only the owning organiser may add categories.</summary>
    /// <response code="201">Created category.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, CreateCategoryDto dto)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev is null)
        {
            return NotFound(new { message = "Event does not exist." });
        }
        if (ev.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }

        var category = new Category
        {
            EventId = eventId,
            Name = dto.Name,
            DistanceKm = dto.DistanceKm,
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategoriesForEvent), new { eventId }, ToDto(category));
    }

    /// <summary>Updates a category's details. Only the owning organiser may edit it.</summary>
    /// <response code="200">Updated category.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">No category with that id.</response>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return NotFound();
        }
        if (category.Event!.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }

        category.Name = dto.Name;
        category.DistanceKm = dto.DistanceKm;
        category.MinAge = dto.MinAge;
        category.MaxAge = dto.MaxAge;
        category.EntryFee = dto.EntryFee;
        category.MaxParticipants = dto.MaxParticipants;

        await _context.SaveChangesAsync();

        return Ok(ToDto(category));
    }

    /// <summary>Removes a category, but only if it has no enrolments. Only the owning organiser may delete it.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">The category has active enrolments and cannot be removed.</response>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
        {
            return NotFound();
        }
        if (category.Event!.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }
        if (category.Enrolments.Any())
        {
            return Conflict(new { message = "This category has active enrolments and cannot be removed." });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };
}
