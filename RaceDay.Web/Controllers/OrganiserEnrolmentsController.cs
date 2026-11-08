using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Everyone who has enrolled for one of the organiser's events, with category and status.</summary>
[Authorize(Roles = AppRoles.Organiser)]
[Route("organiser/events/{eventId:int}/enrolments")]
public class OrganiserEnrolmentsController : Controller
{
    private readonly IRaceDayApi _api;

    public OrganiserEnrolmentsController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId, int? categoryId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (ev.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        if (categoryId.HasValue)
        {
            enrolments = enrolments.Where(e => e.CategoryId == categoryId.Value).ToList();
        }

        return View(new EnrolmentsPageViewModel { Event = ev, Enrolments = enrolments, CategoryId = categoryId });
    }

    /// <summary>Check a participant in on race day (or cancel their enrolment).</summary>
    [HttpPost("{enrolmentId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int eventId, int enrolmentId, string status)
    {
        try
        {
            await _api.UpdateEnrolmentStatusAsync(enrolmentId, status);
            TempData["Success"] = "Enrolment status updated.";
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
