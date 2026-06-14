using System;
using System.Data;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

public class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, Email, PasswordHash, CreatedAt FROM Users WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReaderToUser(reader);
        }

        return null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, Email, PasswordHash, CreatedAt FROM Users WHERE Email = @Email";
        command.Parameters.AddWithValue("@Email", email);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReaderToUser(reader);
        }

        return null;
    }

    public async Task AddAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Users (Id, Username, Email, PasswordHash, CreatedAt) 
            VALUES (@Id, @Username, @Email, @PasswordHash, @CreatedAt)";
        
        command.Parameters.AddWithValue("@Id", user.Id);
        command.Parameters.AddWithValue("@Username", user.Username);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    private User MapReaderToUser(SqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            CreatedAt = reader.GetDateTime(4)
        };
    }
}
