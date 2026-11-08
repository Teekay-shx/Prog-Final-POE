using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>A participant's race history across every completed event they enrolled in.</summary>
[Authorize(Roles = AppRoles.Participant)]
public class MyResultsController : Controller
{
    private readonly IRaceDayApi _api;

    public MyResultsController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var results = await _api.GetMyResultsAsync();
        return View(results.OrderByDescending(r => r.EventDate).ToList());
    }
}
