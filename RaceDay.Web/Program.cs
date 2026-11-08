using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is not configured. Set it in appsettings.json or the Api__BaseUrl environment variable.");

// ---------- MVC ----------
builder.Services.AddControllersWithViews(options =>
{
    // Turns failed API calls into friendly messages instead of crashes.
    options.Filters.Add<ApiExceptionFilter>();

    // Every POST must carry the anti-forgery token the form tag helper adds.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Optional fields (string?) are optional; only [Required] ones are required.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ---------- Session + cookie authentication ----------
// Session (server-side) holds the API's session cookie; the authentication cookie carries the
// user's id and role so [Authorize(Roles = ...)] protects every page.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Web.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// ---------- The only way this app reaches data: the RaceDay API ----------
builder.Services
    .AddHttpClient<IRaceDayApi, RaceDayApi>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // The session cookie is attached by hand (see RaceDayApi) so each user's calls use
        // their own API session; a shared cookie jar would mix users up.
        UseCookies = false,
        AllowAutoRedirect = false
    });

var app = builder.Build();

// Unexpected errors show a friendly page, never a stack trace.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

// One culture everywhere. Browsers send decimals as 42.20 in number inputs, but a machine set to
// a comma-decimal culture (e.g. South Africa) would otherwise fail to read them and would write
// 42,20 back into the form. Running locally or in Docker therefore behaves identically.
var invariantCulture = new RequestCulture(CultureInfo.InvariantCulture);
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = invariantCulture,
    SupportedCultures = new List<CultureInfo> { CultureInfo.InvariantCulture },
    SupportedUICultures = new List<CultureInfo> { CultureInfo.InvariantCulture }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
