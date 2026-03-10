using Lost_Item.DTOs;

namespace Lost_Item.Services;

public interface IAuthService
{
    Task<AuthResponse?> AuthenticateGoogleAsync(string idToken);
    Task<(bool Success, string? Error)> LogoutAsync(string jti, int userId, DateTime expiresAt);
    Task<bool> IsTokenRevokedAsync(string jti);
    Task<UserResponse?> GetMeAsync(int userId);
    Task<List<UserResponse>> GetAllUsersAsync();
    Task<(bool Success, string? Error)> SetAdminAsync(int targetUserId, bool isAdmin);
    Task CleanupExpiredTokensAsync();
}