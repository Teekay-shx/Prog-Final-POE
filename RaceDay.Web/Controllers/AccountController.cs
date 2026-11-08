using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApi _api;

    public AccountController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password });

            if (!IsWebRole(result.User.Role))
            {
                ModelState.AddModelError(string.Empty, "Admin accounts are managed through the API and have no web interface.");
                return View(model);
            }

            await UserSession.SignInAsync(HttpContext, result.User, result.ApiCookie);
            return RedirectAfterLogin(model.ReturnUrl);
        }
        catch (ApiUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!IsWebRole(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Please choose Organiser or Participant.");
            return View(model);
        }

        try
        {
            var result = await _api.RegisterAsync(new RegisterRequest
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Password = model.Password,
                Role = model.Role,
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                DateOfBirth = model.DateOfBirth
            });

            await UserSession.SignInAsync(HttpContext, result.User, result.ApiCookie);
            TempData["Success"] = $"Welcome to RaceDay, {result.User.FullName}!";
            return RedirectToHome();
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        await UserSession.SignOutAsync(HttpContext);

        TempData["Success"] = "You've been logged out.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private static bool IsWebRole(string role) => role == AppRoles.Organiser || role == AppRoles.Participant;

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToHome();
    }

    private IActionResult RedirectToHome() =>
        User.IsInRole(AppRoles.Organiser) || HttpContext.Session.GetString(SessionKeys.Role) == AppRoles.Organiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Home");
}
