using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Auth;

namespace RetroMask.Application.Services.Auth;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, CancellationToken ct = default);
    Task<ApiResponse> LogoutAsync(string userId, string refreshToken, CancellationToken ct = default);
    Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<ApiResponse> ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
