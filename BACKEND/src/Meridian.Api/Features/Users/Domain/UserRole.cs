namespace Meridian.Api.Features.Users.Domain;

/// <summary>Join entity for the many-to-many User &lt;-&gt; Role relationship.</summary>
public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
