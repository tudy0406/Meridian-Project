using Meridian.Api.Common.Domain;

namespace Meridian.Api.Features.Users.Domain;

/// <summary>A named permission set (Administrator, HR Employee, Manager, ...).</summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
