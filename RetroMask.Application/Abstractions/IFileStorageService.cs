namespace RetroMask.Application.Abstractions;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
    string GetPublicUrl(string storagePath);
}
