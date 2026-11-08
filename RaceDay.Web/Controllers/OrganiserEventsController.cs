using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Create, edit and delete the organiser's own events, including the banner image.</summary>
[Authorize(Roles = AppRoles.Organiser)]
[Route("organiser/events")]
public class OrganiserEventsController : Controller
{
    private readonly IRaceDayApi _api;

    public OrganiserEventsController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var events = await _api.GetMyEventsAsync();
        return View(events.OrderBy(e => e.EventDate).ToList());
    }

    [HttpGet("create")]
    public IActionResult Create() => View("Form", new EventFormViewModel());

    [HttpPost("create")]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            var created = await _api.CreateEventAsync(ToRequest(model));
            var bannerWarning = await TryUploadBannerAsync(created.EventId, model.BannerFile);

            TempData["Success"] = "Event created. Add categories so participants can enrol.";
            if (bannerWarning is not null)
            {
                TempData["Warning"] = bannerWarning;
            }

            return RedirectToAction("Index", "OrganiserCategories", new { eventId = created.EventId });
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (ev.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        return View("Form", new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Description = ev.Description,
            Distance = ev.Distance,
            Type = ev.Type,
            Status = ev.Status,
            ExistingBannerUrl = ev.BannerImageUrl
        });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await _api.UpdateEventAsync(id, ToRequest(model));
            var bannerWarning = await TryUploadBannerAsync(id, model.BannerFile);

            TempData["Success"] = "Event updated.";
            if (bannerWarning is not null)
            {
                TempData["Warning"] = bannerWarning;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _api.DeleteEventAsync(id);
            TempData["Success"] = "Event deleted.";
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static EventRequest ToRequest(EventFormViewModel model) => new()
    {
        Name = model.Name.Trim(),
        EventDate = model.EventDate!.Value,
        Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim(),
        Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
        Distance = model.Distance,
        Type = model.Type,
        Status = model.IsEdit ? model.Status : null
    };

    /// <summary>Sends the banner to the API (which stores it in Azure). A failure here must not lose the saved event.</summary>
    private async Task<string?> TryUploadBannerAsync(int eventId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _api.UploadEventBannerAsync(eventId, stream, file.FileName, file.ContentType);
            return null;
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            return $"The event was saved, but the banner image could not be uploaded: {ex.Message}";
        }
    }
}
