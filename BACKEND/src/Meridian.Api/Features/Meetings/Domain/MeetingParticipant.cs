using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Meetings.Domain;

/// <summary>Join entity for the many-to-many Meeting &lt;-&gt; User relationship.</summary>
public class MeetingParticipant
{
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
