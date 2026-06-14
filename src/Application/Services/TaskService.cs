using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class TaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;

    public TaskService(ITaskRepository taskRepository, ICurrentUserService currentUserService)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
    }

    public virtual async Task<IEnumerable<TaskDto>> GetTasksForCurrentUserAsync()
    {
        var userId = GetCurrentUserIdOrThrow();
        var tasks = await _taskRepository.GetAllByUserIdAsync(userId);
        return tasks.Select(MapToDto);
    }

    public virtual async Task<TaskDto?> GetTaskByIdAsync(Guid id)
    {
        var userId = GetCurrentUserIdOrThrow();
        var task = await _taskRepository.GetByIdAsync(id);
        
        if (task == null) return null;

        if (task.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this task.");
        }

        return MapToDto(task);
    }

    public virtual async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto)
    {
        var userId = GetCurrentUserIdOrThrow();
        ValidateTaskData(dto.Title, dto.DueDate, dto.Status);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description ?? string.Empty,
            Status = Enum.Parse<TaskItemStatus>(dto.Status, true),
            DueDate = dto.DueDate,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.CreateAsync(task);
        return MapToDto(task);
    }

    public virtual async Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto dto)
    {
        var userId = GetCurrentUserIdOrThrow();
        var task = await _taskRepository.GetByIdAsync(id);

        if (task == null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        if (task.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this task.");
        }

        ValidateTaskData(dto.Title, dto.DueDate, dto.Status);

        task.Title = dto.Title;
        task.Description = dto.Description ?? string.Empty;
        task.Status = Enum.Parse<TaskItemStatus>(dto.Status, true);
        task.DueDate = dto.DueDate;

        await _taskRepository.UpdateAsync(task);
        return MapToDto(task);
    }

    public virtual async Task DeleteTaskAsync(Guid id)
    {
        var userId = GetCurrentUserIdOrThrow();
        var task = await _taskRepository.GetByIdAsync(id);

        if (task == null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        if (task.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this task.");
        }

        await _taskRepository.DeleteAsync(id);
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return userId.Value;
    }

    private void ValidateTaskData(string title, DateTime? dueDate, string status)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (dueDate.HasValue && dueDate.Value < DateTime.UtcNow)
        {
            // Give a tiny tolerance of 2 seconds to avoid race conditions in fast tests
            if (DateTime.UtcNow.Subtract(dueDate.Value).TotalSeconds > 2)
            {
                throw new ArgumentException("DueDate cannot be in the past.");
            }
        }

        if (string.IsNullOrWhiteSpace(status) || 
            (!Enum.TryParse<TaskItemStatus>(status, true, out var parsedStatus) || 
             !Enum.IsDefined(typeof(TaskItemStatus), parsedStatus)))
        {
            throw new ArgumentException("Status must be Pending, InProgress, or Completed.");
        }
    }

    private TaskDto MapToDto(TaskItem task)
    {
        return new TaskDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.DueDate,
            task.UserId,
            task.CreatedAt);
    }
}
