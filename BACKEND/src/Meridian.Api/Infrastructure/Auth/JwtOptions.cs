namespace Meridian.Api.Infrastructure.Auth;

/// <summary>Strongly-typed JWT settings bound from configuration ("Jwt" section).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Meridian";
    public string Audience { get; set; } = "Meridian.Client";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 120;
}
