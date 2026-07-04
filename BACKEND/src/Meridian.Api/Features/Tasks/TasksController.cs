using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Web;
using Meridian.Api.Features.Tasks.Events;
using Meridian.Api.Features.Tasks.Generation;
using Meridian.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Tasks;

[Authorize]
public sealed class TasksController : ApiControllerBase
{
    private readonly ITaskService _tasks;
    private readonly ICurrentUser _currentUser;

    public TasksController(ITaskService tasks, ICurrentUser currentUser)
    {
        _tasks = tasks;
        _currentUser = currentUser;
    }

    /// <summary>The authenticated employee's own onboarding tasks.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<EmployeeTaskDto>>> Mine(CancellationToken ct) =>
        Ok(await _tasks.GetMyTasksAsync(_currentUser.RequireUserId(), ct));

    /// <summary>Tasks for a specific employee (mentor/team lead/manager/HR view).</summary>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IReadOnlyList<EmployeeTaskDto>>> ForEmployee(int employeeId, CancellationToken ct) =>
        Ok(await _tasks.GetTasksForEmployeeAsync(employeeId, ct));

    /// <summary>Full task detail: description, requirements, attachments, comments, history.</summary>
    [HttpGet("{taskId:int}")]
    public async Task<ActionResult<EmployeeTaskDetailDto>> Detail(int taskId, CancellationToken ct) =>
        Ok(await _tasks.GetTaskDetailAsync(taskId, ct));

    /// <summary>Assign a custom onboarding task. The allowed category depends on the caller's role.</summary>
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Manager},{RoleNames.TeamLead},{RoleNames.Mentor},{RoleNames.Administrator}")]
    public async Task<ActionResult<EmployeeTaskDto>> Assign(AssignTaskRequest request, CancellationToken ct) =>
        Ok(await _tasks.AssignTaskAsync(request, ct));

    /// <summary>Assign a task by instantiating a predefined template.</summary>
    [HttpPost("from-template")]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Manager},{RoleNames.TeamLead},{RoleNames.Mentor},{RoleNames.Administrator}")]
    public async Task<ActionResult<EmployeeTaskDto>> AssignFromTemplate(AssignFromTemplateRequest request, CancellationToken ct) =>
        Ok(await _tasks.AssignFromTemplateAsync(request, ct));

    /// <summary>Update a task's status. Only the owning employee may do this.</summary>
    [HttpPatch("{taskId:int}/status")]
    public async Task<ActionResult<EmployeeTaskDto>> UpdateStatus(int taskId, UpdateTaskStatusRequest request, CancellationToken ct) =>
        Ok(await _tasks.UpdateStatusAsync(taskId, request, ct));

    /// <summary>Add a comment to a task (owner or a supervising role).</summary>
    [HttpPost("{taskId:int}/comments")]
    public async Task<ActionResult<TaskCommentDto>> AddComment(int taskId, AddCommentRequest request, CancellationToken ct) =>
        Ok(await _tasks.AddCommentAsync(taskId, request, ct));

    // ---- Templates -------------------------------------------------------

    [HttpGet("templates")]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Manager},{RoleNames.TeamLead},{RoleNames.Mentor},{RoleNames.Administrator}")]
    public async Task<ActionResult<IReadOnlyList<TaskTemplateDto>>> ListTemplates([FromQuery] TaskCategory? category, CancellationToken ct) =>
        Ok(await _tasks.ListTemplatesAsync(category, ct));

    [HttpPost("templates")]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Manager},{RoleNames.TeamLead},{RoleNames.Mentor},{RoleNames.Administrator}")]
    public async Task<ActionResult<TaskTemplateDto>> CreateTemplate(CreateTaskTemplateRequest request, CancellationToken ct) =>
        Ok(await _tasks.CreateTemplateAsync(request, ct));

    [HttpPut("templates/{id:int}")]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Manager},{RoleNames.TeamLead},{RoleNames.Mentor},{RoleNames.Administrator}")]
    public async Task<ActionResult<TaskTemplateDto>> UpdateTemplate(int id, CreateTaskTemplateRequest request, CancellationToken ct) =>
        Ok(await _tasks.UpdateTemplateAsync(id, request, ct));

    [HttpDelete("templates/{id:int}")]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Manager},{RoleNames.TeamLead},{RoleNames.Mentor},{RoleNames.Administrator}")]
    public async Task<IActionResult> DeactivateTemplate(int id, CancellationToken ct)
    {
        await _tasks.DeactivateTemplateAsync(id, ct);
        return NoContent();
    }
}

public static class TasksModule
{
    public static IServiceCollection AddTasksFeature(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();

        // Strategy pattern: one template-selection strategy per category.
        services.AddScoped<ITaskTemplateStrategy, HrTaskTemplateStrategy>();
        services.AddScoped<ITaskTemplateStrategy, DepartmentTaskTemplateStrategy>();
        services.AddScoped<ITaskTemplateStrategy, TeamTaskTemplateStrategy>();
        services.AddScoped<ITaskTemplateStrategy, PersonalTaskTemplateStrategy>();
        services.AddScoped<IOnboardingTaskComposer, OnboardingTaskComposer>();

        // Observer pattern: handlers reacting to task domain events.
        services.AddScoped<Common.Domain.Events.IDomainEventHandler<TaskAssignedEvent>, TaskAssignedNotificationHandler>();
        services.AddScoped<Common.Domain.Events.IDomainEventHandler<TaskCompletedEvent>, TaskCompletedNotificationHandler>();
        services.AddScoped<Common.Domain.Events.IDomainEventHandler<OnboardingProcess.Events.MentorAssignedEvent>, MentorAssignedTaskHandler>();
        return services;
    }
}
