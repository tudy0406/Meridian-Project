namespace Meridian.Api.Features.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<EmployeeTaskDto>> GetMyTasksAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeTaskDto>> GetTasksForEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<EmployeeTaskDetailDto> GetTaskDetailAsync(int taskId, CancellationToken ct = default);
    Task<EmployeeTaskDto> AssignTaskAsync(AssignTaskRequest request, CancellationToken ct = default);
    Task<EmployeeTaskDto> AssignFromTemplateAsync(AssignFromTemplateRequest request, CancellationToken ct = default);
    Task<EmployeeTaskDto> UpdateStatusAsync(int taskId, UpdateTaskStatusRequest request, CancellationToken ct = default);
    Task<TaskCommentDto> AddCommentAsync(int taskId, AddCommentRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<TaskTemplateDto>> ListTemplatesAsync(Common.Domain.TaskCategory? category, CancellationToken ct = default);
    Task<TaskTemplateDto> CreateTemplateAsync(CreateTaskTemplateRequest request, CancellationToken ct = default);
    Task<TaskTemplateDto> UpdateTemplateAsync(int id, CreateTaskTemplateRequest request, CancellationToken ct = default);
    Task DeactivateTemplateAsync(int id, CancellationToken ct = default);
}
