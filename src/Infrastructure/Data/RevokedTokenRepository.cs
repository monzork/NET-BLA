using System;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

public class RevokedTokenRepository : IRevokedTokenRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RevokedTokenRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RevokeAsync(string token, DateTime expiryDate)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            IF NOT EXISTS (SELECT 1 FROM RevokedTokens WHERE Token = @Token)
            BEGIN
                INSERT INTO RevokedTokens (Token, ExpiryDate)
                VALUES (@Token, @ExpiryDate);
            END;";
        command.Parameters.AddWithValue("@Token", token);
        command.Parameters.AddWithValue("@ExpiryDate", expiryDate);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> IsRevokedAsync(string token)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM RevokedTokens WHERE Token = @Token";
        command.Parameters.AddWithValue("@Token", token);

        await connection.OpenAsync();
        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }
}
