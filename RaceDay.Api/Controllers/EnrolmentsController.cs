using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Participants enrolling into categories, and organisers managing their event rosters.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public EnrolmentsController(RaceDayDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>Enrols the logged-in participant into a category.</summary>
    /// <response code="201">Created enrolment, with its assigned bib number.</response>
    /// <response code="404">The category does not exist.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [HttpPost("categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = UserRole.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category is null)
        {
            return NotFound(new { message = "Category does not exist." });
        }

        var participantId = _currentUser.UserId!.Value;

        var alreadyEnrolled = category.Enrolments.Any(e => e.ParticipantId == participantId);
        if (alreadyEnrolled)
        {
            return Conflict(new { message = "You are already enrolled in this category." });
        }

        if (category.MaxParticipants.HasValue && category.Enrolments.Count >= category.MaxParticipants.Value)
        {
            return Conflict(new { message = "This category is full." });
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = categoryId,
            BibNumber = await GenerateBibNumberAsync(category.EventId),
            Status = EnrolmentStatus.Registered
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var saved = await LoadEnrolmentAsync(enrolment.EnrolmentId);
        return CreatedAtAction(nameof(GetMyEnrolments), null, ToDto(saved!));
    }

    /// <summary>Lists the logged-in participant's own enrolments across all events.</summary>
    /// <response code="200">The caller's enrolments.</response>
    [HttpGet("users/me/enrolments")]
    [Authorize(Roles = UserRole.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = _currentUser.UserId!.Value;

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>Lists all participants enrolled in a category. Only the owning organiser may view the roster.</summary>
    /// <response code="200">Enrolments for the category.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">The category does not exist.</response>
    [HttpGet("categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolmentsForCategory(int categoryId)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null)
        {
            return NotFound(new { message = "Category does not exist." });
        }
        if (category.Event!.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Where(e => e.CategoryId == categoryId)
            .OrderBy(e => e.BibNumber)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>
    /// Lists every enrolment for an event across all of its categories, including any result already
    /// captured. Only the owning organiser may view it.
    /// </summary>
    /// <response code="200">Enrolments for the event.</response>
    /// <response code="403">The caller does not own the event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
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

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Result)
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Where(e => e.Category!.EventId == eventId)
            .OrderBy(e => e.Category!.Name)
            .ThenBy(e => e.BibNumber)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>Updates an enrolment's status (e.g. check-in on race day). Only the owning organiser may do this.</summary>
    /// <response code="200">Updated enrolment.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">No enrolment with that id.</response>
    [HttpPut("enrolments/{id:int}")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEnrolmentStatus(int id, UpdateEnrolmentStatusDto dto)
    {
        var enrolment = await LoadEnrolmentAsync(id);
        if (enrolment is null)
        {
            return NotFound();
        }
        if (enrolment.Category!.Event!.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }
        if (!EnrolmentStatus.IsValid(dto.Status))
        {
            return BadRequest(new { message = $"Status must be one of: {string.Join(", ", EnrolmentStatus.All)}." });
        }

        enrolment.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(ToDto(enrolment));
    }

    /// <summary>Cancels an enrolment. Only the owning participant may cancel their own enrolment.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The caller does not own this enrolment.</response>
    /// <response code="404">No enrolment with that id.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = UserRole.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _context.Enrolments.FindAsync(id);
        if (enrolment is null)
        {
            return NotFound();
        }
        if (enrolment.ParticipantId != _currentUser.UserId)
        {
            return Forbid();
        }

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string> GenerateBibNumberAsync(int eventId)
    {
        var countForEvent = await _context.Enrolments.CountAsync(e => e.Category!.EventId == eventId);
        return $"E{eventId}-{countForEvent + 1:D4}";
    }

    private Task<Enrolment?> LoadEnrolmentAsync(int id) =>
        _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

    private static EnrolmentDto ToDto(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant?.FullName ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        EventId = e.Category?.EventId ?? 0,
        EventName = e.Category?.Event?.Name ?? string.Empty,
        BibNumber = e.BibNumber,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status,
        ResultId = e.Result?.ResultId,
        FinishTime = e.Result?.FinishTime,
        Position = e.Result?.Position,
        ResultStatus = e.Result?.Status
    };
}
