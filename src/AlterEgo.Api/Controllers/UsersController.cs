using AlterEgo.Api.Dtos;
using AlterEgo.Core.Entities;
using AlterEgo.Core.Enums;
using AlterEgo.Core.Repositories;
using AlterEgo.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlterEgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUsersRepository _usersRepository;
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUsersRepository usersRepository,
        IRefreshTokensRepository refreshTokensRepository,
        IPasswordHasher passwordHasher,
        ILogger<UsersController> logger)
    {
        _usersRepository = usersRepository;
        _refreshTokensRepository = refreshTokensRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin creating user: {Username}", request.Username);

        if (await _usersRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
        {
            return Conflict(new ErrorResponse("Username already exists"));
        }

        if (await _usersRepository.ExistsByTelegramIdAsync(request.TelegramId, cancellationToken))
        {
            return Conflict(new ErrorResponse("TelegramId already registered"));
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            TelegramId = request.TelegramId,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _usersRepository.AddAsync(user, cancellationToken);
        await _usersRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Username} created with ID {UserId}", user.Username, user.Id);

        var response = new UserResponse(user.Id, user.Username, user.TelegramId, user.Role, user.CreatedAt);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await _usersRepository.GetAllAsync(cancellationToken);
        var response = users.Select(u => new UserResponse(u.Id, u.Username, u.TelegramId, u.Role, u.CreatedAt)).ToList();
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserResponse(user.Id, user.Username, user.TelegramId, user.Role, user.CreatedAt));
    }

    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] SetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _usersRepository.SaveChangesAsync(cancellationToken);

        await _refreshTokensRepository.RevokeAllByUserIdAsync(id, cancellationToken);
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin set password for user {UserId}", id);

        return NoContent();
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (user.Role == Role.Admin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("Cannot modify admin user role"));
        }

        user.Role = request.Role;
        await _usersRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin changed role for user {UserId} to {Role}", id, request.Role);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (user.Role == Role.Admin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse("Cannot delete admin user"));
        }

        await _usersRepository.DeleteAsync(user, cancellationToken);
        await _usersRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin deleted user {UserId}", id);

        return NoContent();
    }
}
