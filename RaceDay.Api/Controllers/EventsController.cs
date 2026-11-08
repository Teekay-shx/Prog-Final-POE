using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Categories;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Browsing events (public) and managing them (Organiser, own events only).</summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBlobStorageService _blobStorage;

    public EventsController(RaceDayDbContext context, ICurrentUserService currentUser, IBlobStorageService blobStorage)
    {
        _context = context;
        _currentUser = currentUser;
        _blobStorage = blobStorage;
    }

    /// <summary>Lists all events, optionally filtered by status, type, date or location.</summary>
    /// <response code="200">Matching events.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] string? status, [FromQuery] string? type, [FromQuery] DateOnly? date, [FromQuery] string? location)
    {
        var query = _context.Events.Include(e => e.Organiser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(e => e.Type == type);
        }
        if (date.HasValue)
        {
            query = query.Where(e => e.EventDate == date.Value);
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(e => e.Location != null && e.Location.Contains(location));
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>Lists the logged-in organiser's own events with enrolment and category totals (dashboard data).</summary>
    /// <response code="200">The organiser's events.</response>
    [HttpGet("mine")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<OrganiserEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEvents()
    {
        var organiserId = _currentUser.UserId!.Value;

        var events = await _context.Events
            .Include(e => e.Organiser)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        var enrolmentCounts = await _context.Enrolments
            .Where(en => en.Category!.Event!.OrganiserId == organiserId && en.Status != EnrolmentStatus.Cancelled)
            .GroupBy(en => en.Category!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var categoryCounts = await _context.Categories
            .Where(c => c.Event!.OrganiserId == organiserId)
            .GroupBy(c => c.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var dtos = events.Select(ev =>
        {
            var dto = CopyTo(new OrganiserEventDto(), ev);
            dto.TotalEnrolments = enrolmentCounts.GetValueOrDefault(ev.EventId);
            dto.CategoryCount = categoryCounts.GetValueOrDefault(ev.EventId);
            return dto;
        });

        return Ok(dtos);
    }

    /// <summary>Returns full details for a single event, including its categories.</summary>
    /// <response code="200">The event, with nested categories.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null)
        {
            return NotFound();
        }

        return Ok(ToDetailDto(ev));
    }

    /// <summary>Creates a new event owned by the logged-in organiser.</summary>
    /// <response code="201">Created event.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEvent(CreateEventDto dto)
    {
        if (!Models.EventType.IsValid(dto.Type))
        {
            return BadRequest(new { message = $"Type must be one of: {string.Join(", ", Models.EventType.All)}." });
        }

        var ev = new Event
        {
            OrganiserId = _currentUser.UserId!.Value,
            Name = dto.Name,
            EventDate = dto.EventDate,
            Location = dto.Location,
            Description = dto.Description,
            Distance = dto.Distance,
            Type = dto.Type,
            Status = EventStatus.Planned
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();
        await _context.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, ToDto(ev));
    }

    /// <summary>Updates an event. Only the owning organiser may edit it.</summary>
    /// <response code="200">Updated event.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, UpdateEventDto dto)
    {
        var ev = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
        {
            return NotFound();
        }
        if (ev.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }
        if (!Models.EventType.IsValid(dto.Type))
        {
            return BadRequest(new { message = $"Type must be one of: {string.Join(", ", Models.EventType.All)}." });
        }
        if (!EventStatus.IsValid(dto.Status))
        {
            return BadRequest(new { message = $"Status must be one of: {string.Join(", ", EventStatus.All)}." });
        }

        ev.Name = dto.Name;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Description = dto.Description;
        ev.Distance = dto.Distance;
        ev.Type = dto.Type;
        ev.Status = dto.Status;

        await _context.SaveChangesAsync();

        return Ok(ToDto(ev));
    }

    /// <summary>
    /// Uploads or replaces the event's banner image. The file is stored in Azure Blob Storage
    /// and its public URL is saved on the event. Only the owning organiser may do this.
    /// </summary>
    /// <param name="id">The event id.</param>
    /// <param name="file">The banner image (JPEG, PNG, WebP or GIF, up to 5 MB), sent as multipart/form-data.</param>
    /// <response code="200">The event, with its new BannerImageUrl.</response>
    /// <response code="400">The file is missing, too large or not an image.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="503">Azure Blob Storage has not been configured on the server.</response>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = UserRole.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var ev = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id, cancellationToken);
        if (ev is null)
        {
            return NotFound();
        }
        if (ev.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }

        var error = ImageUploadRules.Validate(file);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            ev.BannerImageUrl = await _blobStorage.UploadImageAsync(stream, file.ContentType, "event-banners", cancellationToken);
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

        return Ok(ToDto(ev));
    }

    /// <summary>Cancels/removes an event. Only the owning organiser may delete it, and only while it has no enrolments.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="409">The event already has enrolments; set its status to Cancelled instead.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev is null)
        {
            return NotFound();
        }
        if (ev.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }

        var hasEnrolments = await _context.Enrolments.AnyAsync(en => en.Category!.EventId == id);
        if (hasEnrolments)
        {
            return Conflict(new { message = "This event already has enrolments and cannot be deleted. Set its status to Cancelled instead." });
        }

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static T CopyTo<T>(T dto, Event ev) where T : EventDto
    {
        dto.EventId = ev.EventId;
        dto.OrganiserId = ev.OrganiserId;
        dto.OrganiserName = ev.Organiser?.FullName ?? string.Empty;
        dto.Name = ev.Name;
        dto.EventDate = ev.EventDate;
        dto.Location = ev.Location;
        dto.Description = ev.Description;
        dto.Distance = ev.Distance;
        dto.Type = ev.Type;
        dto.Status = ev.Status;
        dto.BannerImageUrl = ev.BannerImageUrl;
        dto.CreatedAt = ev.CreatedAt;
        return dto;
    }

    private static EventDto ToDto(Event ev) => CopyTo(new EventDto(), ev);

    private static EventDetailDto ToDetailDto(Event ev)
    {
        var dto = CopyTo(new EventDetailDto(), ev);
        dto.Categories = ev.Categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            MinAge = c.MinAge,
            MaxAge = c.MaxAge,
            EntryFee = c.EntryFee,
            MaxParticipants = c.MaxParticipants
        }).ToList();
        return dto;
    }
}
