using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lost_Item.Data;
using Lost_Item.DTOs;
using Lost_Item.Models;

namespace Lost_Item.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse?> AuthenticateGoogleAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            _logger.LogWarning("AuthenticateGoogleAsync called with empty token");
            return null;
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Invalid Google token: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating Google token");
            return null;
        }

        var adminEmails = _config.GetSection("AdminEmails").Get<List<string>>() ?? [];
        var isAdminEmail = adminEmails.Contains(payload.Email, StringComparer.OrdinalIgnoreCase);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);
        if (user == null)
        {
            user = new User
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                IsAdmin = isAdminEmail
            };
            _db.Users.Add(user);
            _logger.LogInformation("New user registered: {Email} (IsAdmin={IsAdmin})", user.Email, user.IsAdmin);
        }
        else
        {
            // Sync name/email with Google; promote to admin if email is in AdminEmails
            user.Name = payload.Name;
            user.Email = payload.Email;
            if (isAdminEmail && !user.IsAdmin)
            {
                user.IsAdmin = true;
                _logger.LogInformation("User promoted to admin via AdminEmails config: {Email}", user.Email);
            }
        }

        await _db.SaveChangesAsync();

        var jwt = GenerateJwt(user);
        _logger.LogInformation("User authenticated: {Email}", user.Email);
        return new AuthResponse(jwt, user.Name, user.Email, user.IsAdmin);
    }

    public async Task<(bool Success, string? Error)> LogoutAsync(string jti, int userId, DateTime expiresAt)
    {
        var alreadyRevoked = await _db.RevokedTokens.AnyAsync(t => t.Jti == jti);
        if (alreadyRevoked)
            return (false, "Token already revoked");

        _db.RevokedTokens.Add(new RevokedToken
        {
            Jti = jti,
            UserId = userId,
            ExpiresAt = expiresAt
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Token revoked for UserId={UserId}", userId);
        return (true, null);
    }

    public async Task<bool> IsTokenRevokedAsync(string jti) =>
        await _db.RevokedTokens.AnyAsync(t => t.Jti == jti);

    public async Task<UserResponse?> GetMeAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user == null ? null : MapToResponse(user);
    }

    public async Task<List<UserResponse>> GetAllUsersAsync()
    {
        var users = await _db.Users.OrderBy(u => u.CreatedAt).ToListAsync();
        return users.Select(MapToResponse).ToList();
    }

    public async Task<(bool Success, string? Error)> SetAdminAsync(int targetUserId, bool isAdmin)
    {
        var user = await _db.Users.FindAsync(targetUserId);
        if (user == null) return (false, "User not found");
        if (user.Id == 1) return (false, "Cannot change the seeded admin account");

        user.IsAdmin = isAdmin;
        await _db.SaveChangesAsync();
        _logger.LogInformation("User {UserId} IsAdmin set to {IsAdmin}", targetUserId, isAdmin);
        return (true, null);
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var expired = await _db.RevokedTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expired.Any())
        {
            _db.RevokedTokens.RemoveRange(expired);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired revoked tokens", expired.Count);
        }
    }

    // --- Helpers ---

    private string GenerateJwt(User user)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString(); // unique token ID for blacklist

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("isAdmin", user.IsAdmin.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserResponse MapToResponse(User u) =>
        new(u.Id, u.Email, u.Name, u.IsAdmin, u.CreatedAt);
}