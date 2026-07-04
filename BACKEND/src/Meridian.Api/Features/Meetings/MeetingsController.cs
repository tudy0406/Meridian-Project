using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Common.Web;
using Meridian.Api.Features.Meetings.Events;
using Meridian.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Meetings;

[Authorize]
public sealed class MeetingsController : ApiControllerBase
{
    private readonly IMeetingService _meetings;
    private readonly ICurrentUser _currentUser;

    public MeetingsController(IMeetingService meetings, ICurrentUser currentUser)
    {
        _meetings = meetings;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<MeetingDto>>> Mine(CancellationToken ct) =>
        Ok(await _meetings.GetMyMeetingsAsync(_currentUser.RequireUserId(), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MeetingDto>> Get(int id, CancellationToken ct) =>
        Ok(await _meetings.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.TeamLead},{RoleNames.Manager},{RoleNames.Mentor},{RoleNames.HrEmployee},{RoleNames.Administrator}")]
    public async Task<ActionResult<MeetingDto>> Create(SaveMeetingRequest request, CancellationToken ct)
    {
        var created = await _meetings.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.TeamLead},{RoleNames.Manager},{RoleNames.Mentor},{RoleNames.HrEmployee},{RoleNames.Administrator}")]
    public async Task<ActionResult<MeetingDto>> Update(int id, SaveMeetingRequest request, CancellationToken ct) =>
        Ok(await _meetings.UpdateAsync(id, request, ct));
}

public static class MeetingsModule
{
    public static IServiceCollection AddMeetingsFeature(this IServiceCollection services)
    {
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<IDomainEventHandler<MeetingChangedEvent>, MeetingChangedNotificationHandler>();
        return services;
    }
}
