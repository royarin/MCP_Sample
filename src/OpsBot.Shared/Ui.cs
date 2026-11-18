using System.Text.Json;
using Microsoft.Extensions.AI;

namespace OpsBot.Shared;

/// <summary>Console output helpers – colour-coded so the room can follow from the back.</summary>
public static class Ui
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Banner(string title, IList<AITool>? tools)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine();
        Console.WriteLine($"══ {title} ".PadRight(78, '═'));
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
                if (desc.Length > 72) desc = desc[..69] + "...";
                var mark = t is AIFunction g && PermissionGate.IsWriteTool(g.Name) ? "✎" : "•";
                Console.WriteLine($"     {mark} {t.Name,-28} {desc}");
            }
            Console.WriteLine("     ✎ = write tool, asks for permission before running");
        }
        Console.ResetColor();
        Console.WriteLine("   type a question, or 'exit'");
        Console.WriteLine();
    }

    public static void PromptMarker() { Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("You: "); Console.ResetColor(); }

    public static void ToolCall(FunctionCallContent call)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        var args = call.Arguments is null ? "{}" : JsonSerializer.Serialize(call.Arguments, Json);
        Console.WriteLine($"  → tool {call.Name} {args}");
        Console.ResetColor();
    }

    public static void ToolResult(FunctionResultContent result)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        var text = result.Result switch
        {
            null => "(null)",
            string s => s,
            JsonElement je => je.GetRawText(),
            _ => JsonSerializer.Serialize(result.Result, Json)
        };
        text = text.Replace("\r", " ").Replace("\n", " ");
        if (text.Length > 220) text = text[..217] + "...";
        Console.WriteLine($"  ← {text}");
        Console.ResetColor();
    }

    public static char AskPermission(string tool, string args)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine();
        Console.WriteLine($"  ⚠  {tool} wants to run with {args}");
        Console.Write("     Allow?  [y]es / [n]o / [a]lways for this tool › ");
        Console.ResetColor();
        var key = Console.ReadKey(intercept: true).KeyChar;
        Console.WriteLine(key);
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
