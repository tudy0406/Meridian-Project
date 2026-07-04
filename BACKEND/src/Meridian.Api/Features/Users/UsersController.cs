using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Web;
using Meridian.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Users;

[Authorize]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _users;
    private readonly ICurrentUser _currentUser;

    public UsersController(IUserService users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    /// <summary>HR creates a new employee account and starts their onboarding.</summary>
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Administrator}")]
    public async Task<ActionResult<CreateEmployeeResponse>> CreateEmployee(CreateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _users.CreateEmployeeAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.UserId }, result);
    }

    /// <summary>The authenticated user's own profile.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken ct) =>
        Ok(await _users.GetProfileAsync(_currentUser.RequireUserId(), ct));

    /// <summary>Update the authenticated user's own editable profile fields.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await _users.UpdateProfileAsync(_currentUser.RequireUserId(), request, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserProfileDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _users.GetProfileAsync(id, ct));

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.HrEmployee},{RoleNames.Administrator},{RoleNames.Manager},{RoleNames.TeamLead}")]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> List(
        [FromQuery] int? teamId, [FromQuery] int? departmentId, CancellationToken ct) =>
        Ok(await _users.ListAsync(teamId, departmentId, ct));

    [HttpGet("team/{teamId:int}")]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> TeamMembers(int teamId, CancellationToken ct) =>
        Ok(await _users.GetTeamMembersAsync(teamId, ct));
}
