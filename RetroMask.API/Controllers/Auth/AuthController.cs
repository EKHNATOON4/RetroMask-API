using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Auth;
using RetroMask.Application.Services.Auth;
using RetroMask.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace RetroMask.API.Controllers.Auth;

/// <summary>
/// Authentication and user profile management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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

    /// <summary>Register a new user account.</summary>
    /// <param name="request">Registration details including name, email, and password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Access and refresh tokens with user profile.</returns>
    /// <response code="200">Registration successful. Returns JWT tokens.</response>
    /// <response code="400">Validation failed or email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Authenticate with email and password.</summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Access and refresh tokens with user profile.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.LoginAsync(request, ip, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Refresh an expired access token using a valid refresh token.</summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">New tokens issued.</response>
    /// <response code="401">Refresh token is invalid or expired.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.RefreshTokenAsync(request, ip, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Revoke a refresh token (logout).</summary>
    /// <param name="request">The refresh token to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Token revoked successfully.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _authService.LogoutAsync(userId, request.RefreshToken, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Change the current user's password.</summary>
    /// <param name="request">Current and new passwords.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Current password is incorrect or new password is invalid.</response>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _authService.ChangePasswordAsync(userId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Request a password reset email.</summary>
    /// <param name="request">Email address to send the reset link to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Reset email sent (or silently ignored if email not found).</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email, ct);
        return Ok(result);
    }

    /// <summary>Reset password using a token from the reset email.</summary>
    /// <param name="request">Reset token and new password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Password reset successful.</response>
    /// <response code="400">Token is invalid or expired.</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Upload or replace the current user's avatar image.</summary>
    /// <param name="file">Image file (JPEG, PNG, WebP, or GIF). Max 2 MB.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Avatar uploaded. Returns public URL.</response>
    /// <response code="400">Invalid file format or size.</response>
    [Authorize]
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
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

    /// <summary>Delete the current user's avatar.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Avatar removed.</response>
    /// <response code="400">No avatar to remove.</response>
    [Authorize]
    [HttpDelete("avatar")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
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

    /// <summary>Get the authenticated user's profile.</summary>
    /// <returns>User profile with role information.</returns>
    /// <response code="200">Returns the user profile.</response>
    /// <response code="401">Not authenticated.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
