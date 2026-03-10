using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lost_Item.DTOs;
using Lost_Item.Services;

namespace Lost_Item.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, IConfiguration config)
    {
        _auth = auth;
        _config = config;
    }

    /// <summary>POST /api/auth/google — exchange Google ID token for app JWT</summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequest req)
    {
        var result = await _auth.AuthenticateGoogleAsync(req.idToken);
        if (result == null) return Unauthorized("Invalid Google token");
        return Ok(result);
    }

    /// <summary>POST /api/auth/logout — revoke current JWT</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var (jti, expiresAt) = ExtractJtiAndExpiry();
        if (jti == null) return BadRequest("Token missing jti claim");

        var userId = GetUserId();
        var (success, error) = await _auth.LogoutAsync(jti, userId, expiresAt);

        if (!success) return BadRequest(error);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>GET /api/auth/me — current authenticated user</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        var user = await _auth.GetMeAsync(userId);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>GET /api/auth/users — admin: list all users</summary>
    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetAllUsers()
    {
        if (!IsAdmin()) return Forbid();
        var users = await _auth.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>PATCH /api/auth/users/{id}/make-admin — admin: grant or revoke admin</summary>
    [HttpPatch("users/{id}/make-admin")]
    [Authorize]
    public async Task<IActionResult> SetAdmin(int id, [FromBody] SetAdminRequest req)
    {
        if (!IsAdmin()) return Forbid();

        var (success, error) = await _auth.SetAdminAsync(id, req.IsAdmin);
        if (!success) return error == "User not found" ? NotFound(error) : BadRequest(error);
        return Ok(new { message = $"User {id} IsAdmin set to {req.IsAdmin}" });
    }

    // --- Helpers ---

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin() =>
        User.FindFirstValue("isAdmin") == "True";

    private (string? Jti, DateTime ExpiresAt) ExtractJtiAndExpiry()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ")) return (null, default);

        var rawToken = authHeader["Bearer ".Length..].Trim();
        var secret = _config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var handler = new JwtSecurityTokenHandler();
        try
        {
            handler.ValidateToken(rawToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out var validated);

            var jwt = (JwtSecurityToken)validated;
            var jti = jwt.Id;
            var exp = jwt.ValidTo;
            return (jti, exp);
        }
        catch
        {
            return (null, default);
        }
    }
}