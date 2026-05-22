using Microsoft.Extensions.Configuration;
using RetroMask.Application.Abstractions;

namespace RetroMask.Infrastructure.Files;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = configuration["FileStorage:BaseUrl"] ?? "/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_basePath, uniqueName);
        await using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output, ct);
        return uniqueName;
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var full = Path.Combine(_basePath, storagePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storagePath) => $"{_baseUrl}/{storagePath}";
}
