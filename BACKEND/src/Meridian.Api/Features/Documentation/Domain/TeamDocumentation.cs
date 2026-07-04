using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Teams.Domain;

namespace Meridian.Api.Features.Documentation.Domain;

/// <summary>
/// A categorized section of a team's onboarding documentation. Splitting docs
/// into sections (instead of one large blob) makes updates and navigation easy.
/// </summary>
public class TeamDocumentation : BaseEntity
{
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public DocumentationCategory Category { get; set; }
    public string Content { get; set; } = string.Empty;

    public int CreatedById { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
