using System;
using Domain.Enums;

namespace Domain.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Pending;
    public DateTime? DueDate { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Required for ORM/deserialization or parameterless reflection
    private TaskItem() { }

    public TaskItem(Guid id, string title, string description, TaskItemStatus status, DateTime? dueDate, Guid userId, DateTime createdAt, DateTime currentUtcTime)
    {
        ValidateTaskInvariants(title, dueDate, currentUtcTime);

        Id = id;
        Title = title;
        Description = description ?? string.Empty;
        Status = status;
        DueDate = dueDate;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public void Update(string title, string description, TaskItemStatus status, DateTime? dueDate, DateTime currentUtcTime)
    {
        ValidateTaskInvariants(title, dueDate, currentUtcTime);

        Title = title;
        Description = description ?? string.Empty;
        Status = status;
        DueDate = dueDate;
    }

    public void Complete()
    {
        Status = TaskItemStatus.Completed;
    }

    public static TaskItem CreateFromDatabase(Guid id, string title, string description, TaskItemStatus status, DateTime? dueDate, Guid userId, DateTime createdAt)
    {
        return new TaskItem
        {
            Id = id,
            Title = title,
            Description = description ?? string.Empty,
            Status = status,
            DueDate = dueDate,
            UserId = userId,
            CreatedAt = createdAt
        };
    }

    private void ValidateTaskInvariants(string title, DateTime? dueDate, DateTime currentUtcTime)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (dueDate.HasValue && dueDate.Value < currentUtcTime)
        {
            throw new ArgumentException("DueDate cannot be in the past.");
        }
    }
}
