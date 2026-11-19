# Demo manual – Builders Foundry London

Run preparation commands from your repository checkout. Everything server-side runs in Azure; only the console hosts run on the laptop.

Pattern for every console demo: **launch the program, wait for the banner and tool list, then type the prompt.** Type `exit` to quit.

> Use this manual for preparation and rehearsal, and `DEMO-PRESENTATION.md` on stage. Replace `<base-name>` with your deployment's complete resource-name stem and the URL placeholders with your deployed app and APIM names.

## 0 · Before the session

```powershell
cd "<repo-root>"
```

```powershell
dotnet build MCP_Sample.slnx
```

```powershell
.\scripts\reset.ps1 -BaseName "<base-name>"
```

Open in tabs: dashboard, Swagger, MCP Inspector, APIM (tool selection + policy of `opsdesk-apim-secure`), API Center entry, App Insights Logs.

Dashboard:

```text
https://<web-app-name>.azurewebsites.net
```

Swagger:

```text
https://<api-app-name>.azurewebsites.net/openapi/v1.json
```

MCP Inspector (header `Ocp-Apim-Subscription-Key`):

```powershell
npx @modelcontextprotocol/inspector
```

```text
https://<apim-name>.azure-api.net/opsdesk-code/mcp
```

```text
https://<apim-name>.azure-api.net/opsdesk-tasks/mcp
```

```text
https://<apim-name>.azure-api.net/opsdesk-apim/mcp
```

```text
https://<apim-name>.azure-api.net/opsdesk-apim-secure/mcp
```

VS Code: workspace `.vscode/mcp.json`; keep all servers **off** until needed. Copilot in **agent mode**.

## Prompts

Briefing:

```text
What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
```

Write action:

```text
Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner.
```

Fan-out (Demo 4, rate limit):

```text
Give me the status, owner, runbook and open incidents for every system.
```

---

## Demo 1 · An agent without your systems is just a chatbot

**Scenario:** same question, no tools → can't answer; hand-wired functions → answers; then show the cost in code.

Launch – no tools:

```powershell
dotnet run --no-build --project src/OpsBot.NoTools
```

Type:

```text
What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
```

Launch – hand-wired functions:

```powershell
dotnet run --no-build --project src/OpsBot.Tools
```

Type:

```text
What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
```

Then open `src/OpsBot.Tools/OpsDeskFunctions.cs` – "this file is what MCP replaces".

---

## Demo 2 · From existing API to MCP server – two ways

**Scenario:** the API becomes an MCP server in C# (`OpsDesk.Mcp`) and with no code (APIM `opsdesk-apim`); Copilot – a host you didn't write – uses both. No console.

1. Swagger + dashboard: "this is what exists, running in Azure, untouched".
2. `src/OpsDesk.Mcp/Program.cs` + `Tools/*.cs` in VS Code (not started). Inspector → `opsdesk-code` → `tools/list`.
3. Copilot, **only `opsdesk-code` enabled** – type:

```text
What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
```

   then type → approval dialog → refresh dashboard:

```text
Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner.
```

4. Switch Copilot to **`opsdesk-apim`** (APIM-generated, no code) – type:

```text
What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
```

5. Inspector side by side: `opsdesk-apim` vs `opsdesk-code` descriptions.

---

## Demo 3 · Tool design is the UX for the model

**Scenario:** run the identical briefing prompt twice with `OpsBot.Mcp`: first against MCP code (`opsdesk-code`), then against MCP tasks (`opsdesk-tasks`). The automatically printed scorecards are the observation.

Use the same model, credentials and data for both runs. Before this segment, leave `label` empty in `src\OpsBot.Mcp\appsettings.Development.json` so each run identifies its discovered server.

1. Set `Mcp:Endpoint` to `https://<apim-name>.azure-api.net/opsdesk-code/mcp`, using the actual APIM host name. Save and build the project, then start `OpsBot.Mcp` from the IDE.
2. Type:

   ```text
   What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
   ```

3. Keep the automatically printed scorecard visible or note its figures.
4. Type `exit`. Change only `Mcp:Endpoint` to `https://<apim-name>.azure-api.net/opsdesk-tasks/mcp`, save and build, then start a fresh `OpsBot.Mcp` session from the IDE.
5. Type the identical prompt:

   ```text
   What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
   ```

6. Compare the two printed scorecards: model turns, tool calls, input/output tokens and elapsed time. Describe the live numbers; fewer calls do not guarantee fewer turns or lower latency.

**Takeaway:** same host, same model, same question; the tool shape changes the model's work. There are no additional live actions in this demo.

---

## Demo 4 · Enterprise-grade MCP on Azure (pre-provisioned)

**Scenario:** `opsdesk-apim-secure` – a dedicated, secured and curated twin of the Demo 2 server, reusing the existing `demo-open` product, subscription key and Application Insights – used by a dedicated console agent (`OpsBot.Enterprise`). One scenario per concept – show the config, then show it working. Extras are marked *(extra)* and can be skipped.

### Security – subscription key + Entra ID

Show: APIM → `opsdesk-apim-secure` → policy (`validate-jwt`, role `Ops.Agent`).

No key → 401:

```powershell
dotnet run --no-build --project src/OpsBot.Enterprise -- --no-key
```

Key, no token → 401:

```powershell
dotnet run --no-build --project src/OpsBot.Enterprise -- --no-token
```

Key + token → identity and roles printed:

```powershell
dotnet run --no-build --project src/OpsBot.Enterprise
```

Type:

```text
What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
```

*(extra)* Authenticated, not authorised → 403:

```powershell
dotnet run --no-build --project src/OpsBot.Enterprise -- --identity norole
```

### Governance – curated tools + rate limit

Show: APIM → `opsdesk-apim-secure` → tools (reads selected, writes not). In the agent, point at the tool list – no `CreateIncident`. Type (answer: no tool for that; dashboard unchanged):

```text
Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner.
```

Rate limit – show the policy (tool calls only, `opsdesk-ratelimit-calls = 6` per minute). Type the fan-out question → tool calls, then `⛔ Rate limit exceeded`, partial answer:

```text
Give me the status, owner, runbook and open incidents for every system.
```

*(extra)* Same API, same generation, full surface (`opsdesk-apim`, Demo 2) – the write operations are there:

```powershell
dotnet run --no-build --project src/OpsBot.Enterprise -- --open --no-token
```

### Discoverability – API Center

Show: API Center → `opsdesk-apim-secure` (owner, version, environment, endpoint, `toolsExposed=read-only`, `rateLimit=6 tool calls/min per agent`).

*(extra)* Endpoint resolved from API Center:

```powershell
dotnet run --no-build --project src/OpsBot.Enterprise -- --discover opsdesk-apim-secure
```

### Observability – one trace across two App Insights

Copy the `trace:` id printed after the last answer, paste it into `scripts/demo4-trace.kql`, run it in App Insights Logs → agent span → APIM → API.

```text
scripts/demo4-trace.kql
```

*(extra)* Calls and p95 per tool, 429s included:

```text
scripts/demo4-tools.kql
```

---

## Rehearsal (not on stage)

Measure Demo 3 – writes an ignored local report to `artifacts\demo3-measurements.md` for the "What you just saw" slide:

```powershell
.\scripts\demo3-measure.ps1 -BaseName "<base-name>"
```

Reset between runs:

```powershell
.\scripts\reset.ps1 -BaseName "<base-name>"
```

VS Code: *MCP: Restart server* if a tool list looks stale. Keys and identities live in the git-ignored `appsettings.Development.json` of `OpsBot.Mcp` and `OpsBot.Enterprise`; fallback narration in `scripts/demo4-expected.md`. Run the security steps and the fan-out question at least a minute apart only if the briefing came close to 6 calls; tune with `.\scripts\demo4-ratelimit.ps1 -BaseName "<base-name>" -Show` / `-Calls 7`. Fallback if the model fans out too little on the day: `.\scripts\demo4-burst.ps1 -BaseName "<base-name>"`.
