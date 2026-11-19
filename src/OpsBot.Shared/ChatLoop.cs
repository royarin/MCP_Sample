using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

namespace OpsBot.Shared;

/// <summary>Interactive loop (or one-shot with --ask) shared by the console hosts; prints a scorecard after each answer.</summary>
public static class ChatLoop
{
    public static async Task RunAsync(AIAgent agent, IConfiguration config, UsageMeter meter, string runLabel = "run",
        string? promptLabel = null, ConsoleColor? accent = null)
    {
        AgentSession session = await agent.CreateSessionAsync();

        if (config["ask"] is { Length: > 0 } ask)
        {
            await AskOnceAsync(agent, session, meter, ask, runLabel);
            return;
        }

        while (true)
        {
            Ui.PromptMarker(promptLabel, accent);
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\nGoodbye! Stay on top of those incidents!");
                break;
            }
            if (input.Trim().Equals("stats", StringComparison.OrdinalIgnoreCase))
            {
                meter.PrintSession(runLabel);
                continue;
            }
            if (input.Trim().Equals("reset-stats", StringComparison.OrdinalIgnoreCase))
            {
                meter.ResetSession();
                Ui.Info("session statistics reset");
                continue;
            }
            await AskOnceAsync(agent, session, meter, input, runLabel);
        }
    }

    public static async Task<UsageMeter.Snapshot> AskOnceAsync(AIAgent agent, AgentSession session, UsageMeter meter,
        string question, string runLabel, bool printTraceId = false)
    {
        using var activity = Telemetry.Source.StartActivity("agent.question")
            ?? new Activity("agent.question").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity?.SetTag("agent.question", question.Length > 200 ? question[..200] : question);
        meter.Start();
        try
        {
            var response = await agent.RunAsync(question, session);
            if (string.IsNullOrWhiteSpace(response.Text))
            {
                meter.MarkFailure();
                Ui.Error("The model returned an empty answer.");
            }
            meter.Stop();
            Ui.Answer(OpsBotInstructions.AgentName, response.Text);
        }
        catch (Exception ex)
        {
            meter.MarkFailure();
            meter.Stop();
            Ui.Error(ex.Message);
        }
        activity?.SetTag("agent.tool_calls", meter.ToolCalls);
        activity?.SetTag("agent.input_tokens", meter.InputTokens);
        var snap = meter.Capture(runLabel);
        UsageMeter.PrintScorecard(snap);
        if (printTraceId) Ui.Info($"trace: {activity!.TraceId}");
        return snap;
    }
}
