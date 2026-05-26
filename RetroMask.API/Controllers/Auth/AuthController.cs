using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Auth;
using RetroMask.Application.Services.Auth;
using RetroMask.Domain.Entities.Identity;

namespace RetroMask.API.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _fileStorageService;

    public AuthController(
        IAuthService authService,
        UserManager<ApplicationUser> userManager,
        IFileStorageService fileStorageService)
    {
        _authService = authService;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.LoginAsync(request, ip, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.RefreshTokenAsync(request, ip, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _authService.LogoutAsync(userId, request.RefreshToken, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _authService.ChangePasswordAsync(userId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email, ct);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file uploaded."));

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(ApiResponse.Fail("File size must not exceed 2 MB."));

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(ApiResponse.Fail("Only JPEG, PNG, WebP, and GIF images are allowed."));

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return NotFound(ApiResponse.Fail("User not found."));

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldPath = user.AvatarUrl.TrimStart('/').Replace("uploads/", "");
            await _fileStorageService.DeleteAsync(oldPath, ct);
        }

        using var stream = file.OpenReadStream();
        var storagePath = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, ct);
        var publicUrl = _fileStorageService.GetPublicUrl(storagePath);

        user.AvatarUrl = publicUrl;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse<object>.Ok(new { avatarUrl = publicUrl }, "Avatar uploaded successfully."));
    }

    [Authorize]
    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return NotFound(ApiResponse.Fail("User not found."));

        if (string.IsNullOrEmpty(user.AvatarUrl))
            return BadRequest(ApiResponse.Fail("No avatar to remove."));

        var storagePath = user.AvatarUrl.TrimStart('/').Replace("uploads/", "");
        await _fileStorageService.DeleteAsync(storagePath, ct);

        user.AvatarUrl = null;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse.Ok("Avatar removed successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse.Fail("User not found."));

        var roles = await _userManager.GetRolesAsync(user);

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Role = roles.FirstOrDefault() ?? "User"
        };

        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }
}
