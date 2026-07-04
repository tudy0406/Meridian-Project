using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.OnboardingProcess;
using Meridian.Api.Features.OnboardingProcess.Domain;
using Meridian.Api.Features.Tasks.Domain;
using Meridian.Api.Features.Tasks.Events;
using Meridian.Api.Features.Users.Domain;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Tasks;

/// <summary>
/// Onboarding task operations. Assignment is restricted by task category (HR ->
/// HR tasks, Manager -> department, Team Lead -> team, Mentor -> personal), and
/// completion is restricted to the owning employee. Assignment and completion
/// raise domain events so notifications and live updates fan out automatically.
/// </summary>
public sealed class TaskService : ITaskService
{
    private readonly MeridianDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IOnboardingAudienceResolver _audience;

    public TaskService(MeridianDbContext db, ICurrentUser currentUser, IOnboardingAudienceResolver audience)
    {
        _db = db;
        _currentUser = currentUser;
        _audience = audience;
    }

    public async Task<IReadOnlyList<EmployeeTaskDto>> GetMyTasksAsync(int userId, CancellationToken ct = default)
    {
        var tasks = await TaskQuery()
            .Where(t => t.Onboarding.EmployeeId == userId)
            .OrderBy(t => t.Deadline)
            .ToListAsync(ct);
        return tasks.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<EmployeeTaskDto>> GetTasksForEmployeeAsync(int employeeId, CancellationToken ct = default)
    {
        await EnsureCanViewEmployeeAsync(employeeId, ct);
        var tasks = await TaskQuery()
            .Where(t => t.Onboarding.EmployeeId == employeeId)
            .OrderBy(t => t.Deadline)
            .ToListAsync(ct);
        return tasks.Select(ToDto).ToList();
    }

    public async Task<EmployeeTaskDetailDto> GetTaskDetailAsync(int taskId, CancellationToken ct = default)
    {
        var task = await _db.EmployeeTasks
            .Include(t => t.Onboarding)
            .Include(t => t.AssignedBy)
            .Include(t => t.ContactPerson)
            .Include(t => t.Attachments)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.History).ThenInclude(h => h.ChangedBy)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw NotFoundException.For("Task", taskId);

        await EnsureCanViewEmployeeAsync(task.Onboarding.EmployeeId, ct);
        return ToDetailDto(task);
    }

    public async Task<EmployeeTaskDto> AssignTaskAsync(AssignTaskRequest request, CancellationToken ct = default)
    {
        var onboarding = await LoadOnboardingForAssignmentAsync(request.OnboardingEmployeeId, ct);
        await EnsureCanAssignAsync(request.Category, onboarding.Employee, ct);

        var task = new EmployeeTask
        {
            OnboardingId = onboarding.Id,
            Title = request.Title.Trim(),
            Description = request.Description,
            Requirements = request.Requirements,
            Category = request.Category,
            Priority = request.Priority,
            Deadline = request.Deadline,
            ContactPersonId = request.ContactPersonId,
            AssignedById = _currentUser.RequireUserId(),
            Status = EmployeeTaskStatus.NotStarted
        };

        if (request.Attachments is { Count: > 0 })
            foreach (var a in request.Attachments)
                task.Attachments.Add(new EmployeeTaskAttachment { FileName = a.FileName.Trim(), Url = a.Url.Trim() });

        return await FinalizeAssignmentAsync(onboarding, task, ct);
    }

    public async Task<EmployeeTaskDto> AssignFromTemplateAsync(AssignFromTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _db.TaskTemplates.FirstOrDefaultAsync(t => t.Id == request.TaskTemplateId, ct)
            ?? throw NotFoundException.For("TaskTemplate", request.TaskTemplateId);
        if (!template.IsActive)
            throw new BusinessRuleException("This task template is no longer active.");

        var onboarding = await LoadOnboardingForAssignmentAsync(request.OnboardingEmployeeId, ct);
        await EnsureCanAssignAsync(template.Category, onboarding.Employee, ct);

        // Copy the template into a standalone task (edits to the template later
        // never affect this instance).
        var task = new EmployeeTask
        {
            OnboardingId = onboarding.Id,
            TaskTemplateId = template.Id,
            Title = template.Title,
            Description = template.Description,
            Requirements = template.Requirements,
            Category = template.Category,
            Priority = template.Priority,
            Deadline = request.Deadline ?? DateTime.UtcNow.AddDays(template.EstimatedCompletionDays),
            ContactPersonId = request.ContactPersonId,
            AssignedById = _currentUser.RequireUserId(),
            Status = EmployeeTaskStatus.NotStarted
        };

        return await FinalizeAssignmentAsync(onboarding, task, ct);
    }

    public async Task<EmployeeTaskDto> UpdateStatusAsync(int taskId, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await _db.EmployeeTasks
            .Include(t => t.Onboarding).ThenInclude(o => o.Tasks)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw NotFoundException.For("Task", taskId);

        // Only the employee the task belongs to may change its status.
        if (task.Onboarding.EmployeeId != _currentUser.RequireUserId())
            throw new ForbiddenException("You may only update your own onboarding tasks.");

        if (task.Status == request.Status)
            return ToDto(await TaskQuery().FirstAsync(t => t.Id == task.Id, ct));

        var wasCompleted = task.Status == EmployeeTaskStatus.Completed;
        task.ChangeStatus(request.Status, _currentUser.RequireUserId());
        task.Onboarding.RecalculateProgress();

        if (request.Status == EmployeeTaskStatus.Completed && !wasCompleted)
            task.Raise(new TaskCompletedEvent(
                task.Id, task.Onboarding.EmployeeId, task.Title, task.Onboarding.ProgressPercentage));

        await _db.SaveChangesAsync(ct);
        return ToDto(await TaskQuery().FirstAsync(t => t.Id == task.Id, ct));
    }

    public async Task<TaskCommentDto> AddCommentAsync(int taskId, AddCommentRequest request, CancellationToken ct = default)
    {
        var task = await _db.EmployeeTasks
            .Include(t => t.Onboarding)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw NotFoundException.For("Task", taskId);

        await EnsureCanViewEmployeeAsync(task.Onboarding.EmployeeId, ct);

        var comment = new EmployeeTaskComment
        {
            EmployeeTaskId = task.Id,
            AuthorId = _currentUser.RequireUserId(),
            Text = request.Text.Trim()
        };
        _db.EmployeeTaskComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var author = await _db.Users.FirstAsync(u => u.Id == comment.AuthorId, ct);
        return new TaskCommentDto(comment.Id, author.Id, author.FullName, comment.Text, comment.CreatedAt);
    }

    // ---- Templates -------------------------------------------------------

    public async Task<IReadOnlyList<TaskTemplateDto>> ListTemplatesAsync(TaskCategory? category, CancellationToken ct = default)
    {
        var query = _db.TaskTemplates.Where(t => t.IsActive);

        // Each role only sees templates within its own scope; an Administrator
        // sees everything. Department templates are scoped to the department the
        // caller manages, team templates to the team they lead, and personal
        // templates to the ones they authored — so, e.g., a Legal manager never
        // sees Engineering's templates.
        if (!_currentUser.IsInRole(RoleNames.Administrator))
        {
            var me = _currentUser.RequireUserId();
            var isHr = _currentUser.IsInRole(RoleNames.HrEmployee);
            var isManager = _currentUser.IsInRole(RoleNames.Manager);
            var isLead = _currentUser.IsInRole(RoleNames.TeamLead);
            var isMentor = _currentUser.IsInRole(RoleNames.Mentor);

            var managedDeptIds = isManager
                ? await _db.Departments.Where(d => d.ManagerId == me).Select(d => d.Id).ToListAsync(ct)
                : new List<int>();
            var ledTeamIds = isLead
                ? await _db.Teams.Where(t => t.TeamLeadId == me).Select(t => t.Id).ToListAsync(ct)
                : new List<int>();

            query = query.Where(t =>
                (isHr && t.Category == TaskCategory.Hr)
                || (isManager && t.Category == TaskCategory.Department && t.DepartmentId != null && managedDeptIds.Contains(t.DepartmentId.Value))
                || (isLead && t.Category == TaskCategory.Team && t.TeamId != null && ledTeamIds.Contains(t.TeamId.Value))
                || (isMentor && t.Category == TaskCategory.Personal && t.CreatedById == me));
        }

        if (category is not null) query = query.Where(t => t.Category == category);
        var templates = await query.OrderBy(t => t.Category).ThenBy(t => t.Title).ToListAsync(ct);
        return templates.Select(ToTemplateDto).ToList();
    }

    public async Task<TaskTemplateDto> CreateTemplateAsync(CreateTaskTemplateRequest request, CancellationToken ct = default)
    {
        EnsureCanManageTemplateCategory(request.Category);
        var (departmentId, teamId) = await ResolveTemplateScopeAsync(request, ct);
        var template = new TaskTemplate
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Requirements = request.Requirements,
            Category = request.Category,
            Priority = request.Priority,
            EstimatedCompletionDays = request.EstimatedCompletionDays,
            DepartmentId = departmentId,
            TeamId = teamId,
            CreatedById = _currentUser.RequireUserId()
        };
        _db.TaskTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return ToTemplateDto(template);
    }

    public async Task<TaskTemplateDto> UpdateTemplateAsync(int id, CreateTaskTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _db.TaskTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw NotFoundException.For("TaskTemplate", id);
        EnsureCanManageTemplateCategory(template.Category);
        EnsureCanManageTemplateCategory(request.Category);

        var (departmentId, teamId) = await ResolveTemplateScopeAsync(request, ct);
        template.Title = request.Title.Trim();
        template.Description = request.Description;
        template.Requirements = request.Requirements;
        template.Category = request.Category;
        template.Priority = request.Priority;
        template.EstimatedCompletionDays = request.EstimatedCompletionDays;
        template.DepartmentId = departmentId;
        template.TeamId = teamId;
        await _db.SaveChangesAsync(ct);
        return ToTemplateDto(template);
    }

    public async Task DeactivateTemplateAsync(int id, CancellationToken ct = default)
    {
        var template = await _db.TaskTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw NotFoundException.For("TaskTemplate", id);
        EnsureCanManageTemplateCategory(template.Category);
        template.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    // ---- Internals -------------------------------------------------------

    private async Task<Onboarding> LoadOnboardingForAssignmentAsync(int employeeId, CancellationToken ct) =>
        await _db.Onboardings
            .Include(o => o.Tasks)
            .Include(o => o.Employee).ThenInclude(e => e.Team)
            .Include(o => o.Employee).ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(o => o.EmployeeId == employeeId, ct)
        ?? throw new BusinessRuleException("Tasks can only be assigned to employees currently onboarding.");

    /// <summary>Shared tail of both assignment paths: persist, seed history, recalc, notify.</summary>
    private async Task<EmployeeTaskDto> FinalizeAssignmentAsync(Onboarding onboarding, EmployeeTask task, CancellationToken ct)
    {
        _db.EmployeeTasks.Add(task);
        await _db.SaveChangesAsync(ct); // assigns task.Id; EF fixup adds it to onboarding.Tasks

        // Seed the completion history with the initial "assigned" state.
        task.History.Add(new EmployeeTaskHistory
        {
            Status = task.Status,
            ChangedById = task.AssignedById,
            ChangedAt = task.CreatedAt
        });

        onboarding.RecalculateProgress();
        task.Raise(new TaskAssignedEvent(task.Id, onboarding.EmployeeId, task.Title));
        await _db.SaveChangesAsync(ct);

        return ToDto(await TaskQuery().FirstAsync(t => t.Id == task.Id, ct));
    }

    private async Task EnsureCanViewEmployeeAsync(int employeeId, CancellationToken ct)
    {
        if (employeeId == _currentUser.UserId) return;
        if (_currentUser.IsInRole(RoleNames.HrEmployee) || _currentUser.IsInRole(RoleNames.Administrator)) return;

        var audience = await _audience.ResolveAsync(employeeId, ct);
        if (!audience.Contains(_currentUser.RequireUserId()))
            throw new ForbiddenException("You are not allowed to view this employee's tasks.");
    }

    /// <summary>Maps task category to the responsibility allowed to assign it, plus ownership.</summary>
    private async Task EnsureCanAssignAsync(TaskCategory category, User employee, CancellationToken ct)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator)) return;
        var me = _currentUser.RequireUserId();

        switch (category)
        {
            case TaskCategory.Hr when _currentUser.IsInRole(RoleNames.HrEmployee):
                return;
            case TaskCategory.Department when _currentUser.IsInRole(RoleNames.Manager)
                && employee.Department?.ManagerId == me:
                return;
            case TaskCategory.Team when _currentUser.IsInRole(RoleNames.TeamLead)
                && employee.Team?.TeamLeadId == me:
                return;
            case TaskCategory.Personal when _currentUser.IsInRole(RoleNames.Mentor)
                && await _db.Onboardings.AnyAsync(o => o.EmployeeId == employee.Id && o.MentorId == me, ct):
                return;
        }

        throw new ForbiddenException($"You are not allowed to assign {category} tasks to this employee.");
    }

    private void EnsureCanManageTemplateCategory(TaskCategory category)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator)) return;
        var allowed = category switch
        {
            TaskCategory.Hr => _currentUser.IsInRole(RoleNames.HrEmployee),
            TaskCategory.Team => _currentUser.IsInRole(RoleNames.TeamLead),
            TaskCategory.Department => _currentUser.IsInRole(RoleNames.Manager),
            TaskCategory.Personal => _currentUser.IsInRole(RoleNames.Mentor),
            _ => false
        };
        if (!allowed)
            throw new ForbiddenException($"You are not allowed to manage {category} task templates.");
    }

    /// <summary>
    /// Determines the department/team a template is bound to. HR templates are
    /// company-wide; a Manager's department templates bind to the department they
    /// manage; a Team Lead's team templates bind to the team they lead (and its
    /// department); personal templates carry the mentor's own department/team. An
    /// Administrator specifies the department/team explicitly.
    /// </summary>
    private async Task<(int? DepartmentId, int? TeamId)> ResolveTemplateScopeAsync(CreateTaskTemplateRequest request, CancellationToken ct)
    {
        var me = _currentUser.RequireUserId();
        var isAdmin = _currentUser.IsInRole(RoleNames.Administrator);

        switch (request.Category)
        {
            case TaskCategory.Hr:
                return (null, null);

            case TaskCategory.Department:
                if (isAdmin)
                {
                    if (request.DepartmentId is null)
                        throw new BusinessRuleException("A department template must specify a department.");
                    if (!await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
                        throw NotFoundException.For("Department", request.DepartmentId);
                    return (request.DepartmentId, null);
                }
                var managed = await _db.Departments.FirstOrDefaultAsync(d => d.ManagerId == me, ct)
                    ?? throw new BusinessRuleException("You do not manage a department, so you cannot create department templates.");
                return (managed.Id, null);

            case TaskCategory.Team:
                var team = isAdmin
                    ? await _db.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId, ct)
                        ?? throw new BusinessRuleException("A team template must specify a valid team.")
                    : await _db.Teams.FirstOrDefaultAsync(t => t.TeamLeadId == me, ct)
                        ?? throw new BusinessRuleException("You do not lead a team, so you cannot create team templates.");
                return (team.DepartmentId, team.Id);

            case TaskCategory.Personal:
                var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == me, ct);
                return (author?.DepartmentId, author?.TeamId);

            default:
                return (null, null);
        }
    }

    private IQueryable<EmployeeTask> TaskQuery() => _db.EmployeeTasks
        .Include(t => t.Onboarding)
        .Include(t => t.AssignedBy)
        .Include(t => t.ContactPerson);

    private static EmployeeTaskDto ToDto(EmployeeTask t) => new(
        t.Id, t.Onboarding.EmployeeId, t.Title, t.Description, t.Category.ToString(),
        t.Status.ToString(), t.Priority.ToString(), t.Deadline, t.AssignedById, t.AssignedBy?.FullName,
        t.ContactPersonId, t.ContactPerson?.FullName, t.CompletedAt, t.CreatedAt);

    private static EmployeeTaskDetailDto ToDetailDto(EmployeeTask t) => new(
        t.Id, t.Onboarding.EmployeeId, t.Title, t.Description, t.Requirements,
        t.Category.ToString(), t.Status.ToString(), t.Priority.ToString(), t.Deadline,
        t.AssignedById, t.AssignedBy?.FullName, t.CreatedAt,
        t.ContactPersonId, t.ContactPerson?.FullName, t.CompletedAt,
        t.Attachments.OrderBy(a => a.Id).Select(a => new AttachmentDto(a.Id, a.FileName, a.Url)).ToList(),
        t.Comments.OrderBy(c => c.CreatedAt)
            .Select(c => new TaskCommentDto(c.Id, c.AuthorId, c.Author?.FullName ?? "Unknown", c.Text, c.CreatedAt)).ToList(),
        t.History.OrderBy(h => h.ChangedAt)
            .Select(h => new TaskHistoryDto(h.Status.ToString(), h.ChangedById, h.ChangedBy?.FullName ?? "Unknown", h.ChangedAt)).ToList());

    private static TaskTemplateDto ToTemplateDto(TaskTemplate t) => new(
        t.Id, t.Title, t.Description, t.Requirements, t.Category.ToString(), t.Priority.ToString(),
        t.EstimatedCompletionDays, t.DepartmentId, t.TeamId, t.IsActive);
}
