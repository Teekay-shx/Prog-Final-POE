using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Public event pages. Anyone can read an event; only logged-in participants can enrol.</summary>
public class EventsController : Controller
{
    private readonly IRaceDayApi _api;

    public EventsController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);

        var enrolledCategoryIds = new HashSet<int>();
        var isParticipant = User.IsInRole(AppRoles.Participant);

        if (isParticipant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            foreach (var enrolment in mine.Where(e => e.EventId == id))
            {
                enrolledCategoryIds.Add(enrolment.CategoryId);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var enrolmentsOpen = ev.EventDate >= today && (ev.Status == "Open" || ev.Status == "Planned");

        return View(new EventDetailsViewModel
        {
            Event = ev,
            EnrolledCategoryIds = enrolledCategoryIds,
            CanEnrol = isParticipant && enrolmentsOpen
        });
    }
}
