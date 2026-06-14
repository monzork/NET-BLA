using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[EnableRateLimiting("auth")]
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly IRevokedTokenRepository _revokedTokenRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthController(
        UserService userService, 
        IRevokedTokenRepository revokedTokenRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userService = userService;
        _revokedTokenRepository = revokedTokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _userService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await _userService.RefreshTokensAsync(request.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Message = "Invalid Authorization header." });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var expiryDate = DateTime.UtcNow.AddDays(1); // default fallback

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                expiryDate = jwtToken.ValidTo;
            }
        }
        catch
        {
            // use default expiry date
        }

        await _revokedTokenRepository.RevokeAsync(token, expiryDate);

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(userId);
        }

        return Ok(new { Message = "Logged out successfully." });
    }
}
