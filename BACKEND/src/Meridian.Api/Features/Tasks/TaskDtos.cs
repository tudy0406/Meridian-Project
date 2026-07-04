using System.ComponentModel.DataAnnotations;
using Meridian.Api.Common.Domain;

namespace Meridian.Api.Features.Tasks;

public sealed record AttachmentInput(
    [Required, StringLength(255)] string FileName,
    [Required, StringLength(1000)] string Url);

public sealed record AssignTaskRequest(
    [Required] int OnboardingEmployeeId,
    [Required, StringLength(200)] string Title,
    [StringLength(4000)] string? Description,
    [StringLength(4000)] string? Requirements,
    [Required] TaskCategory Category,
    TaskPriority Priority,
    DateTime? Deadline,
    int? ContactPersonId,
    IReadOnlyList<AttachmentInput>? Attachments);

/// <summary>Assigns a task by instantiating a predefined template for an employee.</summary>
public sealed record AssignFromTemplateRequest(
    [Required] int OnboardingEmployeeId,
    [Required] int TaskTemplateId,
    DateTime? Deadline,
    int? ContactPersonId);

public sealed record UpdateTaskStatusRequest([Required] EmployeeTaskStatus Status);

public sealed record AddCommentRequest([Required, StringLength(2000)] string Text);

public sealed record AttachmentDto(int Id, string FileName, string Url);
public sealed record TaskCommentDto(int Id, int AuthorId, string AuthorName, string Text, DateTime CreatedAt);
public sealed record TaskHistoryDto(string Status, int ChangedById, string ChangedByName, DateTime ChangedAt);

/// <summary>Compact task representation for list/card views.</summary>
public sealed record EmployeeTaskDto(
    int Id, int OnboardingEmployeeId, string Title, string? Description,
    string Category, string Status, string Priority, DateTime? Deadline,
    int AssignedById, string? AssignedByName, int? ContactPersonId, string? ContactPersonName,
    DateTime? CompletedAt, DateTime AssignedAt);

/// <summary>Full task representation for the expandable accordion.</summary>
public sealed record EmployeeTaskDetailDto(
    int Id, int OnboardingEmployeeId, string Title, string? Description, string? Requirements,
    string Category, string Status, string Priority, DateTime? Deadline,
    int AssignedById, string? AssignedByName, DateTime AssignedAt,
    int? ContactPersonId, string? ContactPersonName, DateTime? CompletedAt,
    IReadOnlyList<AttachmentDto> Attachments,
    IReadOnlyList<TaskCommentDto> Comments,
    IReadOnlyList<TaskHistoryDto> History);

public sealed record CreateTaskTemplateRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(4000)] string? Description,
    [StringLength(4000)] string? Requirements,
    [Required] TaskCategory Category,
    TaskPriority Priority,
    [Range(1, 365)] int EstimatedCompletionDays,
    int? DepartmentId,
    int? TeamId);

public sealed record TaskTemplateDto(
    int Id, string Title, string? Description, string? Requirements, string Category, string Priority,
    int EstimatedCompletionDays, int? DepartmentId, int? TeamId, bool IsActive);
