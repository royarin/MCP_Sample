using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace OpsBot.Shared;

/// <summary>Per-question counters: tool calls, model turns, tokens, wall time. Printed as a scorecard.</summary>
public sealed class UsageMeter
{
    private readonly Stopwatch _sw = new();
    public int ToolCalls { get; private set; }
    public int ModelTurns { get; private set; }
    public long InputTokens { get; private set; }
    public long OutputTokens { get; private set; }
    public TimeSpan Elapsed => _sw.Elapsed;
    public bool Success { get; private set; } = true;
    private int _questions, _sessionToolCalls, _sessionModelTurns;
    private long _sessionInputTokens, _sessionOutputTokens;
    private TimeSpan _sessionElapsed;

    public void Start() { ToolCalls = 0; ModelTurns = 0; InputTokens = 0; OutputTokens = 0; Success = true; _sw.Restart(); }
    public void Stop()
    {
        if (!_sw.IsRunning) return;
        _sw.Stop();
        _questions++;
        _sessionToolCalls += ToolCalls;
        _sessionModelTurns += ModelTurns;
        _sessionInputTokens += InputTokens;
        _sessionOutputTokens += OutputTokens;
        _sessionElapsed += Elapsed;
    }
    public void MarkFailure() => Success = false;
    public void ToolCall() => ToolCalls++;
    public void ModelTurn(UsageDetails? usage)
    {
        ModelTurns++;
        InputTokens += usage?.InputTokenCount ?? 0;
        OutputTokens += usage?.OutputTokenCount ?? 0;
    }

    public Snapshot Capture(string label) => new(label, ToolCalls, ModelTurns, InputTokens, OutputTokens, Elapsed) { Success = Success };

    public record Snapshot(string Label, int ToolCalls, int ModelTurns, long InputTokens, long OutputTokens, TimeSpan Elapsed)
    {
        public bool Success { get; init; } = true;
    }

    public void ResetSession()
    {
        _questions = _sessionToolCalls = _sessionModelTurns = 0;
        _sessionInputTokens = _sessionOutputTokens = 0;
        _sessionElapsed = TimeSpan.Zero;
    }

    public void PrintSession(string label) =>
        Console.WriteLine($"session [{label}] · {_questions} questions · {_sessionToolCalls} tool calls · " +
            $"{_sessionModelTurns} model turns · {_sessionInputTokens:N0} input tokens · " +
            $"{_sessionOutputTokens:N0} output tokens · {_sessionElapsed.TotalSeconds:0.0} s");

    public static void PrintScorecard(params Snapshot[] runs)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        var heading = runs.Length == 1 ? $"  ┌─ scorecard [{runs[0].Label}] " : "  ┌─ scorecard ";
        if (heading.Length > 97) heading = heading[..94] + "...";
        Console.WriteLine(heading.PadRight(Math.Max(heading.Length, 26 + 16 * runs.Length), '─') + "┐");
        Console.ResetColor();
        string Row(string name, Func<Snapshot, string> f) =>
            "  │ " + name.PadRight(22) + string.Concat(runs.Select(r => f(r).PadLeft(15))) + " │";
        Console.WriteLine(Row("", r => r.Label.Length > 14 ? r.Label[..14] : r.Label));
        Console.WriteLine(Row("tool calls", r => r.ToolCalls.ToString()));
        Console.WriteLine(Row("model turns", r => r.ModelTurns.ToString()));
        Console.WriteLine(Row("input tokens", r => r.InputTokens.ToString("N0")));
        Console.WriteLine(Row("output tokens", r => r.OutputTokens.ToString("N0")));
        Console.WriteLine(Row("wall time", r => $"{r.Elapsed.TotalSeconds:0.0}s"));
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  └".PadRight(27 + 16 * runs.Length, '─') + "┘");
        Console.ResetColor();
        Console.WriteLine();
    }
}
