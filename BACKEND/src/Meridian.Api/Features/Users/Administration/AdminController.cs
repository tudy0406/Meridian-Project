using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Users.Administration;

/// <summary>Administrator-only endpoints for managing staff accounts and roles.</summary>
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/admin")]
public sealed class AdminController : ApiControllerBase
{
    private readonly IStaffAdminService _staff;
    public AdminController(IStaffAdminService staff) => _staff = staff;

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> Roles(CancellationToken ct) =>
        Ok(await _staff.ListRolesAsync(ct));

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> Users(CancellationToken ct) =>
        Ok(await _staff.ListUsersAsync(ct));

    /// <summary>Create an existing (non-onboarding) employee with one or more roles.</summary>
    [HttpPost("staff")]
    public async Task<ActionResult<CreateStaffResponse>> CreateStaff(CreateStaffRequest request, CancellationToken ct) =>
        Ok(await _staff.CreateStaffAsync(request, ct));

    /// <summary>Replace the set of roles held by a user.</summary>
    [HttpPut("users/{userId:int}/roles")]
    public async Task<ActionResult<AdminUserDto>> SetRoles(int userId, SetRolesRequest request, CancellationToken ct) =>
        Ok(await _staff.SetRolesAsync(userId, request, ct));
}
