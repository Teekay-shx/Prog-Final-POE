using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The organiser's landing page: their events, enrolments per event and upcoming dates.</summary>
[Authorize(Roles = AppRoles.Organiser)]
public class DashboardController : Controller
{
    private readonly IRaceDayApi _api;

    public DashboardController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var events = await _api.GetMyEventsAsync();
        return View(new DashboardViewModel { Events = events.OrderBy(e => e.EventDate).ToList() });
    }
}
