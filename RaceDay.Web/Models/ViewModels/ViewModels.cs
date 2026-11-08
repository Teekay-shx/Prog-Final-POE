using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

public class ErrorViewModel
{
    public int StatusCode { get; set; } = 500;
    public string Message { get; set; } = "Something went wrong. Please try again.";

    public string Title => StatusCode switch
    {
        404 => "We couldn't find that page",
        403 => "You don't have access to that",
        401 => "Please log in",
        503 or 504 => "The service is unavailable",
        _ => "Something went wrong"
    };
}

// ---------- Account ----------

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(8, ErrorMessage = "Your password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose how you'll use RaceDay.")]
    public string Role { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }
}

// ---------- Participant pages ----------

public class HomeIndexViewModel
{
    public List<ApiEvent> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public bool IncludePast { get; set; }
}

public class EventDetailsViewModel
{
    public ApiEvent Event { get; set; } = new();

    /// <summary>Categories of this event the logged-in participant is already enrolled in.</summary>
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();

    public bool CanEnrol { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }
}

// ---------- Organiser pages ----------

public class DashboardViewModel
{
    public List<ApiOrganiserEvent> Events { get; set; } = new();

    public int TotalEnrolments => Events.Sum(e => e.TotalEnrolments);

    public List<ApiOrganiserEvent> Upcoming(DateOnly today) =>
        Events.Where(e => e.EventDate >= today && e.Status != "Cancelled").OrderBy(e => e.EventDate).ToList();
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter an event name.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Date")]
    public DateOnly? EventDate { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public string? Description { get; set; }

    [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string Type { get; set; } = "Run";

    public string Status { get; set; } = "Planned";

    /// <summary>The banner chosen in the form. It is sent to the API, which stores it in Azure Blob Storage.</summary>
    public IFormFile? BannerFile { get; set; }

    public string? ExistingBannerUrl { get; set; }

    public bool IsEdit => EventId.HasValue;
}

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name, e.g. 10km or Under 20.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0, 100000, ErrorMessage = "Entry fee can't be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }
}

public class CategoriesPageViewModel
{
    public ApiEvent Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiEnrolment> Enrolments { get; set; } = new();
    public int? CategoryId { get; set; }
}

public class ResultsPageViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiEnrolment> Enrolments { get; set; } = new();
}

/// <summary>One participant's result row, posted from the results entry page.</summary>
public class ResultInputModel
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}
