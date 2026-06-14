using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Controllers;
using Application.Common.Interfaces;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Application.UnitTests.Controllers;

public class TasksControllerTests
{
    private readonly Services.MockTaskRepository _taskRepository;
    private readonly Services.MockCurrentUserService _currentUserService;
    private readonly TaskService _taskService;
    private readonly TasksController _tasksController;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public TasksControllerTests()
    {
        _taskRepository = new Services.MockTaskRepository();
        _currentUserService = new Services.MockCurrentUserService(_currentUserId);
        _taskService = new TaskService(_taskRepository, _currentUserService, new Services.MockDateTimeProvider());
        _tasksController = new TasksController(_taskService);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithUserTasks()
    {
        // Arrange
        var task1 = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Task 1", "", TaskItemStatus.Pending, null, _currentUserId, DateTime.UtcNow);
        var task2 = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Task 2", "", TaskItemStatus.InProgress, null, _currentUserId, DateTime.UtcNow);
        var otherUserTask = TaskItem.CreateFromDatabase(Guid.NewGuid(), "Other Task", "", TaskItemStatus.Completed, null, Guid.NewGuid(), DateTime.UtcNow);
        
        _taskRepository.Tasks.Add(task1);
        _taskRepository.Tasks.Add(task2);
        _taskRepository.Tasks.Add(otherUserTask);

        // Act
        var result = await _tasksController.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskDto>>(okResult.Value);
        Assert.Equal(2, tasks.Count());
        Assert.All(tasks, t => Assert.Equal(_currentUserId, t.UserId));
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenTaskExistsForUser()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "My Task", "", TaskItemStatus.Pending, null, _currentUserId, DateTime.UtcNow);
        _taskRepository.Tasks.Add(task);

        // Act
        var result = await _tasksController.GetById(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TaskDto>(okResult.Value);
        Assert.Equal(taskId, response.Id);
        Assert.Equal("My Task", response.Title);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        // Act
        var result = await _tasksController.GetById(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WithSavedTask()
    {
        // Arrange
        var dto = new CreateTaskDto("New Task", "Description", "Pending", DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _tasksController.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<TaskDto>(createdResult.Value);
        Assert.Equal("New Task", response.Title);
        Assert.Equal("Pending", response.Status);
        Assert.Equal(_currentUserId, response.UserId);
        
        Assert.Contains(_taskRepository.Tasks, t => t.Title == "New Task" && t.UserId == _currentUserId);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WithUpdatedTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "Old Title", "", TaskItemStatus.Pending, null, _currentUserId, DateTime.UtcNow);
        _taskRepository.Tasks.Add(task);

        var dto = new UpdateTaskDto("Updated Title", "Description", "Completed", DateTime.UtcNow.AddDays(2));

        // Act
        var result = await _tasksController.Update(taskId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TaskDto>(okResult.Value);
        Assert.Equal("Updated Title", response.Title);
        Assert.Equal("Completed", response.Status);

        var updatedTaskInDb = _taskRepository.Tasks.First(t => t.Id == taskId);
        Assert.Equal("Updated Title", updatedTaskInDb.Title);
        Assert.Equal(TaskItemStatus.Completed, updatedTaskInDb.Status);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_AndRemoveTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskItem.CreateFromDatabase(taskId, "To Delete", "", TaskItemStatus.Pending, null, _currentUserId, DateTime.UtcNow);
        _taskRepository.Tasks.Add(task);

        // Act
        var result = await _tasksController.Delete(taskId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.DoesNotContain(_taskRepository.Tasks, t => t.Id == taskId);
    }
}
