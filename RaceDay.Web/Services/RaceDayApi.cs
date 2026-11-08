using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// HTTP client for the RaceDay API.
///
/// The API identifies a logged-in user by its own session cookie. This client keeps that cookie
/// in the web app's server-side session (the browser never sees it) and attaches it to every
/// request, so the API applies the same role and ownership rules it would for a direct caller.
/// </summary>
public class RaceDayApi : IRaceDayApi
{
    public const string ApiCookieName = "RaceDay.Session";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApi> _logger;

    public RaceDayApi(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApi> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    // ---------- Authentication ----------

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/auth/register", JsonContent.Create(request, options: Json));
        var user = await ReadAsync<ApiUser>(response);
        return new AuthResult(user, ExtractSessionCookie(response));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/auth/login", JsonContent.Create(request, options: Json));
        var user = await ReadAsync<ApiUser>(response);
        return new AuthResult(user, ExtractSessionCookie(response));
    }

    public async Task LogoutAsync()
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Post, "api/auth/logout");
        }
        catch (ApiException)
        {
            // The API session is already gone or unreachable; the web session is cleared regardless.
        }
    }

    // ---------- Profile ----------

    public Task<ApiProfile> GetProfileAsync() => GetAsync<ApiProfile>("api/users/me");

    public Task<ApiProfile> UpdateProfileAsync(UpdateProfileRequest request) =>
        SendJsonAsync<ApiProfile>(HttpMethod.Put, "api/users/me", request);

    public Task<ApiProfile> UploadProfilePictureAsync(Stream file, string fileName, string contentType) =>
        SendContentAsync<ApiProfile>(HttpMethod.Post, "api/users/me/profile-picture", FileBody(file, fileName, contentType));

    // ---------- Events ----------

    public Task<List<ApiEvent>> GetEventsAsync(string? type, string? location)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type;
        if (!string.IsNullOrWhiteSpace(location)) query["location"] = location;

        return GetAsync<List<ApiEvent>>(QueryHelpers.AddQueryString("api/events", query));
    }

    public Task<ApiEvent> GetEventAsync(int eventId) => GetAsync<ApiEvent>($"api/events/{eventId}");

    public Task<List<ApiOrganiserEvent>> GetMyEventsAsync() => GetAsync<List<ApiOrganiserEvent>>("api/events/mine");

    public Task<ApiEvent> CreateEventAsync(EventRequest request) =>
        SendJsonAsync<ApiEvent>(HttpMethod.Post, "api/events", request);

    public Task<ApiEvent> UpdateEventAsync(int eventId, EventRequest request) =>
        SendJsonAsync<ApiEvent>(HttpMethod.Put, $"api/events/{eventId}", request);

    public Task DeleteEventAsync(int eventId) => SendNoContentAsync(HttpMethod.Delete, $"api/events/{eventId}");

    public Task<ApiEvent> UploadEventBannerAsync(int eventId, Stream file, string fileName, string contentType) =>
        SendContentAsync<ApiEvent>(HttpMethod.Post, $"api/events/{eventId}/banner", FileBody(file, fileName, contentType));

    // ---------- Categories ----------

    public Task<ApiCategory> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        SendJsonAsync<ApiCategory>(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<ApiCategory> UpdateCategoryAsync(int categoryId, CategoryRequest request) =>
        SendJsonAsync<ApiCategory>(HttpMethod.Put, $"api/categories/{categoryId}", request);

    public Task DeleteCategoryAsync(int categoryId) => SendNoContentAsync(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ---------- Enrolments ----------

    public async Task<ApiEnrolment> EnrolAsync(int categoryId)
    {
        using var response = await SendAsync(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");
        return await ReadAsync<ApiEnrolment>(response);
    }

    public Task<List<ApiEnrolment>> GetMyEnrolmentsAsync() => GetAsync<List<ApiEnrolment>>("api/users/me/enrolments");

    public Task CancelEnrolmentAsync(int enrolmentId) => SendNoContentAsync(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    public Task<List<ApiEnrolment>> GetEventEnrolmentsAsync(int eventId) =>
        GetAsync<List<ApiEnrolment>>($"api/events/{eventId}/enrolments");

    public Task<ApiEnrolment> UpdateEnrolmentStatusAsync(int enrolmentId, string status) =>
        SendJsonAsync<ApiEnrolment>(HttpMethod.Put, $"api/enrolments/{enrolmentId}", new StatusRequest { Status = status });

    // ---------- Results ----------

    public Task<List<ApiResult>> GetMyResultsAsync() => GetAsync<List<ApiResult>>("api/users/me/results");

    public Task<ApiResult> CreateResultAsync(int enrolmentId, ResultRequest request) =>
        SendJsonAsync<ApiResult>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", request);

    public Task<ApiResult> UpdateResultAsync(int resultId, ResultRequest request) =>
        SendJsonAsync<ApiResult>(HttpMethod.Put, $"api/results/{resultId}", request);

    // ---------- Plumbing ----------

    private async Task<T> GetAsync<T>(string url)
    {
        using var response = await SendAsync(HttpMethod.Get, url);
        return await ReadAsync<T>(response);
    }

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string url, object body)
    {
        using var response = await SendAsync(method, url, JsonContent.Create(body, body.GetType(), options: Json));
        return await ReadAsync<T>(response);
    }

    private async Task<T> SendContentAsync<T>(HttpMethod method, string url, HttpContent content)
    {
        using var response = await SendAsync(method, url, content);
        return await ReadAsync<T>(response);
    }

    private async Task SendNoContentAsync(HttpMethod method, string url)
    {
        using var response = await SendAsync(method, url);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        var apiCookie = _accessor.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={apiCookie}");
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach the RaceDay API at {Address}", _http.BaseAddress);
            throw new ApiException(HttpStatusCode.ServiceUnavailable,
                "The RaceDay service is not reachable right now. Please try again in a moment.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "The RaceDay API did not answer in time");
            throw new ApiException(HttpStatusCode.GatewayTimeout,
                "The RaceDay service took too long to respond. Please try again.");
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            throw await BuildExceptionAsync(response);
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(Json);
            if (value is not null)
            {
                return value;
            }
        }
        catch (JsonException)
        {
            // fall through to the friendly error below
        }

        throw new ApiException(HttpStatusCode.BadGateway, "The RaceDay service returned an unexpected response.");
    }

    private static async Task<ApiException> BuildExceptionAsync(HttpResponseMessage response)
    {
        var status = response.StatusCode;
        string? message = null;

        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var apiMessage) && apiMessage.ValueKind == JsonValueKind.String)
                    {
                        message = apiMessage.GetString();
                    }
                    else if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        // ASP.NET model-validation problem details: { "errors": { "Field": ["message"] } }
                        message = errors.EnumerateObject()
                            .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                            .SelectMany(p => p.Value.EnumerateArray())
                            .Where(v => v.ValueKind == JsonValueKind.String)
                            .Select(v => v.GetString())
                            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // not JSON: use the fallback message
        }

        message ??= (int)status switch
        {
            400 => "Some of the information you entered is not valid.",
            401 => "Please log in to continue.",
            403 => "You don't have permission to do that.",
            404 => "We couldn't find what you were looking for.",
            409 => "That conflicts with something that already exists.",
            >= 500 => "Something went wrong on our side. Please try again.",
            _ => "Something went wrong. Please try again."
        };

        return status == HttpStatusCode.Unauthorized
            ? new ApiUnauthorizedException(message)
            : new ApiException(status, message);
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var prefix = ApiCookieName + "=";

        if (response.Headers.TryGetValues("Set-Cookie", out var headers))
        {
            foreach (var header in headers)
            {
                if (header.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var end = header.IndexOf(';');
                    return end < 0 ? header[prefix.Length..] : header[prefix.Length..end];
                }
            }
        }

        throw new ApiException(HttpStatusCode.BadGateway, "The RaceDay service did not start a session. Please try again.");
    }

    private static MultipartFormDataContent FileBody(Stream file, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var part = new StreamContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(part, "file", fileName);
        return content;
    }
}
