using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Teams;

[Authorize]
public sealed class TeamsController : ApiControllerBase
{
    private readonly ITeamService _teams;
    public TeamsController(ITeamService teams) => _teams = teams;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> List([FromQuery] int? departmentId, CancellationToken ct) =>
        Ok(await _teams.ListAsync(departmentId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeamDto>> Get(int id, CancellationToken ct) =>
        Ok(await _teams.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Manager},{RoleNames.Administrator}")]
    public async Task<ActionResult<TeamDto>> Create(CreateTeamRequest request, CancellationToken ct)
    {
        var created = await _teams.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Manager},{RoleNames.TeamLead},{RoleNames.Administrator}")]
    public async Task<ActionResult<TeamDto>> Update(int id, UpdateTeamRequest request, CancellationToken ct) =>
        Ok(await _teams.UpdateAsync(id, request, ct));

    /// <summary>Assign an employee to this team (Manager within their department, or Admin).</summary>
    [HttpPost("{teamId:int}/members/{userId:int}")]
    [Authorize(Roles = $"{RoleNames.Manager},{RoleNames.Administrator}")]
    public async Task<IActionResult> AssignEmployee(int teamId, int userId, CancellationToken ct)
    {
        await _teams.AssignEmployeeAsync(teamId, userId, ct);
        return NoContent();
    }

    /// <summary>Assign or change the Team Lead (Manager within their department, or Admin).</summary>
    [HttpPut("{teamId:int}/team-lead/{userId:int}")]
    [Authorize(Roles = $"{RoleNames.Manager},{RoleNames.Administrator}")]
    public async Task<IActionResult> AssignTeamLead(int teamId, int userId, CancellationToken ct)
    {
        await _teams.AssignTeamLeadAsync(teamId, userId, ct);
        return NoContent();
    }
}

public static class TeamsModule
{
    public static IServiceCollection AddTeamsFeature(this IServiceCollection services)
    {
        services.AddScoped<ITeamService, TeamService>();
        return services;
    }
}
