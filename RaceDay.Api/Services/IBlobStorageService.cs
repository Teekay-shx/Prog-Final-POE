namespace RaceDay.Api.Services;

/// <summary>Stores uploaded images in cloud storage and returns the public URL of the stored file.</summary>
public interface IBlobStorageService
{
    /// <param name="content">The image bytes.</param>
    /// <param name="contentType">Validated image MIME type, e.g. image/png.</param>
    /// <param name="folder">Virtual folder inside the container, e.g. "event-banners".</param>
    /// <returns>The absolute URL of the uploaded image.</returns>
    Task<string> UploadImageAsync(Stream content, string contentType, string folder, CancellationToken cancellationToken = default);
}

/// <summary>Thrown when an upload is attempted but no Azure Storage connection string has been configured.</summary>
public class BlobStorageNotConfiguredException : Exception
{
    public BlobStorageNotConfiguredException()
        : base("Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString.")
    {
    }
}
