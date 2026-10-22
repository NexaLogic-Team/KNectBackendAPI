namespace Knect.Application.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "https://api.example.com";
    public string Audience { get; set; } = "knect-api";
    public string Key { get; set; } = "replace-with-long-random-key";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 14;
}
