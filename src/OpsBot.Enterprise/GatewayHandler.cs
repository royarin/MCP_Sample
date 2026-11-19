using Azure.Core;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OpsBot.Enterprise;

internal sealed class GatewayHandler(TokenCredential? credential, string scope) : DelegatingHandler
{
    public string? LastFailure { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (credential is not null)
        {
            var token = await credential.GetTokenAsync(new TokenRequestContext([scope]), ct);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }
        var response = await base.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var message = response.ReasonPhrase ?? "Gateway request failed";
            if (response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var json = JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("message", out var detail)) message = detail.GetString() ?? message;
            }
            LastFailure = $"{(int)response.StatusCode} - {message}";
        }
        return response;
    }
}
