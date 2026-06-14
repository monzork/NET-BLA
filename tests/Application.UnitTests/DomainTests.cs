using System;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.UnitTests;

public class DomainTests
{
    [Fact]
    public void UserConstructor_ShouldThrowException_WhenUsernameIsBlank()
    {
        Assert.Throws<ArgumentException>(() => 
            new User(Guid.NewGuid(), "  ", "email@test.com", "hash", DateTime.UtcNow));
    }

    [Fact]
    public void UserConstructor_ShouldThrowException_WhenEmailIsBlank()
    {
        Assert.Throws<ArgumentException>(() => 
            new User(Guid.NewGuid(), "username", "", "hash", DateTime.UtcNow));
    }

    [Fact]
    public void UserConstructor_ShouldThrowException_WhenPasswordHashIsBlank()
    {
        Assert.Throws<ArgumentException>(() => 
            new User(Guid.NewGuid(), "username", "email@test.com", null!, DateTime.UtcNow));
    }

    [Fact]
    public void TaskItemConstructor_ShouldThrowException_WhenTitleIsBlank()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => 
            new TaskItem(Guid.NewGuid(), "", "desc", TaskItemStatus.Pending, null, Guid.NewGuid(), now, now));
    }

    [Fact]
    public void TaskItemConstructor_ShouldThrowException_WhenDueDateIsInPast()
    {
        var now = DateTime.UtcNow;
        var past = now.AddMinutes(-5);
        Assert.Throws<ArgumentException>(() => 
            new TaskItem(Guid.NewGuid(), "Title", "desc", TaskItemStatus.Pending, past, Guid.NewGuid(), now, now));
    }

    [Fact]
    public void TaskItemUpdate_ShouldThrowException_WhenTitleIsBlank()
    {
        var now = DateTime.UtcNow;
        var task = new TaskItem(Guid.NewGuid(), "Title", "desc", TaskItemStatus.Pending, null, Guid.NewGuid(), now, now);
        
        Assert.Throws<ArgumentException>(() => 
            task.Update("", "desc", TaskItemStatus.Pending, null, now));
    }

    [Fact]
    public void TaskItemUpdate_ShouldThrowException_WhenDueDateIsInPast()
    {
        var now = DateTime.UtcNow;
        var task = new TaskItem(Guid.NewGuid(), "Title", "desc", TaskItemStatus.Pending, null, Guid.NewGuid(), now, now);
        var past = now.AddMinutes(-5);

        Assert.Throws<ArgumentException>(() => 
            task.Update("Title", "desc", TaskItemStatus.Pending, past, now));
    }

    [Fact]
    public void TaskItemComplete_ShouldChangeStatusToCompleted()
    {
        var now = DateTime.UtcNow;
        var task = new TaskItem(Guid.NewGuid(), "Title", "desc", TaskItemStatus.Pending, null, Guid.NewGuid(), now, now);
        
        task.Complete();

        Assert.Equal(TaskItemStatus.Completed, task.Status);
    }

    [Fact]
    public void RefreshTokenConstructor_ShouldThrowException_WhenTokenIsBlank()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => 
            new RefreshToken("  ", Guid.NewGuid(), now.AddDays(1), now));
    }

    [Fact]
    public void RefreshTokenConstructor_ShouldThrowException_WhenUserIdIsEmpty()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => 
            new RefreshToken("token", Guid.Empty, now.AddDays(1), now));
    }

    [Fact]
    public void RefreshTokenConstructor_ShouldThrowException_WhenExpiryIsBeforeCreated()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => 
            new RefreshToken("token", Guid.NewGuid(), now.AddMinutes(-5), now));
    }

    [Fact]
    public void RefreshTokenIsExpired_ShouldReturnTrue_WhenExpiryDateIsPast()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.CreateFromDatabase("token", Guid.NewGuid(), now.AddMinutes(-5), now.AddDays(-1), null);
        
        Assert.True(token.IsExpired(now));
        Assert.False(token.IsActive(now));
    }

    [Fact]
    public void RefreshTokenIsActive_ShouldReturnFalse_WhenRevoked()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.CreateFromDatabase("token", Guid.NewGuid(), now.AddDays(1), now, now);
        
        Assert.False(token.IsActive(now));
    }
}
