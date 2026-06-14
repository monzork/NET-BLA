using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Xunit;

namespace Application.UnitTests.Services;

public class UserServiceTests
{
    private readonly MockUserRepository _userRepository;
    private readonly MockJwtProvider _jwtProvider;
    private readonly MockDateTimeProvider _dateTimeProvider;
    private readonly MockPasswordHasher _passwordHasher;
    private readonly MockRefreshTokenRepository _refreshTokenRepository;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepository = new MockUserRepository();
        _jwtProvider = new MockJwtProvider();
        _dateTimeProvider = new MockDateTimeProvider();
        _passwordHasher = new MockPasswordHasher();
        _refreshTokenRepository = new MockRefreshTokenRepository();
        _userService = new UserService(_userRepository, _jwtProvider, _dateTimeProvider, _passwordHasher, _refreshTokenRepository);
    }

    [Fact]
    public async Task Register_ShouldThrowException_WhenEmailIsEmpty()
    {
        var request = new RegisterRequest("username", "", "password");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync(request));
        Assert.Contains("Email and Username are required", exception.Message);
    }

    [Fact]
    public async Task Register_ShouldThrowException_WhenEmailAlreadyExists()
    {
        var existingUser = new User(Guid.NewGuid(), "existing", "test@example.com", "hashed", DateTime.UtcNow);
        _userRepository.Users.Add(existingUser);

        var request = new RegisterRequest("username", "test@example.com", "password");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync(request));
        Assert.Contains("Email is already registered", exception.Message);
    }

    [Fact]
    public async Task Register_ShouldSucceed_WhenDataIsValid()
    {
        var request = new RegisterRequest("newuser", "new@example.com", "Password123!");

        var result = await _userService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("newuser", result.Username);
        Assert.Equal("mock_token_newuser", result.Token);

        var savedUser = _userRepository.Users.FirstOrDefault(u => u.Email == "new@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("newuser", savedUser.Username);
    }

    [Fact]
    public async Task Login_ShouldThrowException_WhenEmailDoesNotExist()
    {
        var request = new LoginRequest("nonexistent@example.com", "password");

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _userService.LoginAsync(request));
        Assert.Contains("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task Login_ShouldThrowException_WhenPasswordIsIncorrect()
    {
        // Simple hash logic: password + "_hashed" (just for mock testing)
        var user = new User(
            Guid.NewGuid(),
            "testuser",
            "test@example.com",
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Password123!")),
            DateTime.UtcNow
        );
        _userRepository.Users.Add(user);

        var request = new LoginRequest("test@example.com", "WrongPassword");

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _userService.LoginAsync(request));
        Assert.Contains("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task Login_ShouldSucceed_WhenCredentialsAreValid()
    {
        // Password hash using base64 for simplicity in mocks/tests
        var password = "Password123!";
        var user = new User(
            Guid.NewGuid(),
            "testuser",
            "test@example.com",
            HashPassword(password),
            DateTime.UtcNow
        );
        _userRepository.Users.Add(user);

        var request = new LoginRequest("test@example.com", password);

        var result = await _userService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("mock_token_testuser", result.Token);
    }

    [Fact]
    public async Task Refresh_ShouldThrowException_WhenTokenDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _userService.RefreshTokensAsync("nonexistent_token"));
        Assert.Contains("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task Refresh_ShouldThrowException_WhenTokenIsExpired()
    {
        var userId = Guid.NewGuid();
        var expiredToken = new RefreshToken("expired_token", userId, _dateTimeProvider.UtcNow.AddMinutes(-5), _dateTimeProvider.UtcNow.AddDays(-1));
        _refreshTokenRepository.RefreshTokens.Add(expiredToken);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _userService.RefreshTokensAsync("expired_token"));
        Assert.Contains("Refresh token has expired", exception.Message);
    }

    [Fact]
    public async Task Refresh_ShouldSucceed_AndRotateToken_WhenTokenIsValid()
    {
        var user = new User(Guid.NewGuid(), "testuser", "test@example.com", "hash", _dateTimeProvider.UtcNow);
        _userRepository.Users.Add(user);

        var validToken = new RefreshToken("valid_token", user.Id, _dateTimeProvider.UtcNow.AddDays(1), _dateTimeProvider.UtcNow);
        _refreshTokenRepository.RefreshTokens.Add(validToken);

        var result = await _userService.RefreshTokensAsync("valid_token");

        Assert.NotNull(result);
        Assert.NotEqual("valid_token", result.RefreshToken);
        Assert.Equal("testuser", result.Username);

        var oldToken = _refreshTokenRepository.RefreshTokens.First(t => t.Token == "valid_token");
        Assert.NotNull(oldToken.RevokedAt);

        var newToken = _refreshTokenRepository.RefreshTokens.First(t => t.Token == result.RefreshToken);
        Assert.Equal(user.Id, newToken.UserId);
        Assert.Null(newToken.RevokedAt);
    }

    [Fact]
    public async Task Refresh_ShouldTriggerBreachDetection_WhenTokenIsAlreadyRevoked()
    {
        var userId = Guid.NewGuid();
        var revokedToken = RefreshToken.CreateFromDatabase("revoked_token", userId, _dateTimeProvider.UtcNow.AddDays(1), _dateTimeProvider.UtcNow.AddDays(-1), _dateTimeProvider.UtcNow.AddMinutes(-10));
        _refreshTokenRepository.RefreshTokens.Add(revokedToken);

        var activeToken = new RefreshToken("active_token", userId, _dateTimeProvider.UtcNow.AddDays(1), _dateTimeProvider.UtcNow);
        _refreshTokenRepository.RefreshTokens.Add(activeToken);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _userService.RefreshTokensAsync("revoked_token"));
        Assert.Contains("Possible breach detected", exception.Message);

        Assert.True(_refreshTokenRepository.RevokeAllCalled);
        Assert.Equal(userId, _refreshTokenRepository.RevokeAllUserId);
        var activeTokenState = _refreshTokenRepository.RefreshTokens.First(t => t.Token == "active_token");
        Assert.NotNull(activeTokenState.RevokedAt);
    }

    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

#region Mocks

public class MockUserRepository : IUserRepository
{
    public List<User> Users { get; } = new();

    public Task<User?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return Task.FromResult(Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }

    public Task AddAsync(User user)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }
}

public class MockJwtProvider : IJwtProvider
{
    public string Generate(User user)
    {
        return $"mock_token_{user.Username}";
    }
}

public class MockDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}

public class MockPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return HashPassword(password) == hashedPassword;
    }
}

public class MockRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> RefreshTokens { get; } = new();
    public bool RevokeAllCalled { get; private set; }
    public Guid RevokeAllUserId { get; private set; }

    public Task SaveAsync(RefreshToken token)
    {
        var existing = RefreshTokens.FirstOrDefault(t => t.Token == token.Token);
        if (existing != null)
        {
            RefreshTokens.Remove(existing);
        }
        RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return Task.FromResult(RefreshTokens.FirstOrDefault(t => t.Token == token));
    }

    public Task RevokeAllForUserAsync(Guid userId)
    {
        RevokeAllCalled = true;
        RevokeAllUserId = userId;
        foreach (var token in RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null))
        {
            token.Revoke(DateTime.UtcNow);
        }
        return Task.CompletedTask;
    }
}

#endregion
