using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Common.Web;
using Meridian.Api.Features.Documentation.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Documentation;

[Authorize]
[Route("api/documentation")]
public sealed class DocumentationController : ApiControllerBase
{
    private readonly IDocumentationService _docs;
    public DocumentationController(IDocumentationService docs) => _docs = docs;

    /// <summary>All documentation sections for a team (viewable by team members).</summary>
    [HttpGet("team/{teamId:int}")]
    public async Task<ActionResult<IReadOnlyList<DocumentationDto>>> ForTeam(int teamId, CancellationToken ct) =>
        Ok(await _docs.ListForTeamAsync(teamId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocumentationDto>> Get(int id, CancellationToken ct) =>
        Ok(await _docs.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.TeamLead},{RoleNames.HrEmployee},{RoleNames.Administrator}")]
    public async Task<ActionResult<DocumentationDto>> Create(SaveDocumentationRequest request, CancellationToken ct)
    {
        var created = await _docs.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.TeamLead},{RoleNames.HrEmployee},{RoleNames.Administrator}")]
    public async Task<ActionResult<DocumentationDto>> Update(int id, SaveDocumentationRequest request, CancellationToken ct) =>
        Ok(await _docs.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.TeamLead},{RoleNames.HrEmployee},{RoleNames.Administrator}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _docs.DeleteAsync(id, ct);
        return NoContent();
    }
}

public static class DocumentationModule
{
    public static IServiceCollection AddDocumentationFeature(this IServiceCollection services)
    {
        services.AddScoped<IDocumentationService, DocumentationService>();
        services.AddScoped<IDomainEventHandler<DocumentationUpdatedEvent>, DocumentationUpdatedNotificationHandler>();
        return services;
    }
}
