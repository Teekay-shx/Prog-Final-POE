using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>
/// Azure Blob Storage implementation. The container client is created lazily on the first
/// upload, so the API still starts (and every non-upload endpoint still works) when no
/// storage account has been configured yet.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions _options;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private BlobContainerClient? _container;

    public AzureBlobStorageService(IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadImageAsync(Stream content, string contentType, string folder, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);

        // The blob name is generated, never taken from the uploaded file's name, so a
        // user cannot overwrite another image or smuggle path segments into the name.
        var blobName = $"{folder}/{Guid.NewGuid():N}{ImageUploadRules.ExtensionFor(contentType)}";
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return blob.Uri.ToString();
    }

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        if (_container is not null)
        {
            return _container;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new BlobStorageNotConfiguredException();
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_container is null)
            {
                var client = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);

                // Public read access at the blob level lets the browser show the image
                // straight from its URL; listing the container stays private.
                await client.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
                _container = client;
            }

            return _container;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
