using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Results;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Race results: public leaderboards, a participant's own history, and organiser capture/correction.</summary>
[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ResultsController(RaceDayDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>Returns the results leaderboard for a category, ordered by finishing position.</summary>
    /// <response code="200">Results for the category.</response>
    /// <response code="404">The category does not exist.</response>
    [HttpGet("categories/{categoryId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResultsForCategory(int categoryId)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
        if (!categoryExists)
        {
            return NotFound(new { message = "Category does not exist." });
        }

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
            .Where(r => r.Enrolment!.CategoryId == categoryId)
            .OrderBy(r => r.Position ?? int.MaxValue)
            .ToListAsync();

        var finishers = results.Count(r => r.Status == ResultStatus.Finished);
        return Ok(results.Select(r => ToDto(r, finishers)));
    }

    /// <summary>
    /// Returns the logged-in participant's own race results, with the event, category and
    /// the number of finishers in that category (so a position reads "47th out of 312").
    /// </summary>
    /// <response code="200">The caller's results.</response>
    [HttpGet("users/me/results")]
    [Authorize(Roles = UserRole.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = _currentUser.UserId!.Value;

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync();

        var categoryIds = results.Select(r => r.Enrolment!.CategoryId).Distinct().ToList();

        var finisherCounts = await _context.Results
            .Where(r => r.Status == ResultStatus.Finished && categoryIds.Contains(r.Enrolment!.CategoryId))
            .GroupBy(r => r.Enrolment!.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return Ok(results.Select(r => ToDto(r, finisherCounts.GetValueOrDefault(r.Enrolment!.CategoryId))));
    }

    /// <summary>Records a finishing result for a participant's enrolment. Only the owning organiser may capture results.</summary>
    /// <response code="201">Created result.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">The enrolment does not exist.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpPost("enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateResult(int enrolmentId, CreateResultDto dto)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null)
        {
            return NotFound(new { message = "Enrolment does not exist." });
        }
        if (enrolment.Category!.Event!.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }
        if (enrolment.Result is not null)
        {
            return Conflict(new { message = "A result has already been recorded for this enrolment. Use PUT to correct it." });
        }
        if (!ResultStatus.IsValid(dto.Status))
        {
            return BadRequest(new { message = $"Status must be one of: {string.Join(", ", ResultStatus.All)}." });
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position,
            Status = dto.Status
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        result.Enrolment = enrolment;
        var finishers = await CountFinishersAsync(enrolment.CategoryId);
        return CreatedAtAction(nameof(GetResultsForCategory), new { categoryId = enrolment.CategoryId }, ToDto(result, finishers));
    }

    /// <summary>Corrects a previously recorded result. Only the owning organiser may update it.</summary>
    /// <response code="200">Updated result.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">No result with that id.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = UserRole.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, UpdateResultDto dto)
    {
        var result = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null)
        {
            return NotFound();
        }
        if (result.Enrolment!.Category!.Event!.OrganiserId != _currentUser.UserId)
        {
            return Forbid();
        }
        if (!ResultStatus.IsValid(dto.Status))
        {
            return BadRequest(new { message = $"Status must be one of: {string.Join(", ", ResultStatus.All)}." });
        }

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        result.Status = dto.Status;

        await _context.SaveChangesAsync();

        var finishers = await CountFinishersAsync(result.Enrolment.CategoryId);
        return Ok(ToDto(result, finishers));
    }

    private Task<int> CountFinishersAsync(int categoryId) =>
        _context.Results.CountAsync(r => r.Enrolment!.CategoryId == categoryId && r.Status == ResultStatus.Finished);

    private static ResultDto ToDto(Result r, int totalFinishers) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment?.ParticipantId ?? 0,
        ParticipantName = r.Enrolment?.Participant?.FullName ?? string.Empty,
        BibNumber = r.Enrolment?.BibNumber,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status,
        RecordedAt = r.RecordedAt,
        EventId = r.Enrolment?.Category?.EventId ?? 0,
        EventName = r.Enrolment?.Category?.Event?.Name ?? string.Empty,
        EventDate = r.Enrolment?.Category?.Event?.EventDate,
        CategoryId = r.Enrolment?.CategoryId ?? 0,
        CategoryName = r.Enrolment?.Category?.Name ?? string.Empty,
        TotalFinishers = totalFinishers
    };
}
