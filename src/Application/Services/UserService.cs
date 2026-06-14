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

    public UserService(IUserRepository userRepository, IJwtProvider jwtProvider, IDateTimeProvider dateTimeProvider, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
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

        return new AuthResponse(token, user.Username, user.Email);
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

        return new AuthResponse(token, user.Username, user.Email);
    }


}
