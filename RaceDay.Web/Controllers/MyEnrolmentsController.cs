using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>A participant's own enrolments: enrol in a category, see statuses, cancel.</summary>
[Authorize(Roles = AppRoles.Participant)]
public class MyEnrolmentsController : Controller
{
    private readonly IRaceDayApi _api;

    public MyEnrolmentsController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var enrolments = await _api.GetMyEnrolmentsAsync();
        return View(enrolments.OrderByDescending(e => e.EnrolmentDate).ToList());
    }

    /// <summary>Records the enrolment immediately; the API assigns the bib number and enforces capacity and duplicates.</summary>
    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        try
        {
            var enrolment = await _api.EnrolAsync(categoryId);
            TempData["Success"] = $"You're in! Your bib number for {enrolment.EventName} ({enrolment.CategoryName}) is {enrolment.BibNumber}.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Events", new { id = eventId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        await _api.CancelEnrolmentAsync(id);
        TempData["Success"] = "Your enrolment has been cancelled.";
        return RedirectToAction(nameof(Index));
    }
}
