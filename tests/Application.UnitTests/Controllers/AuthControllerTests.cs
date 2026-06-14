using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Controllers;
using Application.Common.Interfaces;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Application.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Services.MockUserRepository _userRepository;
    private readonly Services.MockJwtProvider _jwtProvider;
    private readonly UserService _userService;
    private readonly MockRevokedTokenRepository _revokedTokenRepository;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _userRepository = new Services.MockUserRepository();
        _jwtProvider = new Services.MockJwtProvider();
        _userService = new UserService(_userRepository, _jwtProvider, new Services.MockDateTimeProvider(), new Services.MockPasswordHasher());
        _revokedTokenRepository = new MockRevokedTokenRepository();
        _authController = new AuthController(_userService, _revokedTokenRepository);
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "newuser@example.com", "Password123!");

        // Act
        var result = await _authController.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("newuser", response.Username);
        Assert.Equal("newuser@example.com", response.Email);
        Assert.Equal("mock_token_newuser", response.Token);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var password = "Password123!";
        var user = new User(
            Guid.NewGuid(),
            "loginuser",
            "loginuser@example.com",
            HashPassword(password),
            DateTime.UtcNow
        );
        _userRepository.Users.Add(user);
        var request = new LoginRequest("loginuser@example.com", password);

        // Act
        var result = await _authController.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("loginuser", response.Username);
        Assert.Equal("mock_token_loginuser", response.Token);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk_AndRevokeToken()
    {
        // Arrange
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer test_token_to_revoke";
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _authController.Logout();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Contains(_revokedTokenRepository.RevokedTokens, t => t.Token == "test_token_to_revoke");
    }

    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

public class MockRevokedTokenRepository : IRevokedTokenRepository
{
    public List<(string Token, DateTime ExpiryDate)> RevokedTokens { get; } = new();

    public Task RevokeAsync(string token, DateTime expiryDate)
    {
        RevokedTokens.Add((token, expiryDate));
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string token)
    {
        return Task.FromResult(RevokedTokens.Any(t => t.Token == token));
    }
}
