using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

public class TaskRepository : ITaskRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        var tasks = new List<TaskItem>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Description, Status, DueDate, UserId, CreatedAt FROM TaskItems";

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(MapReaderToTask(reader));
        }

        return tasks;
    }

    public async Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(Guid userId)
    {
        var tasks = new List<TaskItem>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Description, Status, DueDate, UserId, CreatedAt FROM TaskItems WHERE UserId = @UserId ORDER BY CreatedAt DESC";
        command.Parameters.AddWithValue("@UserId", userId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(MapReaderToTask(reader));
        }

        return tasks;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Description, Status, DueDate, UserId, CreatedAt FROM TaskItems WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReaderToTask(reader);
        }

        return null;
    }

    public async Task CreateAsync(TaskItem task)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO TaskItems (Id, Title, Description, Status, DueDate, UserId, CreatedAt)
            VALUES (@Id, @Title, @Description, @Status, @DueDate, @UserId, @CreatedAt)";

        command.Parameters.AddWithValue("@Id", task.Id);
        command.Parameters.AddWithValue("@Title", task.Title);
        command.Parameters.AddWithValue("@Description", task.Description);
        command.Parameters.AddWithValue("@Status", (int)task.Status);
        command.Parameters.AddWithValue("@DueDate", (object?)task.DueDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserId", task.UserId);
        command.Parameters.AddWithValue("@CreatedAt", task.CreatedAt);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(TaskItem task)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE TaskItems 
            SET Title = @Title, Description = @Description, Status = @Status, DueDate = @DueDate 
            WHERE Id = @Id";

        command.Parameters.AddWithValue("@Id", task.Id);
        command.Parameters.AddWithValue("@Title", task.Title);
        command.Parameters.AddWithValue("@Description", task.Description);
        command.Parameters.AddWithValue("@Status", (int)task.Status);
        command.Parameters.AddWithValue("@DueDate", (object?)task.DueDate ?? DBNull.Value);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM TaskItems WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    private TaskItem MapReaderToTask(SqlDataReader reader)
    {
        return new TaskItem
        {
            Id = reader.GetGuid(0),
            Title = reader.GetString(1),
            Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Status = (TaskItemStatus)reader.GetInt32(3),
            DueDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            UserId = reader.GetGuid(5),
            CreatedAt = reader.GetDateTime(6)
        };
    }
}
