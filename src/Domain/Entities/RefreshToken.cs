using System;

namespace Domain.Entities;

public class RefreshToken
{
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken() { }

    public RefreshToken(string token, Guid userId, DateTime expiryDate, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.");
        }

        if (expiryDate <= createdAt)
        {
            throw new ArgumentException("ExpiryDate must be in the future relative to CreatedAt.");
        }

        Token = token;
        UserId = userId;
        ExpiryDate = expiryDate;
        CreatedAt = createdAt;
    }

    public bool IsExpired(DateTime currentUtcTime)
    {
        return ExpiryDate < currentUtcTime;
    }

    public bool IsActive(DateTime currentUtcTime)
    {
        return RevokedAt == null && !IsExpired(currentUtcTime);
    }

    public void Revoke(DateTime currentUtcTime)
    {
        if (RevokedAt == null)
        {
            RevokedAt = currentUtcTime;
        }
    }

    public static RefreshToken CreateFromDatabase(string token, Guid userId, DateTime expiryDate, DateTime createdAt, DateTime? revokedAt)
    {
        return new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiryDate = expiryDate,
            CreatedAt = createdAt,
            RevokedAt = revokedAt
        };
    }
}
