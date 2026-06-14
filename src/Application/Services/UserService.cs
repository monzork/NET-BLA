using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;

namespace Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public UserService(
        IUserRepository userRepository, 
        IJwtProvider jwtProvider, 
        IDateTimeProvider dateTimeProvider, 
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public virtual async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("Email and Username are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ArgumentException("Email is already registered.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User(
            Guid.NewGuid(),
            request.Username,
            request.Email,
            passwordHash,
            _dateTimeProvider.UtcNow
        );

        await _userRepository.AddAsync(user);

        var token = _jwtProvider.Generate(user);
        var refreshTokenString = GenerateRefreshTokenString();
        var refreshToken = new RefreshToken(
            refreshTokenString,
            user.Id,
            _dateTimeProvider.UtcNow.AddDays(7),
            _dateTimeProvider.UtcNow
        );

        await _refreshTokenRepository.SaveAsync(refreshToken);

        return new AuthResponse(token, refreshTokenString, user.Username, user.Email);
    }

    public virtual async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = _jwtProvider.Generate(user);
        var refreshTokenString = GenerateRefreshTokenString();
        var refreshToken = new RefreshToken(
            refreshTokenString,
            user.Id,
            _dateTimeProvider.UtcNow.AddDays(7),
            _dateTimeProvider.UtcNow
        );

        await _refreshTokenRepository.SaveAsync(refreshToken);

        return new AuthResponse(token, refreshTokenString, user.Username, user.Email);
    }

    public virtual async Task<AuthResponse> RefreshTokensAsync(string refreshTokenString)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenString))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var oldToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenString);
        if (oldToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // Breach Detection (RTR)
        if (oldToken.RevokedAt != null)
        {
            // Token has been used before! Potential replay attack breach.
            // Revoke all tokens for this user.
            await _refreshTokenRepository.RevokeAllForUserAsync(oldToken.UserId);
            throw new UnauthorizedAccessException("Refresh token has already been used. Possible breach detected; revoking all active sessions.");
        }

        if (oldToken.IsExpired(_dateTimeProvider.UtcNow))
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var user = await _userRepository.GetByIdAsync(oldToken.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        // Rotate tokens
        oldToken.Revoke(_dateTimeProvider.UtcNow);
        await _refreshTokenRepository.SaveAsync(oldToken);

        var newAccessToken = _jwtProvider.Generate(user);
        var newRefreshTokenString = GenerateRefreshTokenString();
        
        var newRefreshToken = new RefreshToken(
            newRefreshTokenString,
            user.Id,
            _dateTimeProvider.UtcNow.AddDays(7),
            _dateTimeProvider.UtcNow
        );

        await _refreshTokenRepository.SaveAsync(newRefreshToken);

        return new AuthResponse(newAccessToken, newRefreshTokenString, user.Username, user.Email);
    }

    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
