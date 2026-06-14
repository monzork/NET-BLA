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

    public async Task<IEnumerable<TaskItem>> GetPagedByUserIdAsync(
        Guid userId,
        int? limit,
        string? status,
        string? searchQuery,
        DateTime? cursorCreatedAt,
        Guid? cursorId)
    {
        var tasks = new List<TaskItem>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        var selectTop = limit.HasValue ? "SELECT TOP (@Limit)" : "SELECT";
        var sql = $"{selectTop} Id, Title, Description, Status, DueDate, UserId, CreatedAt FROM TaskItems WHERE UserId = @UserId";

        command.Parameters.AddWithValue("@UserId", userId);

        if (limit.HasValue)
        {
            command.Parameters.AddWithValue("@Limit", limit.Value);
        }

        // Apply Status Filter
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskItemStatus>(status, true, out var parsedStatus))
        {
            sql += " AND Status = @Status";
            command.Parameters.AddWithValue("@Status", (int)parsedStatus);
        }

        // Apply Search Filter
        if (!string.IsNullOrEmpty(searchQuery))
        {
            sql += " AND (Title LIKE @SearchQueryLike OR Description LIKE @SearchQueryLike)";
            command.Parameters.AddWithValue("@SearchQueryLike", $"%{searchQuery}%");
        }

        // Apply Cursor Filter
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            sql += " AND (CreatedAt < @CursorCreatedAt OR (CreatedAt = @CursorCreatedAt AND Id < @CursorId))";
            command.Parameters.AddWithValue("@CursorCreatedAt", cursorCreatedAt.Value);
            command.Parameters.AddWithValue("@CursorId", cursorId.Value);
        }

        sql += " ORDER BY CreatedAt DESC, Id DESC";

        command.CommandText = sql;

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
        return TaskItem.CreateFromDatabase(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            (TaskItemStatus)reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            reader.GetGuid(5),
            reader.GetDateTime(6)
        );
    }
}
