using System.ComponentModel.DataAnnotations;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.Meetings.Domain;
using Meridian.Api.Features.Meetings.Events;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Meetings;

public sealed record MeetingParticipantDto(int UserId, string FullName);

public sealed record MeetingDto(
    int Id, string Title, string? Description, int OrganizerId, string OrganizerName,
    DateTime DateTime, string? Location, string? OnlineLink,
    IReadOnlyCollection<MeetingParticipantDto> Participants);

public sealed record SaveMeetingRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(4000)] string? Description,
    [Required] DateTime DateTime,
    [StringLength(300)] string? Location,
    [StringLength(500)] string? OnlineLink,
    [Required, MinLength(1)] IReadOnlyList<int> ParticipantIds);

public interface IMeetingService
{
    Task<IReadOnlyList<MeetingDto>> GetMyMeetingsAsync(int userId, CancellationToken ct = default);
    Task<MeetingDto> GetAsync(int id, CancellationToken ct = default);
    Task<MeetingDto> CreateAsync(SaveMeetingRequest request, CancellationToken ct = default);
    Task<MeetingDto> UpdateAsync(int id, SaveMeetingRequest request, CancellationToken ct = default);
}

/// <summary>
/// Schedules and updates onboarding meetings. Any change notifies participants
/// through a domain event. A meeting must be either in-person (Location) or
/// remote (OnlineLink).
/// </summary>
public sealed class MeetingService : IMeetingService
{
    private readonly MeridianDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly OnboardingProcess.IOnboardingAudienceResolver _audience;

    public MeetingService(MeridianDbContext db, ICurrentUser currentUser,
        OnboardingProcess.IOnboardingAudienceResolver audience)
    {
        _db = db;
        _currentUser = currentUser;
        _audience = audience;
    }

    public async Task<IReadOnlyList<MeetingDto>> GetMyMeetingsAsync(int userId, CancellationToken ct = default)
    {
        var meetings = await MeetingQuery()
            .Where(m => m.OrganizerId == userId || m.Participants.Any(p => p.UserId == userId))
            .OrderBy(m => m.DateTime)
            .ToListAsync(ct);
        return meetings.Select(ToDto).ToList();
    }

    public async Task<MeetingDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await FindOrThrow(id, ct));

    public async Task<MeetingDto> CreateAsync(SaveMeetingRequest request, CancellationToken ct = default)
    {
        Validate(request);
        await EnsureCanInviteAsync(request.ParticipantIds, ct);

        var meeting = new Meeting
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            DateTime = request.DateTime,
            Location = request.Location,
            OnlineLink = request.OnlineLink,
            OrganizerId = _currentUser.RequireUserId()
        };
        foreach (var userId in request.ParticipantIds.Distinct())
            meeting.Participants.Add(new MeetingParticipant { UserId = userId });

        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync(ct);

        meeting.Raise(new MeetingChangedEvent(meeting.Id, meeting.Title, meeting.DateTime,
            meeting.Participants.Select(p => p.UserId).ToList(), IsUpdate: false));
        await _db.SaveChangesAsync(ct);

        return ToDto(await FindOrThrow(meeting.Id, ct));
    }

    public async Task<MeetingDto> UpdateAsync(int id, SaveMeetingRequest request, CancellationToken ct = default)
    {
        Validate(request);
        var meeting = await FindOrThrow(id, ct);

        if (meeting.OrganizerId != _currentUser.RequireUserId() && !_currentUser.IsInRole(Common.Domain.RoleNames.Administrator))
            throw new ForbiddenException("Only the organizer may modify this meeting.");

        await EnsureCanInviteAsync(request.ParticipantIds, ct);

        meeting.Title = request.Title.Trim();
        meeting.Description = request.Description;
        meeting.DateTime = request.DateTime;
        meeting.Location = request.Location;
        meeting.OnlineLink = request.OnlineLink;

        // Replace the participant set.
        meeting.Participants.Clear();
        foreach (var userId in request.ParticipantIds.Distinct())
            meeting.Participants.Add(new MeetingParticipant { UserId = userId });

        meeting.Raise(new MeetingChangedEvent(meeting.Id, meeting.Title, meeting.DateTime,
            request.ParticipantIds.Distinct().ToList(), IsUpdate: true));
        await _db.SaveChangesAsync(ct);

        return ToDto(await FindOrThrow(meeting.Id, ct));
    }

    private static void Validate(SaveMeetingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Location) && string.IsNullOrWhiteSpace(request.OnlineLink))
            throw new BusinessRuleException("A meeting must have either a location or an online link.");
    }

    /// <summary>
    /// Meetings target employees currently onboarding. HR/Admin may invite any of
    /// them; a Manager/Team Lead/Mentor may only invite onboarding employees within
    /// their supervisory scope (the same people they can assign tasks to).
    /// </summary>
    private async Task EnsureCanInviteAsync(IReadOnlyList<int> participantIds, CancellationToken ct)
    {
        var ids = participantIds.Distinct().ToList();
        var participants = await _db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.IsOnboarding })
            .ToListAsync(ct);

        var missing = ids.Except(participants.Select(p => p.Id)).ToList();
        if (missing.Count > 0)
            throw new BusinessRuleException($"Unknown participant id(s): {string.Join(", ", missing)}.");

        var notOnboarding = participants.Where(p => !p.IsOnboarding).Select(p => p.Id).ToList();
        if (notOnboarding.Count > 0)
            throw new BusinessRuleException("Meetings can only include employees who are currently onboarding.");

        if (_currentUser.IsInRole(Common.Domain.RoleNames.HrEmployee) ||
            _currentUser.IsInRole(Common.Domain.RoleNames.Administrator))
            return;

        var me = _currentUser.RequireUserId();
        foreach (var participant in participants)
        {
            var audience = await _audience.ResolveAsync(participant.Id, ct);
            if (!audience.Contains(me))
                throw new ForbiddenException(
                    "You may only invite onboarding employees you supervise (as their mentor, team lead or manager).");
        }
    }

    private IQueryable<Meeting> MeetingQuery() => _db.Meetings
        .Include(m => m.Organizer)
        .Include(m => m.Participants).ThenInclude(p => p.User);

    private async Task<Meeting> FindOrThrow(int id, CancellationToken ct) =>
        await MeetingQuery().FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw NotFoundException.For("Meeting", id);

    private static MeetingDto ToDto(Meeting m) => new(
        m.Id, m.Title, m.Description, m.OrganizerId, m.Organizer?.FullName ?? string.Empty,
        m.DateTime, m.Location, m.OnlineLink,
        m.Participants.Select(p => new MeetingParticipantDto(p.UserId, p.User?.FullName ?? string.Empty)).ToList());
}
