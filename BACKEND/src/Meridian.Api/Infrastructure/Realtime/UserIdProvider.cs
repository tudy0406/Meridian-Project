using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Meridian.Api.Infrastructure.Realtime;

/// <summary>
/// Maps a SignalR connection to our numeric user id (from the JWT) so that
/// <c>Clients.User(id)</c> and per-user groups resolve to the right person.
/// </summary>
public sealed class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
