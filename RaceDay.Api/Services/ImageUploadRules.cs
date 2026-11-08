namespace RaceDay.Api.Services;

/// <summary>Server-side checks applied to every uploaded image before it is sent to storage.</summary>
public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    /// <summary>Returns a user-facing error message, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Please choose an image file to upload.";
        }

        if (file.Length > MaxBytes)
        {
            return "The image is too large. The maximum size is 5 MB.";
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return "Only JPEG, PNG, WebP or GIF images can be uploaded.";
        }

        return null;
    }

    public static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".bin"
    };
}
