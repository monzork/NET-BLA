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
    private readonly TaskService _taskService;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public TaskServiceTests()
    {
        _taskRepository = new MockTaskRepository();
        _currentUserService = new MockCurrentUserService(_currentUserId);
        _taskService = new TaskService(_taskRepository, _currentUserService);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowException_WhenTitleIsEmpty()
    {
        // Arrange
        var dto = new CreateTaskDto("", "Description", "Pending", DateTime.UtcNow.AddDays(1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.CreateTaskAsync(dto));
        Assert.Contains("Title is required", exception.Message);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowException_WhenDueDateIsInPast()
    {
        // Arrange
        var dto = new CreateTaskDto("Task Title", "Description", "Pending", DateTime.UtcNow.AddMinutes(-5));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.CreateTaskAsync(dto));
        Assert.Contains("DueDate cannot be in the past", exception.Message);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowException_WhenStatusIsInvalid()
    {
        // Arrange
        var dto = new CreateTaskDto("Task Title", "Description", "InvalidStatus", DateTime.UtcNow.AddDays(1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.CreateTaskAsync(dto));
        Assert.Contains("Status must be Pending, InProgress, or Completed", exception.Message);
    }

    [Fact]
    public async Task CreateTask_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange
        var dueDate = DateTime.UtcNow.AddDays(1);
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
        var task = new TaskItem
        {
            Id = taskId,
            Title = "Original",
            UserId = _currentUserId,
            Status = Domain.Enums.TaskItemStatus.Pending
        };
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
        var task = new TaskItem
        {
            Id = taskId,
            Title = "Original",
            UserId = _currentUserId,
            Status = Domain.Enums.TaskItemStatus.Pending
        };
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("Updated Title", "Description", "InProgress", DateTime.UtcNow.AddDays(-1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _taskService.UpdateTaskAsync(taskId, dto));
        Assert.Contains("DueDate cannot be in the past", exception.Message);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowException_WhenStatusIsInvalid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskItem
        {
            Id = taskId,
            Title = "Original",
            UserId = _currentUserId,
            Status = Domain.Enums.TaskItemStatus.Pending
        };
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
        var task = new TaskItem
        {
            Id = taskId,
            Title = "Original",
            UserId = anotherUserId,
            Status = Domain.Enums.TaskItemStatus.Pending
        };
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
        var task = new TaskItem
        {
            Id = taskId,
            Title = "Original",
            UserId = _currentUserId,
            Status = Domain.Enums.TaskItemStatus.Pending
        };
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("Updated Title", "New Desc", "Completed", DateTime.UtcNow.AddDays(2));

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
