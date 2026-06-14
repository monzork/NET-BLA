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
    private readonly IDateTimeProvider _dateTimeProvider;

    public TaskService(ITaskRepository taskRepository, ICurrentUserService currentUserService, IDateTimeProvider dateTimeProvider)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
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
        var parsedStatus = ParseStatusOrThrow(dto.Status);

        var task = new TaskItem(
            Guid.NewGuid(),
            dto.Title,
            dto.Description ?? string.Empty,
            parsedStatus,
            dto.DueDate,
            userId,
            _dateTimeProvider.UtcNow,
            _dateTimeProvider.UtcNow
        );

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

        var parsedStatus = ParseStatusOrThrow(dto.Status);
        task.Update(
            dto.Title,
            dto.Description ?? string.Empty,
            parsedStatus,
            dto.DueDate,
            _dateTimeProvider.UtcNow
        );

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

    private TaskItemStatus ParseStatusOrThrow(string status)
    {
        if (string.IsNullOrWhiteSpace(status) || 
            (!Enum.TryParse<TaskItemStatus>(status, true, out var parsedStatus) || 
             !Enum.IsDefined(typeof(TaskItemStatus), parsedStatus)))
        {
            throw new ArgumentException("Status must be Pending, InProgress, or Completed.");
        }
        return parsedStatus;
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
