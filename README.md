# OpsDesk – Your Existing Applications Are Ready for AI Agents

Demo code for the Builders Foundry London session **"Your Existing Applications Are Ready for AI Agents – Here's How MCP Makes It Possible"**
(originally built for Global Azure Bulgaria 2026 as *MCP on Azure: From PoC to Platform*).

One ordinary ASP.NET Core application – **OpsDesk** (system health, owners, runbooks, incidents, Blazor dashboard) – is the
"existing application". It is never modified. Four demos show how agents reach it: first by hand, then through MCP, then
with production controls on Azure.

## Projects

| Project | Role | Demo |
|---|---|---|
| `src/OpsDesk.Api` | The existing application: controllers, EF Core + SQLite, seed data, OpenAPI. **Frozen.** | backdrop |
| `src/OpsDesk.Web` | Blazor dashboard – the audience watches agent actions land here | backdrop |
| `src/OpsBot.NoTools` | Console agent, no tools → cannot answer | 1 |
| `src/OpsBot.Tools` | Console agent, five hand-wired functions over HttpClient (`OpsDeskFunctions.cs` is what MCP replaces) | 1 |
| `src/OpsDesk.Mcp` | **Endpoint-shaped** MCP server: one tool per API operation, raw JSON (port 5001) | 2, 3 |
| `src/OpsDesk.Mcp.Tasks` | **Task-shaped** MCP server: `get_system_briefing`, `get_oncall_handover`, `find_system`, `raise_incident`, `update_system_status` (port 5002) | 3 |
| `src/OpsBot.Mcp` | Subscription-key-only console MCP host: interactive labels, permissions, session stats; rehearsal comparison | 3 |
| `src/OpsBot.Enterprise` | Interactive Entra MCP host: secure/open API-generated tools, API Center discovery, question trace IDs | 4 |
| `src/OpsBot.Shared` | Tool-call trace, **permission gate** (y / n / always before write tools), usage scorecard, tool-catalogue token meter | – |

Hosts used in the demos: **VS Code Copilot chat** (a host you didn't write) and **OpsBot.Mcp** (a host you did).

## Demos

| # | Demo | What runs where |
|---|---|---|
| 1 | An agent without your systems is just a chatbot | `OpsBot.NoTools`, `OpsBot.Tools` on the laptop → API in Azure |
| 2 | From existing API to MCP server – two ways | Copilot → `OpsDesk.Mcp` (App Service) and `opsdesk-apim` (APIM, no code) |
| 3 | Tool design is the UX for the model | Two interactive `OpsBot.Mcp` panes → endpoint-shaped vs task-shaped tools; calls, turns, tokens, time, typo and approval flows |
| 4 | Enterprise-grade MCP on Azure (pre-provisioned) | `OpsBot.Enterprise` → curated `opsdesk-apim-secure`: Entra identity, read-only surface, six calls/minute, API Center, cross-resource trace |

Headline question (seed data: `inventory` is **Red**):
> *What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?*

## Getting started

```powershell
# prerequisites: .NET 10 SDK, Azure CLI signed into the Foundry project tenant,
# a model deployed to the Foundry project, optional VS Code + Copilot and Node for MCP Inspector
az login --tenant "<tenant-id>"
.\infra\configure-entra.ps1 -TenantId "<tenant-id>"
.\infra\configure-apim.ps1 -SubscriptionId "<subscription-id>" -ResourceGroup "<resource-group>" `
  -BaseName "opsdesk-demo" -UniqueSuffix "<unique-suffix>" -FoundryProjectEndpoint "<foundry-project-endpoint>"
dotnet build MCP_Sample.slnx
```

`configure-apim.ps1` merges endpoints, Foundry settings, and the shared `demo-open` key into ignored local development settings. It never prints keys or overwrites existing Entra secrets. Supply a real Foundry project and deployed model. Only console hosts run locally during the talk; API, dashboard, and MCP servers stay deployed.

Each console project's tracked `appsettings.json` is a credential-free template. Copy it to `appsettings.Development.json` for manual setup, fill in your own Foundry project endpoint and deployment, and configure the API/APIM endpoints and credentials needed by that host. Empty strings are private-value placeholders; `<apim-name>` must be replaced with your gateway name. Leave telemetry empty to disable export, or supply your existing Application Insights connection string privately. The setup scripts also seed missing development keys from these templates while preserving existing local values.

The development files are ignored by Git. Demo 3's `label`, `accent`, and `permissions` and Demo 4's `no-key`, `no-token`, `identity`, `open`, and `discover` are included explicitly so the [presenter walkthrough](docs/DEMO-PRESENTATION.md) requires editing values, not inventing missing keys. Keep `Foundry:MaxOutputTokens` null for the earlier hosts' model defaults; Demo 3/4 use 1200. User-secrets and environment variables still override JSON; keep credentials off the projected screen.

The script sets `Foundry:Credential` to `AzureCliCredential` so the local agents use the Azure CLI login above. For existing local settings, add `"Credential": "AzureCliCredential"` inside `Foundry` in each console project's `appsettings.Development.json`. Without this setting, `DefaultAzureCredential` may select a different cached IDE identity or tenant, causing Foundry to return `Workspace not found` even for a valid project endpoint. Use `DefaultAzureCredential` explicitly when its credential chain is intended.

```bash
dotnet run --project src/OpsBot.NoTools -- --ask "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?"
dotnet run --project src/OpsBot.Tools   -- --ask "..."
dotnet run --project src/OpsBot.Mcp     -- --ask "..."                                    # Mcp:Endpoint from config
dotnet run --project src/OpsBot.Mcp     -- --compare <opsdesk-code-apim-url> <opsdesk-tasks-apim-url> --ask "..."
dotnet run --project src/OpsBot.Enterprise
```

Flags: `--ask "<question>"` one shot · `--permissions ask|auto` (default `ask`: write tools prompt `[y]es / [n]o / [a]lways`) · `--compare <a> <b>`.

These are rehearsal flags for the first three hosts, not the on-stage script. `OpsBot.Enterprise` is interactive only and has no local permission gate: its secure catalog has no writes.

### Demo 3

Run the same inventory briefing once against `opsdesk-code` and once against `opsdesk-tasks`, using `OpsBot.Mcp` with the same model and credentials. Compare the automatically printed scorecards: model turns, tool calls, input/output tokens and elapsed time. The [presenter walkthrough](docs/DEMO-PRESENTATION.md) contains the two-run sequence.

For offstage rehearsal, `.\scripts\demo3-measure.ps1 -BaseName "<base-name>"` writes a read-only measurement report to ignored `artifacts\demo3-measurements.md`. It requires actual model usage for both runs and refuses to write a zero-token or failed measurement.

Measured against the deployed endpoints with `gpt-4o` on 2026-11-19:

| Metric | Endpoint-shaped | Task-shaped |
|---|---:|---:|
| Tools / catalogue tokens | 10 / 590 | 5 / 520 |
| Tool calls / model turns | 4 / 2 | 1 / 2 |
| Input / output tokens | 3033 / 331 | 2888 / 190 |
| Wall time (s) | 10.08 | 13.29 |

These are observed results, not latency guarantees: fewer tool calls did not make the task-shaped run faster in this sample. Use the [demo manual](docs/DEMO-MANUAL.md) for preparation and rehearsal and the [presenter walkthrough](docs/DEMO-PRESENTATION.md) on stage. These are the only persisted documents under `docs`.

### Demo 4

`.\scripts\demo4.ps1 -BaseName "<base-name>" -Mode normal` launches the interactive Enterprise host. Other modes: `no-key`, `no-token`, `norole`, `open`, and `discover`. Failed connections remain visible; use `reconnect` or `exit`. A normal secure connection discovers exactly five read tools, answers the briefing, and cannot create an incident. The all-systems question hits the dedicated six-tool-call/minute gateway budget and yields a partial answer explaining the limit.

`--open` selects the eleven-tool Demo 2 twin. Both endpoints reuse the existing `demo-open` product/key and Application Insights setup; only the dedicated secure endpoint has JWT validation, curation, the per-agent rate budget, and its API-scoped diagnostics. `--discover opsdesk-apim-secure` resolves the runtime from API Center using the Azure CLI identity, independent of `Mcp:Endpoint`. Each answer prints its question trace ID; substitute it and your existing App Insights resource names in `scripts\demo4-trace.kql`.

For an existing Demo 1/2 deployment, run `configure-apim.ps1` with `-EnterpriseOnly`: it leaves their gateway resources and local settings untouched. The full setup preserves existing generated MCP APIs because replacing one clears its tool collection. `-RefreshApi` explicitly reimports the otherwise frozen REST API.

### Demo 2 in VS Code

Open this repository folder in VS Code; `.vscode/mcp.json` defines the two Azure-hosted Demo 2 servers. No local server or file copying is required. Start `opsdesk-code` from the configuration file or **MCP: List Servers**, approve the server trust prompt, and enter your APIM service name and `demo-open` subscription key when prompted. The key is a password input, not a value to save in this repository.

In Copilot Chat's agent mode, enable only that server's tools and ask the headline inventory question above. Inspect the tool calls, then ask: **"Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner."** Review and approve the write-tool call, then check the dashboard. Disable those tools and enable `opsdesk-apim` to repeat the briefing using the API-generated tools. Use one server at a time so Copilot cannot mix the two implementations.

This file uses VS Code's editor-managed MCP input prompts. On builds that distinguish the **Local** and **Copilot** session targets, use **Local**: the newer Copilot harness does not forward definitions containing `${input:...}`. The templates in `vscode/` remain available for the other demos.

## Folders

- `.vscode/` – workspace-discovered `mcp.json` for Demo 2; prompts for the APIM name and subscription key
- `apim/` – API/MCP route documentation, `policy-backend.xml`, `policy-mcp-secure.xml`
- `infra/` – `main.bicep` (Linux App Service ×4, APIM, API Center, paired Log Analytics/App Insights), `deploy-apps.ps1`, `postdeploy.ps1`, `entra.md`
- `vscode/` – MCP connection templates; `mcp.local.json` is for rehearsal only
- `scripts/` – `urls.ps1`, `reset.ps1`, `demo1–4.ps1`, `tokens.ps1`, `appinsights.kql`, `demo4-expected.md`
- `fallback/` – recordings (git-ignored)

## Azure provisioning

The templates create a Linux P0v3 App Service plan, four .NET 10 apps, Developer APIM, API Center Free in East US, and separate Log Analytics/App Insights pairs for the apps and MCP servers. The APIM publisher address in `infra\main.bicepparam` is a placeholder; replace it with a monitored address before provisioning. Tenant, subscription, resource group, name stem, uniqueness suffix and Foundry endpoint are caller inputs, not personal deployment values stored in the repository.

```powershell
$TenantId = "<tenant-id>"
$SubscriptionId = "<subscription-id>"
$ResourceGroup = "<resource-group>"
$BaseName = "opsdesk-demo"
$UniqueSuffix = "<unique-lowercase-suffix>"
$FoundryProjectEndpoint = "<foundry-project-endpoint>"
az login --tenant $TenantId
$env:OPSDESK_BASE_NAME = $BaseName
$env:OPSDESK_UNIQUE_SUFFIX = $UniqueSuffix
az deployment group validate --subscription $SubscriptionId -g $ResourceGroup -n main -f infra\main.bicep -p infra\main.bicepparam
az deployment group create --subscription $SubscriptionId -g $ResourceGroup -n main -f infra\main.bicep -p infra\main.bicepparam
.\infra\deploy-apps.ps1 -SubscriptionId $SubscriptionId -ResourceGroup $ResourceGroup -BaseName $BaseName -UniqueSuffix $UniqueSuffix
.\infra\configure-entra.ps1 -TenantId $TenantId
.\infra\configure-apim.ps1 -TenantId $TenantId -SubscriptionId $SubscriptionId -ResourceGroup $ResourceGroup -BaseName $BaseName -UniqueSuffix $UniqueSuffix -FoundryProjectEndpoint $FoundryProjectEndpoint
.\infra\postdeploy.ps1 -SubscriptionId $SubscriptionId -ResourceGroup $ResourceGroup -BaseName $BaseName -UniqueSuffix $UniqueSuffix
```

The App Service plan and Developer APIM are billable. Bicep provisions infrastructure, `deploy-apps.ps1` publishes apps, `configure-entra.ps1` provisions demo identities, `configure-apim.ps1` configures gateway controls, and `postdeploy.ps1` registers API Center metadata. Use `deploy-apps.ps1 -AppNames mcp-tasks` to deploy just that changed server.

### Resource-name uniqueness

For example, `-BaseName opsdesk-demo -UniqueSuffix lab42` produces `app-opsdesk-demo-lab42-api`, `apim-opsdesk-demo-lab42`, `apic-opsdesk-demo-lab42`, and matching plan/telemetry names. Choose your own available suffix; no person's name or account identifier is built into the naming logic. Use lowercase letters, digits and internal hyphens; the combined `BaseName-UniqueSuffix` must be at most 45 characters. Azure still enforces global availability for public host names.

Bicep accepts `baseName` and `uniqueSuffix` parameters; the checked-in `.bicepparam` reads their values from `OPSDESK_BASE_NAME` and `OPSDESK_UNIQUE_SUFFIX`. PowerShell deployment/configuration and demo helper scripts accept `-BaseName` and `-UniqueSuffix` explicitly. Supply the same pair at every step. Bicep's `resourceBaseName` output is the composed stem.

Existing deployments can keep passing their complete, already-unique stem as `-BaseName` and omit `-UniqueSuffix`. Do not append the same suffix twice or change naming inputs when maintaining an existing environment: different inputs target different resource names, not an in-place rename. When using the parameter file in this legacy mode, ensure `OPSDESK_UNIQUE_SUFFIX` is unset.

Demo helpers use the same naming rule, for example `.\scripts\demo1.ps1 -BaseName opsdesk-demo -UniqueSuffix lab42` or `.\scripts\reset.ps1 -BaseName opsdesk-demo -UniqueSuffix lab42`. This is offstage setup/rehearsal; the presenter walkthrough remains interactive. Logical API IDs such as `opsdesk-apim-secure` and the Entra roles are unchanged.

## Things we were tempted to change in the API (and didn't)

- Add a customer-facing "briefing" endpoint. That is exactly what `OpsDesk.Mcp.Tasks/get_system_briefing` does – outside the API.
- Return friendlier errors. The task-shaped server translates them instead.
- Add an API key. The gateway owns authentication; the API stays as it was.

## Package versions

Exact versions are pinned in the `.csproj` files (.NET 10, MCP 2.4.0, Agent Framework 1.28.0). Real response usage feeds the scorecards; `Microsoft.ML.Tokenizers` estimates catalog size. OpenTelemetry/Azure Monitor export the console and MCP traces. The Demo 3/4 hosts bound output-token reservation to 1200 by default (`Foundry:MaxOutputTokens` overrides it); the earlier hosts retain their existing defaults.

---
*Community demo material. Review product names and preview-feature availability (APIM MCP servers, API Center MCP type, Entra Agent ID) before reuse.*
