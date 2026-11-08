namespace RaceDay.Api.Services;

/// <summary>
/// Settings for Azure Blob Storage, bound from the "AzureBlobStorage" configuration section.
/// Keep the connection string out of source control: use user secrets locally
/// (<c>dotnet user-secrets set "AzureBlobStorage:ConnectionString" "..."</c>) or the
/// <c>AzureBlobStorage__ConnectionString</c> environment variable when deployed.
/// </summary>
public class BlobStorageOptions
{
    public string? ConnectionString { get; set; }

    /// <summary>Container that holds event banners and profile pictures. Created on first upload.</summary>
    public string ContainerName { get; set; } = "raceday-images";
}
