using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC / Controllers ----------
builder.Services.AddControllers();

// ---------- EF Core (Code-First) ----------
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Auth helpers ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// ---------- Azure Blob Storage (event banners and profile pictures) ----------
// The MVC front end never talks to Azure: it sends files to this API, and only this API
// holds the storage connection string.
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("AzureBlobStorage"));
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

// ---------- Cookie (session) authentication ----------
// Chosen over JWT because the brief asks for "session management to maintain the
// user's authenticated state and role for all subsequent requests" — an
// authentication cookie IS server-recognised session state, issued at login and
// sent back automatically by the browser/client on every following request.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Session";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // This is an API, not an MVC site with login pages — return status codes
        // instead of 302-redirecting to a non-existent /Account/Login page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "The REST API that powers the RaceDay running-events platform: " +
                      "authentication, user profiles, events, categories, enrolments, " +
                      "results and payments. Built to the endpoint plan in /docs/api-endpoint-plan.md."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // Documents the session cookie so Swagger UI's "Authorize" shows how auth works,
    // even though the cookie itself is set automatically by /api/auth/login.
    options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "RaceDay.Session",
        Description = "Session cookie issued by POST /api/auth/login. Swagger UI sends " +
                      "cookies automatically for same-origin requests once you've logged in " +
                      "through a 'Try it out' call on /api/auth/login — no need to paste a token."
    });
});

var app = builder.Build();

// ---------- Apply migrations + seed sample data on startup ----------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    if (context.Database.IsRelational())
    {
        context.Database.Migrate();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        DbSeeder.Seed(context, hasher);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    });
}
else
{
    // Swagger is kept available outside Development too, since Part 2 requires it
    // as live, browsable proof the implementation matches the Part 1 plan.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    });
}

// In Development the MVC app (locally or in Docker) calls this API over plain http, and a
// redirect to the self-signed dev certificate would make those calls fail.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class to the WebApplicationFactory<Program> used by
// the integration tests in RaceDay.Api.Tests.
public partial class Program { }
