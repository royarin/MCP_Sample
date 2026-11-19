using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpsBot.Shared;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OpsBot.Enterprise;

internal static class ApiCenterResolver
{
    public static async Task<string> ResolveAsync(IConfiguration config, string apiId)
    {
        string Required(string key) => config[$"ApiCenter:{key}"] is { Length: > 0 } value
            ? Uri.EscapeDataString(value) : throw new InvalidOperationException($"ApiCenter:{key} is required.");
        var uri = $"https://management.azure.com/subscriptions/{Required("SubscriptionId")}" +
            $"/resourceGroups/{Required("ResourceGroup")}/providers/Microsoft.ApiCenter/services/{Required("Name")}" +
            $"/workspaces/default/apis/{Uri.EscapeDataString(apiId)}/deployments?api-version=2024-03-01";
        var token = await new AzureCliCredential().GetTokenAsync(
            new TokenRequestContext(["https://management.azure.com/.default"]));
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        using var response = await http.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API Center discovery failed: HTTP {(int)response.StatusCode}.",
                null, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var deployment in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            foreach (var endpoint in deployment.GetProperty("properties").GetProperty("server").GetProperty("runtimeUri").EnumerateArray())
            {
                if (endpoint.GetString() is { } value && Uri.TryCreate(value, UriKind.Absolute, out var address) &&
                    address.Scheme == Uri.UriSchemeHttps)
                {
                    Ui.Info($"resolved from API Center: {value}");
                    return value;
                }
            }
        }
        throw new InvalidOperationException($"API Center has no HTTPS deployment for '{apiId}'.");
    }
}
