using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManager.Api.Contracts;
using SmartTaskManager.Api.Contracts.Requests;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Application.DTOs;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;
using DomainTaskStatus = SmartTaskManager.Domain.Enums.TaskStatus;

namespace SmartTaskManager.Api.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskSummary), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TaskSummary>> CreateTask(
        [FromRoute] Guid userId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await CreateTaskByTypeAsync(userId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetTask),
            new { userId, taskId = task.Id },
            task);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskSummary>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyCollection<TaskSummary>>> ListTasks(
        [FromRoute] Guid userId,
        [FromQuery] DomainTaskStatus? status,
        [FromQuery] TaskPriority? priority,
        [FromQuery] bool overdue,
        CancellationToken cancellationToken)
    {
        ActionResult? validationResult = ValidateFilterQuery(status, priority, overdue);
        if (validationResult is not null)
        {
            return validationResult;
        }

        IReadOnlyCollection<TaskSummary> tasks = await SelectTaskListAsync(
            userId,
            status,
            priority,
            overdue,
            cancellationToken);

        return Ok(tasks);
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskSummary), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TaskSummary>> GetTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.GetTaskAsync(userId, taskId, cancellationToken);
        return Ok(task);
    }

    [HttpPatch("{taskId:guid}/priority")]
    [ProducesResponseType(typeof(TaskSummary), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TaskSummary>> UpdateTaskPriority(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.UpdateTaskPriorityAsync(
            userId,
            taskId,
            request.Priority,
            cancellationToken);

        return Ok(task);
    }

    [HttpPatch("{taskId:guid}/complete")]
    [ProducesResponseType(typeof(TaskSummary), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TaskSummary>> CompleteTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.MarkTaskAsCompletedAsync(userId, taskId, cancellationToken);
        return Ok(task);
    }

    [HttpPatch("{taskId:guid}/archive")]
    [ProducesResponseType(typeof(TaskSummary), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TaskSummary>> ArchiveTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.ArchiveTaskAsync(userId, taskId, cancellationToken);
        return Ok(task);
    }

    [HttpGet("{taskId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<HistoryEntryResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyCollection<HistoryEntryResponse>>> ListTaskHistory(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<HistoryEntry> history = await _taskService.GetTaskHistoryAsync(
            userId,
            taskId,
            cancellationToken);

        return Ok(history.Select(HistoryEntryResponse.FromDomain).ToList());
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetDashboard(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        TaskDashboardSummary summary = await _taskService.GetDashboardSummaryAsync(userId, cancellationToken);
        return Ok(DashboardSummaryResponse.FromApplication(summary));
    }

    private Task<TaskSummary> CreateTaskByTypeAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        return request.TaskType switch
        {
            TaskKind.Personal => _taskService.CreatePersonalTaskAsync(
                userId,
                request.Title,
                request.Description,
                request.DueDate,
                request.Priority,
                request.CategoryName,
                cancellationToken),
            TaskKind.Work => _taskService.CreateWorkTaskAsync(
                userId,
                request.Title,
                request.Description,
                request.DueDate,
                request.Priority,
                request.CategoryName,
                cancellationToken),
            TaskKind.Learning => _taskService.CreateLearningTaskAsync(
                userId,
                request.Title,
                request.Description,
                request.DueDate,
                request.Priority,
                request.CategoryName,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request.TaskType), request.TaskType, "Task type is invalid.")
        };
    }

    private async Task<IReadOnlyCollection<TaskSummary>> SelectTaskListAsync(
        Guid userId,
        DomainTaskStatus? status,
        TaskPriority? priority,
        bool overdue,
        CancellationToken cancellationToken)
    {
        if (status.HasValue)
        {
            return await _taskService.FilterTasksByStatusAsync(userId, status.Value, cancellationToken);
        }

        if (priority.HasValue)
        {
            return await _taskService.FilterTasksByPriorityAsync(userId, priority.Value, cancellationToken);
        }

        if (overdue)
        {
            return await _taskService.GetOverdueTasksAsync(userId, cancellationToken: cancellationToken);
        }

        return await _taskService.ListTasksAsync(userId, cancellationToken);
    }

    private ActionResult? ValidateFilterQuery(
        DomainTaskStatus? status,
        TaskPriority? priority,
        bool overdue)
    {
        int selectedFilters = 0;

        if (status.HasValue)
        {
            selectedFilters++;
        }

        if (priority.HasValue)
        {
            selectedFilters++;
        }

        if (overdue)
        {
            selectedFilters++;
        }

        if (selectedFilters <= 1)
        {
            return null;
        }

        ModelState.AddModelError(
            "filters",
            "Use only one task filter at a time: status, priority, or overdue.");

        return ValidationProblem(ModelState);
    }
}
