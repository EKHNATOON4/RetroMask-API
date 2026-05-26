using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Auth;
using RetroMask.Application.Services.Auth;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Infrastructure.Persistence;

namespace RetroMask.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly RetroMaskDbContext _db;
    private readonly IMapper _mapper;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        RetroMaskDbContext db,
        IMapper mapper)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _db = db;
        _mapper = mapper;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return ApiResponse<AuthResponse>.Fail("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = request.DisplayName ?? $"{request.FirstName} {request.LastName}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return ApiResponse<AuthResponse>.Fail("Registration failed.", result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, "User");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken("registration");

        refreshToken.UserId = user.Id;
        await _db.RefreshTokens.AddAsync(refreshToken, ct);
        await _db.SaveChangesAsync(ct);

        var profile = _mapper.Map<UserProfileDto>(user);
        profile.Role = roles.FirstOrDefault() ?? "User";

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = profile
        }, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

        if (!user.IsActive)
            return ApiResponse<AuthResponse>.Fail("Account is deactivated.");

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(ipAddress);

        refreshToken.UserId = user.Id;
        await _db.RefreshTokens.AddAsync(refreshToken, ct);
        await _db.SaveChangesAsync(ct);

        var profile = _mapper.Map<UserProfileDto>(user);
        profile.Role = roles.FirstOrDefault() ?? "User";

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = profile
        }, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, CancellationToken ct = default)
    {
        var userId = _jwtTokenService.GetUserIdFromExpiredToken(request.AccessToken);
        if (userId == null)
            return ApiResponse<AuthResponse>.Fail("Invalid access token.");

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.UserId == userId, ct);

        if (storedToken == null || !storedToken.IsActive)
            return ApiResponse<AuthResponse>.Fail("Invalid or expired refresh token.");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<AuthResponse>.Fail("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(ipAddress);

        storedToken.ReplacedByToken = newRefreshToken.Token;
        newRefreshToken.UserId = user.Id;

        await _db.RefreshTokens.AddAsync(newRefreshToken, ct);
        await _db.SaveChangesAsync(ct);

        var profile = _mapper.Map<UserProfileDto>(user);
        profile.Role = roles.FirstOrDefault() ?? "User";

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = profile
        }, "Token refreshed.");
    }

    public async Task<ApiResponse> LogoutAsync(string userId, string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken && t.UserId == userId, ct);

        if (storedToken == null)
            return ApiResponse.Fail("Refresh token not found.");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ApiResponse.Ok("Logged out successfully.");
    }

    public async Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse.Fail("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return ApiResponse.Fail("Password change failed.", result.Errors.Select(e => e.Description));

        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return ApiResponse.Ok("Password changed successfully.");
    }

    public async Task<ApiResponse> ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return ApiResponse.Ok("If the email exists, a reset link has been sent.");

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink = $"https://retromask.com/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";
        var htmlBody = $"<h2>Reset Your Password</h2><p>Click the link below to reset your password:</p><a href=\"{resetLink}\">Reset Password</a><p>This link will expire shortly. If you did not request this, please ignore this email.</p>";

        await _emailService.SendAsync(email, "RetroMask - Password Reset", htmlBody, ct);

        return ApiResponse.Ok("If the email exists, a reset link has been sent.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ApiResponse.Fail("Invalid reset request.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return ApiResponse.Fail("Password reset failed.", result.Errors.Select(e => e.Description));

        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return ApiResponse.Ok("Password has been reset successfully.");
    }
}
