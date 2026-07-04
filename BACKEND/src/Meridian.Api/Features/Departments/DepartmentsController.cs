using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Departments;

[Authorize]
public sealed class DepartmentsController : ApiControllerBase
{
    private readonly IDepartmentService _departments;
    public DepartmentsController(IDepartmentService departments) => _departments = departments;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> List(CancellationToken ct) =>
        Ok(await _departments.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> Get(int id, CancellationToken ct) =>
        Ok(await _departments.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<ActionResult<DepartmentDto>> Create(CreateDepartmentRequest request, CancellationToken ct)
    {
        var created = await _departments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Manager}")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, UpdateDepartmentRequest request, CancellationToken ct) =>
        Ok(await _departments.UpdateAsync(id, request, ct));
}

public static class DepartmentsModule
{
    public static IServiceCollection AddDepartmentsFeature(this IServiceCollection services)
    {
        services.AddScoped<IDepartmentService, DepartmentService>();
        return services;
    }
}
