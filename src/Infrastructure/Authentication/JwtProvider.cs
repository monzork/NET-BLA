using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

public class JwtProvider : IJwtProvider
{
    private readonly IConfiguration _configuration;

    public JwtProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(User user)
    {
        var secretKey = _configuration["Jwt:Secret"] ?? "SuperSecretKeyForTaskManagementApplicationCleanArchitecture2026";
        var issuer = _configuration["Jwt:Issuer"] ?? "TaskAppAPI";
        var audience = _configuration["Jwt:Audience"] ?? "TaskAppClient";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            null,
            DateTime.UtcNow.AddDays(7),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
