using System;

namespace Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTime? DueDate,
    Guid UserId,
    DateTime CreatedAt);

public record CreateTaskDto(
    string Title,
    string Description,
    string Status,
    DateTime? DueDate);

public record UpdateTaskDto(
    string Title,
    string Description,
    string Status,
    DateTime? DueDate);
