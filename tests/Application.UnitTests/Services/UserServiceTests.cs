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
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepository = new MockUserRepository();
        _jwtProvider = new MockJwtProvider();
        _userService = new UserService(_userRepository, _jwtProvider);
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
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "existing",
            PasswordHash = "hashed"
        };
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Password123!"))
        };
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            // We use standard hashing in UserService, but in mock we can just use the actual hashing method or make the UserService hashing testable.
            // Since we'll write UserService hash logic using a SHA256 helper, let's hash "Password123!" using SHA256.
            // Let's implement a SHA256 helper in the actual service, but in the test, we'll hash it similarly:
            PasswordHash = HashPassword(password)
        };
        _userRepository.Users.Add(user);

        var request = new LoginRequest("test@example.com", password);

        var result = await _userService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("mock_token_testuser", result.Token);
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

#endregion
