using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.DTOs.Auth;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(emailNormalized, cancellationToken))
        {
            _logger.LogWarning("Registration failed: User with email {Email} already exists.", request.Email);
            throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = emailNormalized,
            PasswordHash = passwordHash,
            Role = request.Role,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} registered successfully with role {Role}.", user.Id, user.Role);

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(emailNormalized, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for email {Email}: Invalid credentials.", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        _logger.LogInformation("User {UserId} logged in successfully.", user.Id);

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = expiresAt
        };
    }
}
