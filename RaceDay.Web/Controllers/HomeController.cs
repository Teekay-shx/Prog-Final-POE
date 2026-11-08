using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly IRaceDayApi _api;

    public HomeController(IRaceDayApi api)
    {
        _api = api;
    }

    /// <summary>The participant home page: upcoming events with filters. Organisers land on their dashboard instead.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? type, string? location, bool includePast = false)
    {
        if (User.IsInRole(AppRoles.Organiser))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var events = await _api.GetEventsAsync(type, location);

        var today = DateOnly.FromDateTime(DateTime.Today);
        IEnumerable<ApiEvent> filtered = events;

        if (!includePast)
        {
            filtered = filtered.Where(e => e.EventDate >= today && e.Status != "Cancelled");
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Description ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return View(new HomeIndexViewModel
        {
            Events = filtered.OrderBy(e => e.EventDate).ToList(),
            Search = search,
            Type = type,
            Location = location,
            IncludePast = includePast
        });
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View(new ErrorViewModel());
    }

    [IgnoreAntiforgeryToken]
    public IActionResult StatusCode(int code)
    {
        var model = new ErrorViewModel
        {
            StatusCode = code,
            Message = code switch
            {
                404 => "The page you were looking for doesn't exist or has moved.",
                403 => "You don't have permission to view that page.",
                _ => "Something went wrong. Please try again."
            }
        };

        Response.StatusCode = code;
        return View("Error", model);
    }
}
