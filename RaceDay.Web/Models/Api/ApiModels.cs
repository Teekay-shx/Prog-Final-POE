namespace RaceDay.Web.Models.Api;

// These classes only describe the JSON the RaceDay API sends and receives. They hold no
// business rules: every rule (who may do what, capacity, ownership) lives in the API.

public class ApiUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ApiProfile
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class ApiCategory
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
}

public class ApiEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public decimal? Distance { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }

    /// <summary>Only filled when a single event is fetched (GET /api/events/{id}).</summary>
    public List<ApiCategory> Categories { get; set; } = new();
}

public class ApiOrganiserEvent : ApiEvent
{
    public int TotalEnrolments { get; set; }
    public int CategoryCount { get; set; }
}

public class ApiEnrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public int? ResultId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? ResultStatus { get; set; }
}

public class ApiResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string? BibNumber { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;

    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly? EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalFinishers { get; set; }
}

// ---------- Request bodies sent to the API ----------

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

/// <summary>Body for both creating an event and updating one (Status is sent only on update).</summary>
public class EventRequest
{
    public string Name { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public decimal? Distance { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Status { get; set; }
}

public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
}

public class ResultRequest
{
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}

public class StatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public record AuthResult(ApiUser User, string ApiCookie);
