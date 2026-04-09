using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManager.Api.Contracts;
using SmartTaskManager.Api.Contracts.Requests;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Application.DTOs;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Records;

namespace SmartTaskManager.Api.Controllers;

/// <summary>
/// Manages task creation, lifecycle actions, history, filters, and dashboard summaries.
/// </summary>
[ApiController]
[Authorize]
[Tags("Tasks")]
[Route("api/users/{userId:guid}/tasks")]
[ProducesResponseType(typeof(ApiErrorResponse), 401)]
[ProducesResponseType(typeof(ApiErrorResponse), 403)]
public sealed class TasksController : ControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
    }

    /// <summary>
    /// Creates a new task for the selected user.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="request">The task payload describing title, type, due date, and priority.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="201">The task was created successfully.</response>
    /// <response code="400">The request payload is invalid.</response>
    /// <response code="404">The user was not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> CreateTask(
        [FromRoute] Guid userId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await CreateTaskByTypeAsync(userId, request, cancellationToken);
        TaskResponse response = TaskResponse.FromApplication(task);

        return CreatedAtAction(
            nameof(GetTask),
            new { userId, taskId = response.Id },
            response);
    }

    /// <summary>
    /// Returns a user's tasks, optionally filtered by status, priority, or overdue state.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="request">Optional query filters. Use only one filter at a time.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The tasks were returned successfully.</response>
    /// <response code="400">The query string contains invalid filters.</response>
    /// <response code="404">The user was not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<IReadOnlyCollection<TaskResponse>>> ListTasks(
        [FromRoute] Guid userId,
        [FromQuery] TaskListQueryRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TaskSummary> tasks = await ListRequestedTasksAsync(userId, request, cancellationToken);

        return Ok(TaskResponse.FromApplication(tasks));
    }

    /// <summary>
    /// Returns a single task by identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The task was found and returned.</response>
    /// <response code="404">The user or task was not found.</response>
    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> GetTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.GetTaskAsync(userId, taskId, cancellationToken);
        return Ok(TaskResponse.FromApplication(task));
    }

    /// <summary>
    /// Updates the priority of a task.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="request">The new priority value.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The task priority was updated successfully.</response>
    /// <response code="400">The request payload is invalid.</response>
    /// <response code="404">The user or task was not found.</response>
    [HttpPatch("{taskId:guid}/priority")]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> UpdateTaskPriority(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.UpdateTaskPriorityAsync(
            userId,
            taskId,
            request.Priority!.Value,
            cancellationToken);

        return Ok(TaskResponse.FromApplication(task));
    }

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The task was marked as completed.</response>
    /// <response code="400">The task cannot be completed in its current state.</response>
    /// <response code="404">The user or task was not found.</response>
    [HttpPatch("{taskId:guid}/complete")]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> CompleteTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.MarkTaskAsCompletedAsync(userId, taskId, cancellationToken);
        return Ok(TaskResponse.FromApplication(task));
    }

    /// <summary>
    /// Archives a task while preserving its history.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The task was archived successfully.</response>
    /// <response code="400">The task cannot be archived in its current state.</response>
    /// <response code="404">The user or task was not found.</response>
    [HttpPatch("{taskId:guid}/archive")]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> ArchiveTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.ArchiveTaskAsync(userId, taskId, cancellationToken);
        return Ok(TaskResponse.FromApplication(task));
    }

    /// <summary>
    /// Returns the full history of a task.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The task history was returned successfully.</response>
    /// <response code="404">The user or task was not found.</response>
    [HttpGet("{taskId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<HistoryEntryResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<IReadOnlyCollection<HistoryEntryResponse>>> ListTaskHistory(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<HistoryEntry> history = await _taskService.GetTaskHistoryAsync(
            userId,
            taskId,
            cancellationToken);

        return Ok(HistoryEntryResponse.FromDomain(history));
    }

    /// <summary>
    /// Returns a dashboard summary for the selected user's tasks.
    /// </summary>
    /// <param name="userId">The unique identifier of the owning user.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The dashboard summary was returned successfully.</response>
    /// <response code="404">The user was not found.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
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
        string description = request.Description ?? string.Empty;
        DateTime dueDate = request.DueDate!.Value;
        SmartTaskManager.Domain.Enums.TaskPriority priority = request.Priority!.Value;

        return request.TaskType!.Value switch
        {
            TaskKind.Personal => _taskService.CreatePersonalTaskAsync(
                userId,
                request.Title,
                description,
                dueDate,
                priority,
                request.CategoryName,
                cancellationToken),
            TaskKind.Work => _taskService.CreateWorkTaskAsync(
                userId,
                request.Title,
                description,
                dueDate,
                priority,
                request.CategoryName,
                cancellationToken),
            TaskKind.Learning => _taskService.CreateLearningTaskAsync(
                userId,
                request.Title,
                description,
                dueDate,
                priority,
                request.CategoryName,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request.TaskType), request.TaskType, "Task type is invalid.")
        };
    }

    private async Task<IReadOnlyCollection<TaskSummary>> ListRequestedTasksAsync(
        Guid userId,
        TaskListQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status.HasValue)
        {
            return await _taskService.FilterTasksByStatusAsync(userId, request.Status.Value, cancellationToken);
        }

        if (request.Priority.HasValue)
        {
            return await _taskService.FilterTasksByPriorityAsync(userId, request.Priority.Value, cancellationToken);
        }

        if (request.Overdue)
        {
            return await _taskService.GetOverdueTasksAsync(userId, cancellationToken: cancellationToken);
        }

        return await _taskService.ListTasksAsync(userId, cancellationToken);
    }
}
