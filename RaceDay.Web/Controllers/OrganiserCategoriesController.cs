using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Age or distance categories for one of the organiser's events (e.g. Under 20, 10km, 21km).</summary>
[Authorize(Roles = AppRoles.Organiser)]
[Route("organiser/events/{eventId:int}/categories")]
public class OrganiserCategoriesController : Controller
{
    private readonly IRaceDayApi _api;

    public OrganiserCategoriesController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await LoadOwnedEventAsync(eventId);
        if (ev is null)
        {
            return Forbid();
        }

        return View(new CategoriesPageViewModel { Event = ev, NewCategory = new CategoryFormViewModel { EventId = eventId } });
    }

    [HttpPost("")]
    public async Task<IActionResult> Create(int eventId, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        ValidateAgeRange(model);

        if (!ModelState.IsValid)
        {
            return await ShowIndexWithAsync(eventId, model);
        }

        try
        {
            await _api.CreateCategoryAsync(eventId, ToRequest(model));
            TempData["Success"] = $"Category “{model.Name.Trim()}” added.";
            return RedirectToAction(nameof(Index), new { eventId });
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ShowIndexWithAsync(eventId, model);
        }
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var ev = await LoadOwnedEventAsync(eventId);
        if (ev is null)
        {
            return Forbid();
        }

        var category = ev.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category is null)
        {
            return NotFound();
        }

        ViewData["EventName"] = ev.Name;
        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            Name = category.Name,
            DistanceKm = category.DistanceKm,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        model.CategoryId = id;
        ValidateAgeRange(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _api.UpdateCategoryAsync(id, ToRequest(model));
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index), new { eventId });
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        try
        {
            await _api.DeleteCategoryAsync(id);
            TempData["Success"] = "Category removed.";
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    /// <summary>Returns the event only when the logged-in organiser owns it.</summary>
    private async Task<ApiEvent?> LoadOwnedEventAsync(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        return ev.OrganiserId == User.GetUserId() ? ev : null;
    }

    private async Task<IActionResult> ShowIndexWithAsync(int eventId, CategoryFormViewModel model)
    {
        var ev = await LoadOwnedEventAsync(eventId);
        if (ev is null)
        {
            return Forbid();
        }

        return View("Index", new CategoriesPageViewModel { Event = ev, NewCategory = model });
    }

    private void ValidateAgeRange(CategoryFormViewModel model)
    {
        if (model.MinAge.HasValue && model.MaxAge.HasValue && model.MinAge > model.MaxAge)
        {
            ModelState.AddModelError(nameof(model.MaxAge), "Maximum age can't be lower than minimum age.");
        }
    }

    private static CategoryRequest ToRequest(CategoryFormViewModel model) => new()
    {
        Name = model.Name.Trim(),
        DistanceKm = model.DistanceKm,
        MinAge = model.MinAge,
        MaxAge = model.MaxAge,
        EntryFee = model.EntryFee,
        MaxParticipants = model.MaxParticipants
    };
}
