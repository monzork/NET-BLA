using System;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RefreshTokenRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(RefreshToken token)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            IF EXISTS (SELECT 1 FROM RefreshTokens WHERE Token = @Token)
            BEGIN
                UPDATE RefreshTokens
                SET RevokedAt = @RevokedAt
                WHERE Token = @Token;
            END
            ELSE
            BEGIN
                INSERT INTO RefreshTokens (Token, UserId, ExpiryDate, CreatedAt, RevokedAt)
                VALUES (@Token, @UserId, @ExpiryDate, @CreatedAt, @RevokedAt);
            END;";
        
        command.Parameters.AddWithValue("@Token", token.Token);
        command.Parameters.AddWithValue("@UserId", token.UserId);
        command.Parameters.AddWithValue("@ExpiryDate", token.ExpiryDate);
        command.Parameters.AddWithValue("@CreatedAt", token.CreatedAt);
        command.Parameters.AddWithValue("@RevokedAt", (object?)token.RevokedAt ?? DBNull.Value);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Token, UserId, ExpiryDate, CreatedAt, RevokedAt FROM RefreshTokens WHERE Token = @Token";
        command.Parameters.AddWithValue("@Token", token);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return RefreshToken.CreateFromDatabase(
                reader.GetString(0),
                reader.GetGuid(1),
                reader.GetDateTime(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4)
            );
        }

        return null;
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE RefreshTokens SET RevokedAt = @RevokedAt WHERE UserId = @UserId AND RevokedAt IS NULL";
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}
