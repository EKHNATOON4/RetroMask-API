using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Domain.Entities.Files;
using RetroMask.Domain.Enums;

namespace RetroMask.API.Controllers.Common;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _storageService;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public FilesController(IFileStorageService storageService, IUnitOfWork uow, ICurrentUser currentUser)
    {
        _storageService = storageService;
        _uow = uow;
        _currentUser = currentUser;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? entityType, [FromQuery] Guid? entityId, CancellationToken ct = default)
    {
        if (file.Length == 0) return BadRequest("File is empty.");

        var storagePath = await _storageService.UploadAsync(file.OpenReadStream(), file.FileName, file.ContentType, ct);
        var storedFile = new StoredFile
        {
            OriginalName = file.FileName,
            StoredName = Path.GetFileName(storagePath),
            StoragePath = storagePath,
            ContentType = file.ContentType,
            SizeInBytes = file.Length,
            FileType = GetFileType(file.ContentType),
            UploadedById = _currentUser.UserId,
            EntityType = entityType,
            EntityId = entityId,
            PublicUrl = _storageService.GetPublicUrl(storagePath)
        };

        await _uow.Repository<StoredFile>().AddAsync(storedFile, ct);
        await _uow.SaveChangesAsync(ct);

        return Ok(new { storedFile.Id, storedFile.PublicUrl, storedFile.OriginalName });
    }

    private static FileType GetFileType(string contentType) => contentType switch
    {
        var ct when ct.StartsWith("image/") => FileType.Image,
        "application/pdf" => FileType.Pdf,
        var ct when ct.StartsWith("video/") => FileType.Video,
        _ => FileType.Other
    };
}
