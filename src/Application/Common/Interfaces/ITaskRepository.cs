using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(Guid userId);
    Task<IEnumerable<TaskItem>> GetPagedByUserIdAsync(
        Guid userId,
        int? limit,
        string? status,
        string? searchQuery,
        DateTime? cursorCreatedAt,
        Guid? cursorId);
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task CreateAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
}
