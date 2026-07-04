using System.ComponentModel.DataAnnotations;
using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.Documentation.Domain;
using Meridian.Api.Features.Documentation.Events;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Documentation;

public sealed record SaveDocumentationRequest(
    [Required] int TeamId,
    [Required, StringLength(200)] string Title,
    [Required] DocumentationCategory Category,
    [Required] string Content);

public sealed record DocumentationDto(
    int Id, int TeamId, string Title, string Category, string Content, DateTime UpdatedAt);

public interface IDocumentationService
{
    Task<IReadOnlyList<DocumentationDto>> ListForTeamAsync(int teamId, CancellationToken ct = default);
    Task<DocumentationDto> GetAsync(int id, CancellationToken ct = default);
    Task<DocumentationDto> CreateAsync(SaveDocumentationRequest request, CancellationToken ct = default);
    Task<DocumentationDto> UpdateAsync(int id, SaveDocumentationRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Team onboarding documentation. Team Leads may only maintain documentation for
/// their own teams; HR/Admin have full access. Content is stored as provided and
/// HTML-encoded at render time by the client.
/// </summary>
public sealed class DocumentationService : IDocumentationService
{
    private readonly MeridianDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DocumentationService(MeridianDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DocumentationDto>> ListForTeamAsync(int teamId, CancellationToken ct = default)
    {
        var docs = await _db.TeamDocumentation
            .Where(d => d.TeamId == teamId)
            .OrderBy(d => d.Category).ThenBy(d => d.Title)
            .ToListAsync(ct);
        return docs.Select(ToDto).ToList();
    }

    public async Task<DocumentationDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await FindOrThrow(id, ct));

    public async Task<DocumentationDto> CreateAsync(SaveDocumentationRequest request, CancellationToken ct = default)
    {
        await EnsureCanMaintainAsync(request.TeamId, ct);

        var doc = new TeamDocumentation
        {
            TeamId = request.TeamId,
            Title = request.Title.Trim(),
            Category = request.Category,
            Content = request.Content,
            CreatedById = _currentUser.RequireUserId(),
            UpdatedAt = DateTime.UtcNow
        };
        _db.TeamDocumentation.Add(doc);
        await _db.SaveChangesAsync(ct);

        doc.Raise(new DocumentationUpdatedEvent(doc.TeamId, doc.Title));
        await _db.SaveChangesAsync(ct);

        return ToDto(doc);
    }

    public async Task<DocumentationDto> UpdateAsync(int id, SaveDocumentationRequest request, CancellationToken ct = default)
    {
        var doc = await FindOrThrow(id, ct);
        await EnsureCanMaintainAsync(doc.TeamId, ct);

        doc.Title = request.Title.Trim();
        doc.Category = request.Category;
        doc.Content = request.Content;
        doc.UpdatedAt = DateTime.UtcNow;

        doc.Raise(new DocumentationUpdatedEvent(doc.TeamId, doc.Title));
        await _db.SaveChangesAsync(ct);

        return ToDto(doc);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var doc = await FindOrThrow(id, ct);
        await EnsureCanMaintainAsync(doc.TeamId, ct);
        _db.TeamDocumentation.Remove(doc);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Team Lead of the team, or HR/Admin, may maintain the documentation.</summary>
    private async Task EnsureCanMaintainAsync(int teamId, CancellationToken ct)
    {
        if (_currentUser.IsInRole(RoleNames.HrEmployee) || _currentUser.IsInRole(RoleNames.Administrator))
            return;

        if (_currentUser.IsInRole(RoleNames.TeamLead))
        {
            var isLead = await _db.Teams.AnyAsync(t => t.Id == teamId && t.TeamLeadId == _currentUser.UserId, ct);
            if (isLead) return;
        }

        throw new ForbiddenException("You may only maintain documentation for your own teams.");
    }

    private async Task<TeamDocumentation> FindOrThrow(int id, CancellationToken ct) =>
        await _db.TeamDocumentation.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw NotFoundException.For("Documentation", id);

    private static DocumentationDto ToDto(TeamDocumentation d) => new(
        d.Id, d.TeamId, d.Title, d.Category.ToString(), d.Content, d.UpdatedAt);
}
