using System.Text.Json;
using Microsoft.Extensions.AI;

namespace OpsBot.Shared;

/// <summary>Console output helpers – colour-coded so the room can follow from the back.</summary>
public static class Ui
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Banner(string title, IList<AITool>? tools, ConsoleColor? accent = null, bool showPermissionMessage = true)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.ForegroundColor = accent ?? ConsoleColor.DarkYellow;
        Console.WriteLine();
        var heading = $"══ {title} ";
        if (heading.Length > 100) heading = heading[..97] + "...";
        Console.WriteLine(heading.PadRight(78, '═'));
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        if (tools is null || tools.Count == 0) Console.WriteLine("   tools: none");
        else
        {
            Console.WriteLine($"   tools ({tools.Count}):");
            foreach (var t in tools)
            {
                var desc = t is AIFunction f ? f.Description : "";
                desc = desc.Replace("\r", " ").Replace("\n", " ");
                var name = t.Name.Length > 28 ? t.Name[..25] + "..." : t.Name;
                var available = 100 - 7 - Math.Max(28, name.Length) - 1;
                if (desc.Length > available) desc = desc[..(available - 3)] + "...";
                var mark = t is AIFunction g && PermissionGate.IsWriteTool(g.Name) ? "✎" : "•";
                Console.WriteLine($"     {mark} {name,-28} {desc}");
            }
            if (showPermissionMessage) Console.WriteLine("     ✎ = write tool, asks for permission before running");
        }
        Console.ResetColor();
        Console.WriteLine("   type a question, 'stats', 'reset-stats', or 'exit'");
        Console.WriteLine();
    }

    public static void PromptMarker(string? label = null, ConsoleColor? accent = null)
    {
        Console.ForegroundColor = accent ?? ConsoleColor.Cyan;
        Console.Write(string.IsNullOrWhiteSpace(label) ? "You: " : $"You [{label}]: ");
        Console.ResetColor();
    }

    public static void ToolCall(FunctionCallContent call)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        var args = call.Arguments is null ? "{}" : JsonSerializer.Serialize(call.Arguments, Json);
        Console.WriteLine($"  → tool {call.Name} {args}");
        Console.ResetColor();
    }

    public static void ToolResult(FunctionResultContent result)
    {
        var text = result.Result switch
        {
            null when result.Exception is not null => result.Exception.Message,
            null => "(null)",
            string s => s,
            TextContent content => content.Text,
            JsonElement je => je.GetRawText(),
            _ => JsonSerializer.Serialize(result.Result, Json)
        };
        text = ReadMcpText(text);
        if (result.Exception is { } error && result.Result is not null)
            text += $" · {ReadMcpText(error.Message)}";
        text = text.Replace("\r", " ").Replace("\n", " ");
        var rateLimited = text.Contains("Rate limit exceeded", StringComparison.OrdinalIgnoreCase);
        Console.ForegroundColor = rateLimited ? ConsoleColor.Red : ConsoleColor.DarkYellow;
        if (rateLimited) Console.WriteLine("  ✖ Rate limit exceeded");
        if (text.Length > 220) text = text[..217] + "...";
        Console.WriteLine($"  ← {text}");
        Console.ResetColor();
    }

    private static string ReadMcpText(string text)
    {
        if (!text.TrimStart().StartsWith('{')) return text;
        try
        {
            using var json = JsonDocument.Parse(text);
            if (json.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                var parts = content.EnumerateArray()
                    .Where(p => p.TryGetProperty("type", out var type) && type.GetString() == "text")
                    .Select(p => p.GetProperty("text").GetString()).ToArray();
                if (parts.Length > 0) return string.Join(" ", parts);
            }
        }
        catch (JsonException)
        {
            // Exception messages can start with '{' without being JSON; retain the original text.
        }
        return text;
    }

    public static char AskPermission(string tool, string args)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine();
        Console.WriteLine($"  ⚠  {tool} wants to run with {args}");
        Console.Write("     Allow?  [y]es / [n]o / [a]lways for this tool › ");
        Console.ResetColor();
        char key;
        if (Console.IsInputRedirected)
        {
            var input = Console.ReadLine();
            key = string.IsNullOrWhiteSpace(input) ? '\0' : input.Trim()[0];
        }
        else
        {
            key = Console.ReadKey(intercept: true).KeyChar;
            Console.WriteLine(key);
        }
        return char.ToLowerInvariant(key);
    }

    public static void Answer(string name, string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine($"{name}:");
        Console.ResetColor();
        Console.WriteLine(text);
        Console.WriteLine();
    }

    public static void Info(string message) { Console.ForegroundColor = ConsoleColor.DarkGray; Console.WriteLine($"   {message}"); Console.ResetColor(); }
    public static void Error(string message) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"  ✖ {message}"); Console.ResetColor(); }
}
