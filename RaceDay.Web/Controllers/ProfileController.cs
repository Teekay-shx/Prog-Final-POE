using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>View and update personal details and upload a profile picture (stored by the API in Azure Blob Storage).</summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly IRaceDayApi _api;

    public ProfileController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await _api.GetProfileAsync();
        return View(ToViewModel(profile));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillReadOnlyFieldsAsync(model);
            return View("Index", model);
        }

        try
        {
            var updated = await _api.UpdateProfileAsync(new UpdateProfileRequest
            {
                FullName = model.FullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                DateOfBirth = model.DateOfBirth
            });

            // Refresh the name shown in the navigation bar.
            await UserSession.SignInAsync(HttpContext, new ApiUser
            {
                UserId = updated.UserId,
                FullName = updated.FullName,
                Email = updated.Email,
                Role = updated.Role
            });

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await FillReadOnlyFieldsAsync(model);
            return View("Index", model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose a picture first.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = picture.OpenReadStream();
            await _api.UploadProfilePictureAsync(stream, picture.FileName, picture.ContentType);
            TempData["Success"] = "Your profile picture has been updated.";
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task FillReadOnlyFieldsAsync(ProfileViewModel model)
    {
        var profile = await _api.GetProfileAsync();
        model.Email = profile.Email;
        model.Role = profile.Role;
        model.ProfilePictureUrl = profile.ProfilePictureUrl;
    }

    private static ProfileViewModel ToViewModel(ApiProfile profile) => new()
    {
        FullName = profile.FullName,
        Email = profile.Email,
        Role = profile.Role,
        Phone = profile.Phone,
        DateOfBirth = profile.DateOfBirth,
        ProfilePictureUrl = profile.ProfilePictureUrl
    };
}
