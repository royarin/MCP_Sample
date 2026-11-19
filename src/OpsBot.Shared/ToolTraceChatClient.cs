using System.Text.Json;
using Microsoft.Extensions.AI;

namespace OpsBot.Shared;

/// <summary>
/// Sits underneath the agent on the raw <see cref="IChatClient"/>. Prints every tool call the model
/// requests and every tool result fed back to it, and feeds the <see cref="UsageMeter"/> with token
/// usage and model turns. Identical for hand-wired functions (OpsBot.Tools) and MCP tools (OpsBot.Mcp):
/// the agent cannot tell where a capability comes from – which is the point of the demo.
/// </summary>
public sealed class ToolTraceChatClient(IChatClient inner, UsageMeter meter, int? maxOutputTokens = 1200) : DelegatingChatClient(inner)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HashSet<string> _seenResults = [];

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();

        // Results produced since the previous model call → "← result"
        foreach (var r in list.SelectMany(m => m.Contents).OfType<FunctionResultContent>())
            if (_seenResults.Add(r.CallId))
            {
                Ui.ToolResult(r);
                var text = r.Result is string s ? s : JsonSerializer.Serialize(r.Result, Json);
                if (r.Exception is not null ||
                    text.Contains("Rate limit exceeded", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("\"isError\":true", StringComparison.OrdinalIgnoreCase))
                    meter.MarkFailure();
            }

        if (maxOutputTokens is not null)
        {
            if (maxOutputTokens <= 0) throw new ArgumentOutOfRangeException(nameof(maxOutputTokens));
            options = options?.Clone() ?? new ChatOptions();
            options.MaxOutputTokens ??= maxOutputTokens;
        }
        var response = await base.GetResponseAsync(list, options, cancellationToken);
        meter.ModelTurn(response.Usage);

        // Calls the model just requested → "→ tool name {args}"
        foreach (var c in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
        {
            meter.ToolCall();
            Ui.ToolCall(c);
        }
        return response;
    }
}
