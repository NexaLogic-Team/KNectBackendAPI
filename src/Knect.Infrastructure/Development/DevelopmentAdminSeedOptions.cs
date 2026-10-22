namespace Knect.Infrastructure.Development;

public sealed class DevelopmentAdminSeedOptions
{
    public const string SectionName = "DevelopmentAdmin";

    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
