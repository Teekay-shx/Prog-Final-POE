using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Capture finish times and finishing positions. A saved result is published to the participant immediately.</summary>
[Authorize(Roles = AppRoles.Organiser)]
[Route("organiser/events/{eventId:int}/results")]
public class OrganiserResultsController : Controller
{
    private readonly IRaceDayApi _api;

    public OrganiserResultsController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (ev.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);

        return View(new ResultsPageViewModel
        {
            Event = ev,
            Enrolments = enrolments
                .Where(e => e.Status != "Cancelled")
                .OrderBy(e => e.CategoryName)
                .ThenBy(e => e.Position ?? int.MaxValue)
                .ThenBy(e => e.BibNumber)
                .ToList()
        });
    }

    [HttpPost("")]
    public async Task<IActionResult> Save(int eventId, ResultInputModel input)
    {
        if (!UiHelpers.ResultStatuses.Contains(input.Status))
        {
            TempData["Error"] = "Please choose a valid result status.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (input.Position is < 1)
        {
            TempData["Error"] = "A finishing position must be 1 or higher.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (input.Status == "Finished" && input.FinishTime is null && input.Position is null)
        {
            TempData["Error"] = "Enter a finish time or a finishing position (or mark the participant as DNF or DSQ).";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var request = new ResultRequest
        {
            FinishTime = input.Status == "Finished" ? input.FinishTime : null,
            Position = input.Status == "Finished" ? input.Position : null,
            Status = input.Status
        };

        try
        {
            if (input.ResultId.HasValue)
            {
                await _api.UpdateResultAsync(input.ResultId.Value, request);
            }
            else
            {
                await _api.CreateResultAsync(input.EnrolmentId, request);
            }

            TempData["Success"] = "Result saved and published.";
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
