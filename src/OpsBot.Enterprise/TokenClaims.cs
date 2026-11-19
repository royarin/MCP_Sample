using System.Text.Json;

namespace OpsBot.Enterprise;

internal static class TokenClaims
{
    public static string Roles(string token)
    {
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
        return json.RootElement.TryGetProperty("roles", out var roles)
            ? string.Join(", ", roles.EnumerateArray().Select(r => r.GetString()))
            : "none";
    }
}
