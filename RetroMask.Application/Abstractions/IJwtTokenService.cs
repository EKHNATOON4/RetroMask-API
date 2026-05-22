using RetroMask.Domain.Entities.Identity;

namespace RetroMask.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    RefreshToken GenerateRefreshToken(string ipAddress);
    string? GetUserIdFromExpiredToken(string token);
    bool ValidateToken(string token);
}
