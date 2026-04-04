using AlterEgo.Api.Dtos;
using AlterEgo.Core.Entities;
using AlterEgo.Core.Repositories;
using AlterEgo.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlterEgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsersRepository _usersRepository;
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsersRepository usersRepository,
        IRefreshTokensRepository refreshTokensRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _usersRepository = usersRepository;
        _refreshTokensRepository = refreshTokensRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for username: {Username}", request.Username);

        var user = await _usersRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return Unauthorized(new ErrorResponse("Invalid username or password"));
        }

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        var tokenResponse = await CreateTokenResponseAsync(user, cancellationToken);
        return Ok(tokenResponse);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Token refresh attempt");

        var refreshToken = await _refreshTokensRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            _logger.LogWarning("Token refresh failed: invalid or expired token");
            return Unauthorized(new ErrorResponse("Invalid or expired refresh token"));
        }

        if (refreshToken.User is null)
        {
            _logger.LogWarning("Token refresh failed: user not found for token");
            return Unauthorized(new ErrorResponse("Invalid refresh token"));
        }

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refreshed for user {UserId}", refreshToken.UserId);

        var tokenResponse = await CreateTokenResponseAsync(refreshToken.User, cancellationToken);
        return Ok(tokenResponse);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        await _refreshTokensRepository.RevokeAllByUserIdAsync(userId.Value, cancellationToken);
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged out, all refresh tokens revoked", userId);

        return NoContent();
    }

    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _usersRepository.GetByIdAsync(userId.Value, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed for user {UserId}: incorrect current password", userId);
            return BadRequest(new ErrorResponse("Current password is incorrect"));
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _usersRepository.SaveChangesAsync(cancellationToken);

        await _refreshTokensRepository.RevokeAllByUserIdAsync(userId.Value, cancellationToken);
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} changed password", userId);

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task<TokenResponse> CreateTokenResponseAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var expiresAt = _jwtService.GetRefreshTokenExpiration();

        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _refreshTokensRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(accessToken, refreshTokenValue, expiresAt);
    }
}
