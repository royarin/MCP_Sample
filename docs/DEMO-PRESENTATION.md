# Builders Foundry London - presenter walkthrough

Use this file on stage. Keep `DEMO-MANUAL.md` for preparation, system checks and regression.

**Presentation rhythm: show the setting or code, explain the change, perform one action, point at the result.**

There are no launcher scripts or command-line application arguments in this walkthrough. Start console projects from the IDE and type questions interactively. All APIs, web pages and MCP servers are already running in Azure.

## Before the audience arrives

1. Complete the technical checks in the existing manual. Reset the demo database once so inventory starts Red with no incidents. Do not deploy, provision identities or reset data during the talk.
2. Open the solution in VS Code with C# Dev Kit. In Settings, set **Csharp > Debug: Console** (`csharp.debug.console`) to `integratedTerminal`. Confirm that console input and write approvals work there.
3. In Solution Explorer, right-click each console project and select **Debug > Start New Instance** once to rehearse its launch. Close it afterward. The projects are `OpsBot.NoTools`, `OpsBot.Tools`, `OpsBot.Mcp` and `OpsBot.Enterprise`. Never start the API, Web or either MCP server locally.
4. Ensure IDE launches have no application arguments or configuration overrides left from rehearsal. Use the same Foundry deployment throughout.
5. Keep credentials in user-secrets or environment variables, not in the settings you will project. Prepare this offstage: remove credential values from the displayed development files after moving them to the private provider. Do not display keys, client secrets, tokens or connection strings.
6. Confirm user-secrets/environment variables do not override the endpoint and public switches you intend to edit. Configuration precedence is JSON, development JSON, user-secrets, environment, then application arguments.
7. Open browser tabs for the dashboard, API OpenAPI document, APIM MCP servers, the secure server's policy and named values, API Center, and Application Insights Logs. Pre-authenticate; keep subscription keys and identity secrets offscreen.
8. Configure the two Demo 2 MCP connections in VS Code before the talk. Use the existing editor/user configuration or the `vscode\mcp.json` template; do not assume a workspace `.vscode\mcp.json` exists. Start only the connection being demonstrated.
9. Prepare the trace query from `scripts\demo4-trace.kql` in an Application Insights Logs tab, with the two existing resource names already substituted. Leave only the question trace ID to paste live. This is a query to show, not a script to launch.
10. Increase editor, terminal and browser zoom. Keep this guide on the presenter display.

### What "start" and "change settings" mean below

**Start a console:** in Solution Explorer, right-click that project, then **Debug > Start New Instance**. Wait for its banner and type in the integrated terminal.

**Change settings:** type `exit`, edit the project's `appsettings.Development.json`, save, then build and start that project through the IDE. All public switches shown below are included in the console projects' `appsettings.json` templates and seeded into development settings by setup. Edit their values; do not replace the file's Foundry, identity or telemetry configuration. For manual setup, copy the template to the ignored development file and fill in your own deployment values first.

The programs read settings beside their executable, and the build copies the project settings there. Editing the project file alone does **not** change an already-running console. Keep this stop/edit/build/start pattern for the main presentation.

Enterprise's `reconnect` reloads the executable-directory copy, not the source-project file. Do not rely on it to pick up source edits without a build.

## Opening - establish what already exists

1. **Show:** the dashboard, then the API's OpenAPI document.
2. **Say:** "This is OpsDesk: an ordinary operational application. It has systems, owners, runbooks and incidents. It is already deployed. We are not rewriting it."
3. **Point at:** inventory, Red status, replication lag, and the absence of an incident at this starting point.
4. **Say:** "The question is not whether a model can talk. It is whether it can safely reach the systems we already own."

## Demo 1 - first no hands, then hand-wired hands

### 1A. Ask the agent that has no tools

1. **Show:** `src\OpsBot.NoTools\Program.cs`. Point out that the agent receives instructions but no operational tools.
2. **Start:** `OpsBot.NoTools`.
3. **Type:**

   ```text
   What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
   ```

4. **Expect:** it cannot retrieve the current inventory state. Point at zero tool calls; it must not invent the owner or status.
5. **Say:** "A model without access to my systems is still just a chatbot. More prompting does not give it access."
6. Type `exit`.

### 1B. Give the same question to the hand-wired host

1. **Show:** `src\OpsBot.Tools\OpsDeskFunctions.cs`.
2. **Say:** "I can give it hands myself: functions that call the existing API. These functions are written into this host."
3. **Show:** the non-secret `OpsDesk:BaseUrl` setting for this project. It points at the deployed API route, not a local service.
4. **Start:** `OpsBot.Tools`. Type the same briefing question.
5. **Point at:** tool calls retrieving status, owner, runbook and incidents, then the grounded answer:
   inventory Red, Supply Chain Systems, `#inventory-oncall`, Inventory DB Failover Runbook.
6. **Point at:** the usage scorecard. The host actually retrieved facts; this is not a staged response.
7. **Say:** "This works, but the integration lives in this host. What happens when I want another host to use the same application?"
8. Type `exit`.

**Transition:** "Instead of wiring each host to my API, I can publish capabilities once and let hosts discover them."

## Demo 2 - the same API exposed through MCP, two ways

No console project is needed for this demo. Use Copilot Chat and the deployed servers.

### 2A. First show how easily APIM exposes an API as MCP

1. **Show:** `opsdesk-api`, the existing OpsDesk REST API in APIM, and its operations. Display names match resource IDs throughout this walkthrough.
2. **Say:** "The API is already managed here. I can expose its operations as MCP tools without writing or deploying an MCP wrapper."
3. In the APIM portal, select **APIs > MCP Servers > + Create MCP server**.
4. Choose **Expose an API as an MCP server**.
5. Under **Backend MCP server**, select `opsdesk-api`. The picker may also list the existing MCP entries; do not choose one of those as the REST source.
6. Select the operations to expose. Show that this is a deliberate tool selection: include the operational reads and writes, but leave `Database/reset` unselected.
7. Under **New MCP server**, show the display-name and name fields. If a name is needed to illustrate the form, use an unused preview name such as `opsdesk-apim-preview`, not an existing server's name.
8. Under **Products**, select the existing `demo-open` product. Explain that its subscription requirement still applies; do not display a key.
9. **Point at:** **Create**, but do not submit. **Say:** "Choose the API, choose the operations, give the server a name and publish it. That is the conversion. I have already created our demo server so we can use it immediately."
10. Cancel the form and open the existing `opsdesk-apim` server. Do not recreate or edit the working server during this walkthrough.
11. **Point at:** its eleven tools and server URL ending in `/opsdesk-apim/mcp`.

### 2B. Use the already-created APIM MCP server

1. **Show:** the `opsdesk-apim` connection in VS Code. Its URL matches the server just shown; its subscription header is configured privately.
2. In Copilot Chat agent mode, disable other MCP servers' tools and enable only `opsdesk-apim`. Start a fresh chat.
3. **Say:** "Now I will use that gateway-generated server from Copilot, a host I did not write."
4. **Type:**

   ```text
   What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
   ```

5. **Point at:** discovered tools, actual calls to the existing API, and the grounded answer.
6. **Say:** "No MCP wrapper code, and no OpsDesk integration written into this host. APIM exposed the tools; Copilot discovered and used them."

### 2C. Make one visible change through the APIM-generated server

1. Keep `opsdesk-apim` enabled and the dashboard visible in another tab.
2. **Type in Copilot:**

   ```text
   Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner.
   ```

3. Review the proposed write and approve it. Approve a separate notification call if the host requests it. If the model asks for confirmation first, confirm the requested action.
4. Refresh the dashboard and point at the new Critical inventory incident. Point at the returned owner-notification content in chat.
5. **Say:** "The action reached the existing application. The notification here is composed and returned by the demo API; I am not claiming a real email or Slack message was delivered."

### 2D. Then show the alternative: our own MCP code

1. **Say:** "Gateway-generated tools are one route. If I want to own the MCP implementation, I can write a server over the same API."
2. **Show:** `src\OpsDesk.Mcp\Program.cs`, then one class in its `Tools` folder.
3. **Point at:** server registration, tool attributes, descriptions and calls to the existing API. Do not start the project; this server is already deployed.
4. **Show:** APIM's `opsdesk-code` entry. Explain that this route fronts an existing MCP server, unlike `opsdesk-apim`, which generates tools from API operations.
5. In Copilot, disable `opsdesk-apim`, enable only `opsdesk-code`, and start a fresh chat.
6. Ask the same briefing question.
7. **Point at:** ten discovered tools and the grounded response, including the incident just created. No second write is needed.
8. **Say:** "First we generated MCP at the gateway; now we used an MCP server we wrote. Same application, same host, same discovery contract."

**Counts to keep straight:** code-based endpoint server = **10** tools; task-shaped server = **5**; open APIM-generated server = **11**. These are three different catalogs.

**State handoff:** do not reset on stage. The incident created here should remain visible in later briefings.

**Transition:** "Both approaches expose tools. Next, let's see how the shape of those tools changes the model's work."

## Demo 3 - change the tool surface, not the host

The main path switches one host's settings visibly, without launcher arguments. Keep the first scorecard visible or note its figures before switching. Side-by-side terminal choreography is not required for this walkthrough.

### 3A. Connect the generic host to endpoint-shaped tools

1. **Show:** `src\OpsBot.Mcp\appsettings.Development.json`, with credentials kept private.
2. Merge these public settings, substituting the actual APIM host name:

   ```json
   {
     "Mcp": {
       "Endpoint": "https://<apim-name>.azure-api.net/opsdesk-code/mcp"
     },
     "label": "endpoint-shaped",
     "accent": "yellow",
     "permissions": "ask"
   }
   ```

3. **Say:** "The endpoint chooses the tool catalog. The label and color are just presentation. Approval remains enabled."
4. Build and **start:** `OpsBot.Mcp`.
5. **Point at:** ten discovered tools and the catalog-token line.
6. **Say:** "Tool definitions cost tokens before the user has asked anything."
7. **Type:**

   ```text
   What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
   ```

8. **Point at:** the individual status, owner, runbook and incident calls. Expect about four calls; read the actual scorecard rather than promising a fixed model response.
9. Type `stats`. Keep the result visible or note the numbers.

### 3B. Show the endpoint-shaped typo

1. In the same host, **type:**

   ```text
   Raise a high incident for inventry: replication lag, reported by Arin, do not notify yet.
   ```

2. At the attempted-write approval, type `y`.
3. **Point at:** the raw API not-found JSON. Do not confirm a corrected write on this side.
4. **Say:** "The API error is accurate, but it does not help the agent recover the task."
5. Type `stats`, then `exit`.

### 3C. Change only the catalog and its presentation label

1. Return to the same development settings file.
2. Change these properties; keep the same model, host, subscription key and approval mode:

   ```json
   {
     "Mcp": {
       "Endpoint": "https://<apim-name>.azure-api.net/opsdesk-tasks/mcp"
     },
     "label": "task-shaped",
     "accent": "cyan"
   }
   ```

3. **Say:** "I have changed where the host discovers tools. I have not rewritten the host."
4. Save, build and **start:** `OpsBot.Mcp`.
5. **Point at:** five discovered tools, including `get_system_briefing`.
6. Type the same briefing question.
7. **Point at:** one briefing tool call, then its scorecard. The response includes the current incident state, not necessarily an empty incident list.
8. **Say:** "This tool represents the engineer's task, rather than one HTTP endpoint. Compare calls, turns and actual tokens. Latency varies; fewer tools or calls do not guarantee a faster answer."

For orientation only, the recorded rehearsal used four versus one calls, two model turns each, and 590 versus 520 catalog tokens. Use the live numbers; `demo3-measurements.md` contains the dated sample.

### 3D. Show an error the agent can act on, with a second approval

1. **Type:**

   ```text
   Raise a high incident for inventry: replication lag, reported by Arin, do not notify yet.
   ```

2. At the attempted-write approval, type `y`.
3. **Point at:** `Unknown system 'inventry'` and `Did you mean 'inventory'?`
4. Wait for the agent to ask for correction confirmation.
5. **Type:**

   ```text
   Yes, inventory. Raise it for inventory without notifying the owner.
   ```

6. At the corrected-write approval, type `y` again.
7. Refresh the dashboard. Point at the new High incident; do not assume its sequence number. The Critical incident from Demo 2 should still exist.
8. **Say:** "A useful error gives a recovery path. It does not silently change the target of a write. Confirming the corrected ID and approving the new write are separate steps."
9. Type `stats` to show the cumulative totals. Do not compare these write-inclusive totals with the first briefing-only scorecard.
10. **Show:** `src\OpsBot.Mcp\McpConnection.cs`, specifically `ListToolsAsync()`, then the tools passed into the agent in `Program.cs`.
11. **Say:** "Discovery replaced the hand-wired functions from Demo 1. Tool design changed the model's work."
12. Type `exit`.

## Demo 4 - show the controls, then show them enforced

**Opening line:** "Now the question is not just whether the agent can act. It is which identity, which tools, what budget, and whether I can trace the result."

### 4A. Establish the dedicated secure endpoint

1. **Show:** APIM's `opsdesk-apim-secure` server.
2. **Point at:** its five GET tools, then its policy: token validation, the `Ops.Agent` role and a tool-call budget.
3. **Say:** "This endpoint is dedicated to governance. It reuses our existing product, subscription key and Application Insights. The policies and rate counter are scoped here, so this demo does not change the earlier endpoints."
4. Open `src\OpsBot.Enterprise\appsettings.Development.json`, with credentials stored privately.
5. Prepare these public settings. Keep the existing identity, API Center and telemetry settings:

   ```json
   {
     "Mcp": {
       "Endpoint": "https://<apim-name>.azure-api.net/opsdesk-apim-secure/mcp",
       "OpenEndpoint": "https://<apim-name>.azure-api.net/opsdesk-apim/mcp"
     },
     "no-key": false,
     "no-token": false,
     "identity": "default",
     "open": false,
     "discover": ""
   }
   ```

The top-level switches are real configuration keys read by this host. They let you demonstrate header and identity choices without deleting or displaying a credential.

### 4B. Remove the subscription header

1. Set `"no-key": true`. Leave `"no-token": false`.
2. **Say:** "I am telling this client not to send its subscription key. I am not weakening the gateway policy."
3. Save, build and **start:** `OpsBot.Enterprise`.
4. **Point at:** `subscription key: missing`, then HTTP 401. The host stays open; it has not reached a model question.
5. Type `exit`.

### 4C. Restore the key, omit the Entra token

1. Set `"no-key": false` and `"no-token": true`.
2. **Say:** "The subscription identifies access to the product. It does not replace the identity token."
3. Save, build and **start:** `OpsBot.Enterprise`.
4. **Point at:** key present, no token, then HTTP 401 requiring a valid Entra token.
5. Type `exit`.

### 4D. Restore the identity and answer the briefing

1. Set `"no-token": false`. Keep `"identity": "default"`.
2. Save, build and **start:** `OpsBot.Enterprise`.
3. **Point at:** key present, identity, `Ops.Agent`, token expiry, then five discovered read tools.
4. **Say:** "The MCP connection now has an application identity and role. This is separate from the presenter's Azure CLI identity used to call the model."
5. **Type:**

   ```text
   What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?
   ```

6. Point at the grounded answer and the current incidents.
7. Save the printed `trace:` ID for the observability step. Use this successful briefing's ID, not a later refusal that may have no API calls.

**Optional authorization extra:** stop the host, change `"identity": "norole"`, build/start and show HTTP 403. Say "Authenticated is not the same as authorized." Restore `"identity": "default"` and build/start before continuing.

### 4E. Show curation, not an approval prompt

1. **Show:** the secure catalog in APIM and in the console. There is no incident-create or notification tool.
2. **Type:**

   ```text
   Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner.
   ```

3. **Expect:** a read-only/no-write-tool explanation. Refresh the dashboard; no additional incident should appear.
4. **Say:** "This is not the host asking me to approve a write. There is no write tool in this catalog. The gateway decides what is exposed."

### 4F. Show the gateway stopping a fan-out

1. **Show:** APIM named values `opsdesk-ratelimit-calls = 6` and `opsdesk-ratelimit-period = 60`.
2. **Point at:** the policy's `tools/call` condition and agent-keyed counter.
3. **Say:** "Discovery does not consume this budget. Actual tool execution does. Six per minute is deliberately low so you can see the boundary."
4. Let the previous tool-call window expire before this question: allow at least 60 seconds from the last read, preferably 65. Use the policy explanation to cover the pause.
5. **Type:**

   ```text
   Give me the status, owner, runbook and open incidents for every system.
   ```

6. **Point at:** successful reads, the red `Rate limit exceeded` tool result, and a partial answer explaining the limit.
7. **Say:** "The gateway enforced the budget. It returned an error the model could understand, rather than asking the model to police itself."
8. Explain, if useful, that this limit is returned as an HTTP 200 MCP result with `isError: true`; do not call it an HTTP 429 on the wire.

**If the model does not fan out enough:** do not claim the limit fired. Ask for the missing per-system owner/runbook details within the same window and point at the actual calls. If it still does not trigger, show the policy and the previously validated result; do not spend stage time running a burst script or changing the policy.

### 4G. Show discovery through the catalog

1. **Show:** API Center's `opsdesk-apim-secure` entry.
2. **Point at:** MCP type, version 1.0, demo environment, APIM runtime endpoint, owner Platform Ops, internal classification, read-only tools and the rate budget.
3. **Say:** "This is how teams find the capability and its ownership. The catalog does not bypass gateway enforcement."

**Optional live resolution:**

1. Type `exit`.
2. Set `"discover": "opsdesk-apim-secure"` and clear `Mcp:Endpoint` to `""`. Leave `open` false and both omission switches false.
3. Save, build and start `OpsBot.Enterprise`.
4. Point at `resolved from API Center`, the secure runtime URL and five tools.
5. Say "The endpoint came from the catalog, not the endpoint field I just cleared."
6. After showing it, stop and restore `"discover": ""` and the secure endpoint for the next run.

### 4H. Follow one real transaction

1. Switch to the prepared Application Insights Logs tab.
2. Paste the successful briefing's trace ID into `targetTraceId` and run the query.
3. **Point at:** `agent.question`, MCP/client dependencies, APIM requests and the corresponding API requests sharing `operation_Id`.
4. **Say:** "One question, across the agent, gateway and existing API. We reused the monitoring resources and correlated the journey."
5. If ingestion is delayed, show API Center while it arrives; use a previously verified trace as an explicitly labeled fallback.
6. Do not interpret zero-duration APIM `forward-request` dependency rows alone as a failed tool call; check the corresponding APIM and API request results.

**Optional open-catalog comparison:** stop the host, set `"open": true`, `"no-token": true` and `"discover": ""`, then build/start. Show eleven tools including writes, but do not perform a write. Explain that this is the earlier open twin using the same key. Restore `open` false and `no-token` false afterward.

## Close

1. Return to the dashboard.
2. **Say:** "We kept the existing application. MCP made its capabilities discoverable. Tool design shaped the model's work. Our existing platform controls governed access and made the result observable."
3. Stop the console host and restore the public switches to their normal values. Keep all credentials private.

## Presenter recovery card

| What you see | Next action |
|---|---|
| An edit appears to do nothing | Stop, save, build/start the correct project. Check for higher-precedence environment/user-secret settings; do not improvise command-line overrides. |
| Wrong tool count | Check the endpoint: code 10, tasks 5, open-generated 11, secure-generated 5. Enable only one server in Copilot. |
| A later briefing reports incidents | Expected: Demo 2 and Demo 3 created them. Do not claim a clean incident list after those writes. |
| The rate limit fires on a briefing | Wait for the tool window to expire; the secure budget is shared by this agent identity across its sessions. Restarting the host does not reset it. |
| A permission prompt appears | Review the target and type `y` only for the intended write. Never choose "always" for this presentation. |
| Copilot or the model behaves differently | Describe what actually happened. Show the discovered tool/policy evidence; do not promise exact wording, turn counts or latency. |
| Network or trace ingestion stalls | Use the dated measurements, recorded validation evidence or prepared portal view and label it as a fallback. |

Reference material: `DEMO-MANUAL.md` for technical operation, `demo-validation.md` for measured acceptance evidence, and `demo3-measurements.md` for the dated scorecard.
