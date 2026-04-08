using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartTaskManager.Api.Contracts;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Api.Contracts.Requests;

public sealed class CreateTaskRequest : IValidatableObject
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    public DateTime DueDate { get; init; }

    public TaskPriority Priority { get; init; }

    [StringLength(100)]
    public string? CategoryName { get; init; }

    public TaskKind TaskType { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDate == default)
        {
            yield return new ValidationResult("DueDate is required.", new[] { nameof(DueDate) });
        }
    }
}
