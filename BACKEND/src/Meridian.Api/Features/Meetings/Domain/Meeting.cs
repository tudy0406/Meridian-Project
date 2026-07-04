using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Meetings.Domain;

/// <summary>An onboarding meeting, either in-person (Location) or remote (OnlineLink).</summary>
public class Meeting : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public DateTime DateTime { get; set; }
    public string? Location { get; set; }
    public string? OnlineLink { get; set; }

    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
}
