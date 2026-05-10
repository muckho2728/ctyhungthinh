using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using CoffeeAnalytics.Application.DTOs.Auth;
using CoffeeAnalytics.Application.Interfaces;
using CoffeeAnalytics.Domain.Entities;
using CoffeeAnalytics.Domain.Interfaces;
using CoffeeAnalytics.Infrastructure.Settings;

namespace CoffeeAnalytics.Infrastructure.Services;

/// <summary>
/// JWT authentication service with refresh token rotation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly JwtSettings _jwt;

    public AuthService(
        IUserRepository userRepo,
        IRefreshTokenRepository tokenRepo,
        IOptions<JwtSettings> jwtOptions)
    {
        _userRepo = userRepo;
        _tokenRepo = tokenRepo;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _userRepo.ExistsAsync(request.Email, ct))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
            Role = UserRole.Free
        };

        await _userRepo.AddAsync(user, ct);
        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var refreshToken = await _tokenRepo.GetByTokenAsync(request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!refreshToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        // Token rotation: revoke old, issue new
        var newRefreshToken = GenerateRefreshToken();
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = newRefreshToken.Token;

        await _tokenRepo.UpdateAsync(refreshToken, ct);

        newRefreshToken.UserId = refreshToken.UserId;
        await _tokenRepo.AddAsync(newRefreshToken, ct);

        var accessToken = GenerateJwtToken(refreshToken.User);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        return new AuthResponse(
            accessToken,
            newRefreshToken.Token,
            expiresAt,
            MapToUserDto(refreshToken.User)
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _tokenRepo.GetByTokenAsync(refreshToken, ct)
            ?? throw new InvalidOperationException("Token not found.");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _tokenRepo.UpdateAsync(token, ct);
    }

    // ─── Private Helpers ──────────────────────────────────────

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, CancellationToken ct)
    {
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        refreshToken.UserId = user.Id;

        await _tokenRepo.AddAsync(refreshToken, ct);

        return new AuthResponse(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            MapToUserDto(user)
        );
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken() => new()
    {
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshExpiryDays)
    };

    private static UserDto MapToUserDto(User user) => new(
        user.Id, user.Email, user.FullName, user.Role.ToString());
}
