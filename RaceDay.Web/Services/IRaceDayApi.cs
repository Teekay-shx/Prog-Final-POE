using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// Everything the web app can ask the RaceDay API to do. The web app has no database and no
/// business rules of its own: each method is one API call.
/// </summary>
public interface IRaceDayApi
{
    // Authentication
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task LogoutAsync();

    // Profile
    Task<ApiProfile> GetProfileAsync();
    Task<ApiProfile> UpdateProfileAsync(UpdateProfileRequest request);
    Task<ApiProfile> UploadProfilePictureAsync(Stream file, string fileName, string contentType);

    // Events
    Task<List<ApiEvent>> GetEventsAsync(string? type, string? location);
    Task<ApiEvent> GetEventAsync(int eventId);
    Task<List<ApiOrganiserEvent>> GetMyEventsAsync();
    Task<ApiEvent> CreateEventAsync(EventRequest request);
    Task<ApiEvent> UpdateEventAsync(int eventId, EventRequest request);
    Task DeleteEventAsync(int eventId);
    Task<ApiEvent> UploadEventBannerAsync(int eventId, Stream file, string fileName, string contentType);

    // Categories
    Task<ApiCategory> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiCategory> UpdateCategoryAsync(int categoryId, CategoryRequest request);
    Task DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiEnrolment> EnrolAsync(int categoryId);
    Task<List<ApiEnrolment>> GetMyEnrolmentsAsync();
    Task CancelEnrolmentAsync(int enrolmentId);
    Task<List<ApiEnrolment>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiEnrolment> UpdateEnrolmentStatusAsync(int enrolmentId, string status);

    // Results
    Task<List<ApiResult>> GetMyResultsAsync();
    Task<ApiResult> CreateResultAsync(int enrolmentId, ResultRequest request);
    Task<ApiResult> UpdateResultAsync(int resultId, ResultRequest request);
}
