using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.UnitTests.Services;

public class TaskServiceTests
{
    private readonly MockTaskRepository _taskRepository;
    private readonly MockCurrentUserService _currentUserService;
    private readonly MockDateTimeProvider _dateTimeProvider;
    private readonly TaskService _taskService;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly DateTime _frozenTime = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

    public TaskServiceTests()
    {
        _taskRepository = new MockTaskRepository();
        _currentUserService = new MockCurrentUserService(_currentUserId);
        _dateTimeProvider = new MockDateTimeProvider { UtcNow = _frozenTime };
        _taskService = new TaskService(_taskRepository, _currentUserService, _dateTimeProvider);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowException_WhenTitleIsEmpty()
    {
        // Arrange
        var dto = new CreateTaskDto("", "Description", "Pending", _frozenTime.AddDays(1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.CreateTaskAsync(dto));
        Assert.Contains("Title is required", exception.Message);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowException_WhenDueDateIsInPast()
    {
        // Arrange
        var dto = new CreateTaskDto("Task Title", "Description", "Pending", _frozenTime.AddMinutes(-5));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.CreateTaskAsync(dto));
        Assert.Contains("DueDate cannot be in the past", exception.Message);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowException_WhenStatusIsInvalid()
    {
        // Arrange
        var dto = new CreateTaskDto("Task Title", "Description", "InvalidStatus", _frozenTime.AddDays(1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.CreateTaskAsync(dto));
        Assert.Contains("Status must be Pending, InProgress, or Completed", exception.Message);
    }

    [Fact]
    public async Task CreateTask_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange
        var dueDate = _frozenTime.AddDays(1);
        var dto = new CreateTaskDto("Task Title", "Description", "Pending", dueDate);

        // Act
        var result = await _taskService.CreateTaskAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.Status, result.Status);
        Assert.Equal(dto.DueDate, result.DueDate);
        Assert.Equal(_currentUserId, result.UserId);

        var savedTask = _taskRepository.Tasks.FirstOrDefault(t => t.Id == result.Id);
        Assert.NotNull(savedTask);
        Assert.Equal(dto.Title, savedTask.Title);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowException_WhenTitleIsEmpty()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "Original", "Description", Domain.Enums.TaskItemStatus.Pending, null, _currentUserId, _frozenTime);
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("", "Description", "InProgress", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.UpdateTaskAsync(taskId, dto));
        Assert.Contains("Title is required", exception.Message);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowException_WhenDueDateIsInPast()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "Original", "Description", Domain.Enums.TaskItemStatus.Pending, null, _currentUserId, _frozenTime);
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("Updated Title", "Description", "InProgress", _frozenTime.AddDays(-1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.UpdateTaskAsync(taskId, dto));
        Assert.Contains("DueDate cannot be in the past", exception.Message);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowException_WhenStatusIsInvalid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "Original", "Description", Domain.Enums.TaskItemStatus.Pending, null, _currentUserId, _frozenTime);
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("Updated Title", "Description", "InvalidStatus", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.UpdateTaskAsync(taskId, dto));
        Assert.Contains("Status must be Pending, InProgress, or Completed", exception.Message);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowException_WhenTaskDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var dto = new UpdateTaskDto("Updated Title", "Description", "InProgress", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _taskService.UpdateTaskAsync(nonExistentId, dto));
        Assert.Contains("Task not found", exception.Message);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowException_WhenUserDoesNotOwnTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "Original", "Description", Domain.Enums.TaskItemStatus.Pending, null, anotherUserId, _frozenTime);
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("Updated Title", "Description", "InProgress", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _taskService.UpdateTaskAsync(taskId, dto));
        Assert.Contains("You do not have permission to access this task", exception.Message);
    }

    [Fact]
    public async Task UpdateTask_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "Original", "Description", Domain.Enums.TaskItemStatus.Pending, null, _currentUserId, _frozenTime);
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("Updated Title", "New Desc", "Completed", _frozenTime.AddDays(2));

        // Act
        var result = await _taskService.UpdateTaskAsync(taskId, dto);

        // Assert
        Assert.Equal(taskId, result.Id);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.Status, result.Status);
        Assert.Equal(dto.DueDate, result.DueDate);

        var savedTask = _taskRepository.Tasks.First(t => t.Id == taskId);
        Assert.Equal(dto.Title, savedTask.Title);
        Assert.Equal(Domain.Enums.TaskItemStatus.Completed, savedTask.Status);
    }

    [Fact]
    public async Task GetTasksPaged_ShouldReturnAllTasks_WhenLimitIsNull()
    {
        // Arrange
        var task1 = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Task 1", "", TaskItemStatus.Pending, null, _currentUserId, _frozenTime.AddHours(-1));
        var task2 = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Task 2", "", TaskItemStatus.InProgress, null, _currentUserId, _frozenTime);
        _taskRepository.Tasks.Add(task1);
        _taskRepository.Tasks.Add(task2);

        // Act
        var result = await _taskService.GetTasksPagedForCurrentUserAsync(null, null, null, null);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.Null(result.NextCursor);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetTasksPaged_ShouldReturnPagedTasks_WhenLimitIsSet()
    {
        // Arrange
        var task1 = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Task 1", "", TaskItemStatus.Pending, null, _currentUserId, _frozenTime.AddHours(-2));
        var task2 = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Task 2", "", TaskItemStatus.InProgress, null, _currentUserId, _frozenTime.AddHours(-1));
        var task3 = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Task 3", "", TaskItemStatus.Completed, null, _currentUserId, _frozenTime);
        _taskRepository.Tasks.Add(task1);
        _taskRepository.Tasks.Add(task2);
        _taskRepository.Tasks.Add(task3);

        // Act - Page 1
        var page1 = await _taskService.GetTasksPagedForCurrentUserAsync(2, null, null, null);

        // Assert Page 1
        Assert.Equal(2, page1.Items.Count());
        Assert.True(page1.HasMore);
        Assert.NotNull(page1.NextCursor);

        // Act - Page 2
        var page2 = await _taskService.GetTasksPagedForCurrentUserAsync(2, page1.NextCursor, null, null);

        // Assert Page 2
        Assert.Single(page2.Items);
        Assert.False(page2.HasMore);
        Assert.Null(page2.NextCursor);
    }
}

#region Mocks

public class MockTaskRepository : ITaskRepository
{
    public List<TaskItem> Tasks { get; } = new();

    public Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<TaskItem>>(Tasks);
    }

    public Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(Guid userId)
    {
        return Task.FromResult<IEnumerable<TaskItem>>(Tasks.Where(t => t.UserId == userId).ToList());
    }

    public Task<IEnumerable<TaskItem>> GetPagedByUserIdAsync(
        Guid userId,
        int? limit,
        string? status,
        string? searchQuery,
        DateTime? cursorCreatedAt,
        Guid? cursorId)
    {
        var query = Tasks.Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.TaskItemStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(t => t.Status == parsedStatus);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            var sq = searchQuery.ToLowerInvariant();
            query = query.Where(t => t.Title.ToLowerInvariant().Contains(sq) || t.Description.ToLowerInvariant().Contains(sq));
        }

        query = query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(t => 
                t.CreatedAt < cursorCreatedAt.Value 
                || (t.CreatedAt == cursorCreatedAt.Value && t.Id.CompareTo(cursorId.Value) < 0));
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return Task.FromResult<IEnumerable<TaskItem>>(query.ToList());
    }

    public Task<TaskItem?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(Tasks.FirstOrDefault(t => t.Id == id));
    }

    public Task CreateAsync(TaskItem task)
    {
        Tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem task)
    {
        var existing = Tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing != null)
        {
            Tasks.Remove(existing);
            Tasks.Add(task);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var existing = Tasks.FirstOrDefault(t => t.Id == id);
        if (existing != null)
        {
            Tasks.Remove(existing);
        }
        return Task.CompletedTask;
    }
}

public class MockCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; }

    public MockCurrentUserService(Guid? userId)
    {
        UserId = userId;
    }
}

#endregion
