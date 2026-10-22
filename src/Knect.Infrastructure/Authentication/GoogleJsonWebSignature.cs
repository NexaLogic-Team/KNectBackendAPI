using Google.Apis.Auth;

namespace Knect.Infrastructure.Authentication;

public static class GoogleTokenValidator
{
    public static async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string credential, string audience)
    {
        if (string.IsNullOrWhiteSpace(audience) || audience.Contains("replace", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Google OAuth client ID is not configured.");
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { audience }
        };

        return await GoogleJsonWebSignature.ValidateAsync(credential, settings);
    }
}
