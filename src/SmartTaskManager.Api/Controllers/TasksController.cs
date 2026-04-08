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
        Guid userId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await CreateTaskAsync(userId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetTaskById),
            new { userId, taskId = task.Id },
            task);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskSummary>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyCollection<TaskSummary>>> GetTasks(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TaskSummary> tasks = await _taskService.ListTasksAsync(userId, cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskSummary), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TaskSummary>> GetTaskById(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.GetTaskAsync(userId, taskId, cancellationToken);
        return Ok(task);
    }

    [HttpPatch("{taskId:guid}/priority")]
    [ProducesResponseType(typeof(TaskSummary), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TaskSummary>> UpdatePriority(
        Guid userId,
        Guid taskId,
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
    public async Task<ActionResult<TaskSummary>> MarkAsCompleted(
        Guid userId,
        Guid taskId,
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
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary task = await _taskService.ArchiveTaskAsync(userId, taskId, cancellationToken);
        return Ok(task);
    }

    [HttpGet("{taskId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<HistoryEntryResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyCollection<HistoryEntryResponse>>> GetTaskHistory(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<HistoryEntry> history = await _taskService.GetTaskHistoryAsync(
            userId,
            taskId,
            cancellationToken);

        return Ok(history.Select(HistoryEntryResponse.FromDomain).ToList());
    }

    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskSummary>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyCollection<TaskSummary>>> GetTasksByStatus(
        Guid userId,
        DomainTaskStatus status,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TaskSummary> tasks = await _taskService.FilterTasksByStatusAsync(
            userId,
            status,
            cancellationToken);

        return Ok(tasks);
    }

    [HttpGet("priority/{priority}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskSummary>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyCollection<TaskSummary>>> GetTasksByPriority(
        Guid userId,
        TaskPriority priority,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TaskSummary> tasks = await _taskService.FilterTasksByPriorityAsync(
            userId,
            priority,
            cancellationToken);

        return Ok(tasks);
    }

    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskSummary>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyCollection<TaskSummary>>> GetOverdueTasks(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TaskSummary> tasks = await _taskService.GetOverdueTasksAsync(userId, cancellationToken: cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetDashboardSummary(
        Guid userId,
        CancellationToken cancellationToken)
    {
        TaskDashboardSummary summary = await _taskService.GetDashboardSummaryAsync(userId, cancellationToken);
        return Ok(DashboardSummaryResponse.FromApplication(summary));
    }

    private Task<TaskSummary> CreateTaskAsync(
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
}
