using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace OpsBot.Shared;

/// <summary>
/// The console equivalent of Copilot's tool-approval dialog. Every tool whose name looks like a write
/// (Create*, Update*, Notify*, raise_*, update_*, notify_*, cancel_* …) is wrapped so that, before it
/// runs, the user is asked:  Allow &lt;tool&gt; {args}?  [y]es / [n]o / [a]lways for this tool
/// A denial does not throw: the model receives an explanation and recovers gracefully.
/// </summary>
public static class PermissionGate
{
    public enum Mode { Ask, Auto }

    private static readonly Regex WritePattern = new(
        @"^(create|update|notify|raise|cancel|delete|set|reset|send|post)[_a-z]*|_(create|update|notify|raise|cancel|delete)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsWriteTool(string name) => WritePattern.IsMatch(name);

    public static IList<AITool> Wrap(IEnumerable<AITool> tools, Mode mode) =>
        tools.Select(t => mode == Mode.Ask && t is AIFunction f && IsWriteTool(f.Name) ? new Gated(f) : t).ToList();

    private sealed class Gated(AIFunction inner) : DelegatingAIFunction(inner)
    {
        private static readonly HashSet<string> AlwaysAllowed = new(StringComparer.OrdinalIgnoreCase);
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            if (!AlwaysAllowed.Contains(Name))
            {
                var args = JsonSerializer.Serialize(arguments.ToDictionary(kv => kv.Key, kv => kv.Value), Json);
                var answer = Ui.AskPermission(Name, args);
                switch (answer)
                {
                    case 'a': AlwaysAllowed.Add(Name); break;
                    case 'y': break;
                    default:
                        Ui.Info($"declined {Name}");
                        return $"The user declined the call to {Name}. Do not retry it. Explain what you would have done and ask how they want to proceed.";
                }
            }
            return await base.InvokeCoreAsync(arguments, cancellationToken);
        }
    }
}
