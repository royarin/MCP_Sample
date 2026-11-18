# AGENTS.md – context for coding assistants working in this repository

This repository is the demo code for the Builders Foundry London session *"Your Existing Applications Are
Ready for AI Agents – Here's How MCP Makes It Possible"*. The full demo plan (environment, per-demo build
tasks, live scripts) lives next to the slides; this file is the part every task needs.

## Shared context

### A.1 The story

One ordinary ASP.NET Core application – **OpsDesk** (system health, owners, runbooks, incidents, with a Blazor dashboard) – is the "existing application". It is never modified. Four demo moments show, in order:

| # | Demo moment | Min | Claim it proves |
|---|---|---|---|
| 1 | An agent without your systems is just a chatbot | 5 | The model has no hands; hand-wiring works but is bespoke and host-bound |
| 2 | From existing API to MCP server – two ways | 10 | Describe the API once; any host – bought or built – discovers it at runtime |
| 2½ | Tool design is the UX for the model | 4 | Tool shape changes calls, turns, tokens, latency and error handling – measurably |
| 3 | Enterprise-grade MCP on Azure (pre-provisioned) | 6 | Identity, policy, catalogue and tracing are the API controls you already own |

**Everything server-side runs on Azure before the session** – API, dashboard and both MCP servers on Container Apps, fronted by APIM. Only the console agents run on the laptop; code is shown in VS Code but never started live.

Two hosts are used throughout the MCP part: **VS Code Copilot chat** (a host you didn't write) and **`OpsBot.Mcp`** (a host you did – a console agent with the same behaviours: tool list, tool-call trace, permission prompt before write tools, token scorecard, Entra identity).

### A.2 The headline questions (fixed – seed data supports them)

- **Briefing:** *"What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?"* → `inventory` is **Red** (replication lag), owner *Supply Chain Systems* / `#inventory-oncall`, runbook *Inventory DB Failover Runbook*, no open incident until the demo creates one.
- **Write action:** *"Raise a Critical incident for inventory – replication lag over threshold – reported by Arin, and notify the owner."* → creates `INC-0000n`, composes the owner e-mail; appears on the dashboard.
- **Error shaping:** *"Raise an incident for `inventry`."* (typo) → endpoint-shaped server returns the API's 404 JSON; task-shaped server answers *"Unknown system 'inventry' – did you mean inventory?"*
- **Governance:** the write action as *Reader* → blocked by policy with an escalation message; as *Operator* → succeeds.

### A.3 Technology baseline (proven in the base repo, April 2026)

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| API | ASP.NET Core controllers, EF Core 10 + SQLite (migrations + seed), `Microsoft.AspNetCore.OpenApi` |
| UI | Blazor Server (`OpsDesk.Web`) |
| MCP servers | `ModelContextProtocol.AspNetCore` **1.2.0**, stateless Streamable HTTP, `[McpServerToolType]` classes, prompts |
| MCP client | `ModelContextProtocol` 1.2.0 – `McpClient.CreateAsync(new HttpClientTransport(...))`, `ListToolsAsync()` (tools are `AIFunction`s) |
| Agent | Microsoft Agent Framework (`Microsoft.Agents.AI.OpenAI` 1.0.0-preview) – `chatClient.AsAIAgent(instructions, name, tools)`, `CreateSessionAsync()`, `RunAsync(input, session)`; `Microsoft.Extensions.AI.OpenAI` 10.2 preview; `Azure.AI.OpenAI` 2.8 beta |
| LLM | Azure OpenAI `gpt-4o` (or `gpt-4.1`) |
| Token counting | `Microsoft.ML.Tokenizers` (Tiktoken for the deployed model) for tool-definition size; `ChatResponse.Usage` for actual usage |
| Telemetry | OpenTelemetry → Application Insights (`Azure.Monitor.OpenTelemetry.AspNetCore` in web apps, exporter in console); APIM diagnostics; W3C correlation |
| Gateway / identity / catalogue | Azure API Management (one instance, two MCP servers), Microsoft Entra ID (app roles), Azure API Center |
| Hosting | Azure Container Apps for API, Web, both MCP servers (all pre-deployed; nothing runs locally during the talk except the console agents); Bicep or `azd` |
| Dev tools | VS Code + Copilot agent mode (`mcp.json`), MCP Inspector (`npx @modelcontextprotocol/inspector`), REST Client |

> Instruction for the assistant: reuse the exact package versions already in the repo's `.csproj` files; for new packages (`ModelContextProtocol` client, `Microsoft.ML.Tokenizers`, OpenTelemetry) check NuGet for the latest stable and adapt API names if they differ from the skeletons here.

### A.4 Repository layout (after the work)

```
MCP_Sample/
├─ README.md                       NEW   slides ↔ projects ↔ demos
├─ AGENTS.md                       NEW   this Part A
├─ MCP_Sample.slnx                 +2 projects
├─ src/
│  ├─ OpsDesk.Api/                 existing · frozen (config only)
│  ├─ OpsDesk.Web/                 existing · frozen (config only)
│  ├─ OpsBot.NoTools/              existing · + --ask, user-secrets, env
│  ├─ OpsBot.Tools/                existing · + ToolTraceChatClient, --ask
│  ├─ OpsDesk.Mcp/                 existing · endpoint-shaped server, + OTel, port 5001
│  ├─ OpsDesk.Mcp.Tasks/           NEW     · task-shaped server, port 5002
│  ├─ OpsBot.Mcp/                  NEW     · console host over MCP (trace, permissions, scorecard, Entra)
│  └─ OpsBot.Shared/               NEW     · ToolTraceChatClient, PermissionGate, UsageMeter, console UI (referenced by OpsBot.Tools and OpsBot.Mcp)
├─ apim/                           NEW   click-path, policies, exported config
├─ infra/                          NEW   Bicep/azd, post-deploy script, entra.md
├─ vscode/mcp.json                 NEW   opsdesk-local · opsdesk-tasks · opsdesk-apim · opsdesk-secure
├─ scripts/                        NEW   demo1 · demo2 · demo-tooldesign · demo3 · reset · appinsights.kql
└─ fallback/                       NEW   demo3-appinsights.mp4 (only recording needed)
```

### A.5 Ground rules

- `OpsDesk.Api` and `OpsDesk.Web` are frozen after tag `demo0-baseline`. Anything you are tempted to change goes into `README.md › "Things we were tempted to change"` – it is talk material.
- Secrets only via `dotnet user-secrets` (shared id `opsdesk-mcp-demos`) or environment; `appsettings.Development.json` git-ignored. **Rotate the Azure OpenAI key that is currently committed.**
- Every console run supports `--ask "<question>"` (one shot) so demos are copy-paste free.
- Servers are never started live. `dotnet run` appears only in rehearsal notes; demos use the Azure URLs. Local `launchSettings` stay for development.
- Every tool call is visible: console trace in your hosts, the tools UI in Copilot.

---

