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
| `src/OpsDesk.Mcp.Tasks` | **Task-shaped** MCP server: `get_system_briefing`, `get_oncall_handover`, `find_system`, `raise_incident`, `update_system_status` (port 5002) | 3, 4 |
| `src/OpsBot.Mcp` | Console host that discovers its tools over MCP; `--compare` scorecard; Entra identity | 2, 3, 4 |
| `src/OpsBot.Shared` | Tool-call trace, **permission gate** (y / n / always before write tools), usage scorecard, tool-catalogue token meter | – |

Hosts used in the demos: **VS Code Copilot chat** (a host you didn't write) and **OpsBot.Mcp** (a host you did).

## Demos

| # | Demo | What runs where |
|---|---|---|
| 1 | An agent without your systems is just a chatbot | `OpsBot.NoTools`, `OpsBot.Tools` on the laptop → API in Azure |
| 2 | From existing API to MCP server – two ways | Copilot → `OpsDesk.Mcp` (App Service) and `opsdesk-apim` (APIM, no code) |
| 3 | Tool design is the UX for the model | `OpsBot.Mcp --compare` → `OpsDesk.Mcp` vs `OpsDesk.Mcp.Tasks`: calls · turns · tokens · time; typo test; permission prompt |
| 4 | Enterprise-grade MCP on Azure (pre-provisioned) | `opsdesk-secure` in APIM: Entra `validate-jwt`, role-based write-tool block, API Center, App Insights trace |

Headline question (seed data: `inventory` is **Red**):
> *What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?*

## Getting started

```bash
# prerequisites: .NET 10 SDK, an Azure OpenAI deployment (gpt-4o), optional: VS Code + Copilot, Node for MCP Inspector
dotnet build MCP_Sample.slnx

# secrets are shared by OpsBot.* (user-secrets id opsdesk-mcp-demos) – run from any OpsBot project folder
cd src/OpsBot.Mcp
dotnet user-secrets set AzureOpenAI:Endpoint       https://<resource>.openai.azure.com/
dotnet user-secrets set AzureOpenAI:ApiKey         <key>          # omit to use az login
dotnet user-secrets set AzureOpenAI:DeploymentName gpt-4o
dotnet user-secrets set OpsDesk:BaseUrl            https://app-<base-name>-api.azurewebsites.net   # or http://localhost:5047
dotnet user-secrets set Mcp:Endpoint               https://app-<base-name>-mcp.azurewebsites.net/mcp
```

Run locally (one terminal each): `OpsDesk.Api` (:5047), `OpsDesk.Web`, `OpsDesk.Mcp` (:5001), `OpsDesk.Mcp.Tasks` (:5002).
For the session everything server-side is deployed to Azure (`infra/`) and only the console agents run on the laptop.

```bash
dotnet run --project src/OpsBot.NoTools -- --ask "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?"
dotnet run --project src/OpsBot.Tools   -- --ask "..."
dotnet run --project src/OpsBot.Mcp     -- --ask "..."                                    # Mcp:Endpoint from config
dotnet run --project src/OpsBot.Mcp     -- --compare http://localhost:5001/mcp http://localhost:5002/mcp --ask "..."
dotnet run --project src/OpsBot.Mcp     -- --Mcp:Endpoint https://<apim>/opsdesk-secure/mcp --Mcp:Auth entra
```

Flags: `--ask "<question>"` one shot · `--permissions ask|auto` (default `ask`: write tools prompt `[y]es / [n]o / [a]lways`) · `--compare <a> <b>`.

## Folders

- `apim/` – click-path for *Expose API as MCP server*, `policy-backend.xml`, `policy-mcp-auth.xml`, `policy-mcp-governance.xml`
- `infra/` – `main.bicep` (Linux App Service ×4, APIM, API Center, paired Log Analytics/App Insights), `deploy-apps.ps1`, `postdeploy.ps1`, `entra.md`
- `vscode/` – `mcp.json` (Azure endpoints: `opsdesk-code`, `opsdesk-tasks`, `opsdesk-apim`, `opsdesk-secure`), `mcp.local.json`
- `scripts/` – `urls.ps1`, `reset.ps1`, `demo1–4.ps1`, `tokens.ps1`, `appinsights.kql`, `demo4-expected.md`
- `fallback/` – recordings (git-ignored)

## Azure provisioning

The templates mirror the source `<resource-group>` layout in Sweden Central: a Linux P0v3 App Service plan, four .NET 10 apps, Developer APIM, API Center Free in East US, and separate Log Analytics/App Insights pairs for the apps and MCP servers. The APIM publisher address in `infra/main.bicepparam` is a placeholder; replace it with a monitored address before provisioning.

```powershell
az deployment group validate --subscription <subscription-id> -g <resource-group> -n main -f infra/main.bicep -p infra/main.bicepparam
az deployment group create --subscription <subscription-id> -g <resource-group> -n main -f infra/main.bicep -p infra/main.bicepparam
.\infra\deploy-apps.ps1 -SubscriptionId <subscription-id>
.\infra\postdeploy.ps1 -ResourceGroup <resource-group> -TenantId "<tenant-guid>" -McpResourceAppId "<resource-app-id>"
```

The App Service plan and Developer APIM are billable resources. The Bicep deployment provisions infrastructure; the next script publishes the four applications, and `postdeploy.ps1` configures APIM/API Center.

## Things we were tempted to change in the API (and didn't)

- Add a customer-facing "briefing" endpoint. That is exactly what `OpsDesk.Mcp.Tasks/get_system_briefing` does – outside the API.
- Return friendlier errors. The task-shaped server translates them instead.
- Add an API key. The gateway owns authentication; the API stays as it was.

## Package versions

Pinned in the `.csproj` files to versions that restore together (.NET 10, `ModelContextProtocol` 1.2.0, Agent Framework 1.0 preview).
New in this branch: `Microsoft.ML.Tokenizers` (+ `Data.O200kBase`), OpenTelemetry + Azure Monitor exporter/distro. If `dotnet restore`
rejects a version, bump it in the referencing project; the API surface used is documented inline.

---
*Community demo material. Review product names and preview-feature availability (APIM MCP servers, API Center MCP type, Entra Agent ID) before reuse.*
