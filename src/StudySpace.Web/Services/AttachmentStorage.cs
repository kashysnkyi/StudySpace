using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace StudySpace.Web.Services;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(string storedName, Stream content, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string storageUrl, CancellationToken cancellationToken);
}

public sealed class AttachmentStorage(IConfiguration configuration, IWebHostEnvironment environment) : IAttachmentStorage
{
    public async Task<string> SaveAsync(string storedName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var container = new BlobContainerClient(connectionString, "studyspace-assets");
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            var blob = container.GetBlobClient(storedName);
            await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
            return blob.Uri.ToString();
        }

        var uploadsPath = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);
        await using var destination = File.Create(Path.Combine(uploadsPath, storedName));
        await content.CopyToAsync(destination, cancellationToken);
        return $"/uploads/{storedName}";
    }

    public async Task DeleteAsync(string storageUrl, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var container = new BlobContainerClient(connectionString, "studyspace-assets");
            var blobName = Uri.TryCreate(storageUrl, UriKind.Absolute, out var uri)
                ? Uri.UnescapeDataString(uri.Segments[^1])
                : Path.GetFileName(storageUrl);
            await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
            return;
        }

        var fileName = Path.GetFileName(storageUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var uploadsPath = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
        var filePath = Path.GetFullPath(Path.Combine(uploadsPath, fileName));
        if (filePath.StartsWith(uploadsPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(filePath);
        }
    }
}
