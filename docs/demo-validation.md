# Demo validation

Date: 2026-11-19

The implementation was built and exercised locally, then deployed and rehearsed against the Azure endpoints and the real Foundry model. This records runtime evidence, not a claim that the desktop presentation has been visually signed off.

## Results

| Scenario | Result | Evidence |
|---|---|---|
| Demo 1: no tools | Pass | No invented inventory facts; zero tool calls, one model turn, 798 input and 137 output tokens. |
| Demo 1: hand-wired tools | Pass | Grounded inventory status, owner, runbook and incidents; four calls, two turns, 2354 input and 286 output tokens. |
| Demo 2: code MCP discovery and briefing | Pass | Ten tools; real model retrieved inventory Red, Supply Chain Systems and Inventory DB Failover Runbook. |
| Demo 2: write and notify | Pass | Two separate approvals; exactly one Critical inventory incident and composed owner notification. Dashboard returned HTTP 200 and its backing API contained the incident; database reset afterward. |
| Demo 2: generated MCP | Pass | Eleven tools, including creation and notification, excluding database reset; real model briefing passed. |
| Demo 2: literal VS Code chat and approval UI | Manual check outstanding | Available IDE tools cannot drive Copilot Chat. Protocol/model counterparts passed. The existing user deletion of `.vscode\mcp.json` was deliberately preserved. |
| Demo 3: labeled hosts, scorecards and session stats | Pass | Both real interactive subprocesses answered the briefing; four versus one tool calls, two turns each, nonzero usage; stats/reset matched expected totals. Startup lines were at most 100 columns. |
| Demo 3: pane presentation | Partially verified | Windows Terminal and separate-console launch arguments passed isolated command-mock checks; physical panes/colors still need visual confirmation. |
| Demo 3: error shaping | Pass | Endpoint-shaped typo response exactly matched the API's raw HTTP 404 JSON. Task-shaped response suggested inventory and required confirmation before retrying. |
| Demo 3: repeated typo/write flow | Pass, three consecutive runs | Each run approved the rejected inventry write, confirmed inventory, approved the corrected write separately, and created exactly one incident. No silent target correction. Database reset afterward. |
| Demo 3: measurement script | Pass | Wrote actual model usage to `demo3-measurements.md`; README contains the final values. Task-shaped latency was higher in this sample despite fewer calls. |
| Demo 4: subscription key missing | Pass | HTTP 401; interactive host remained available for reconnect/exit. |
| Demo 4: token missing | Pass | HTTP 401 with gateway message. |
| Demo 4: token without Ops.Agent | Pass | HTTP 403, authenticated but not authorized. |
| Demo 4: authorized briefing and curation | Pass | Identity/roles/expiry displayed; exactly five GET tools; grounded briefing. Write request refused and incident state unchanged. |
| Demo 4: tool-only rate budget | Pass | Calls one through six succeeded; call seven returned HTTP 200 with matching JSON-RPC ID, result.isError and retry guidance. Ten tools/list requests did not consume the budget; discovery still worked after exhaustion. |
| Demo 4: model fan-out | Pass, three consecutive runs | Real all-systems questions exhausted the dedicated budget, displayed the shaped error and produced partial answers explaining the limit without retrying. |
| Demo 4: open twin and isolation | Pass | Open twin exposed eleven tools without a token. While the secure budget was exhausted, code/task/open-generated read calls all succeeded using the same subscription key and no JWT. |
| Demo 4: API Center | Pass | MCP type, version 1.0, demo environment/deployment and four metadata fields verified live. Discovery worked with the configured endpoint empty. |
| Demo 4: distributed tracing | Pass | One printed question ID selected agent.question, model/MCP dependencies, four APIM requests and the corresponding four API requests across the existing two Insights resources. |
| Demo 4: per-tool chart | Pass | Live KQL returned real tool counts and p95 durations. Tool metadata joins on request span ID and operation ID to avoid multiplying concurrent calls. |

## Isolation and provisioning

- Demo 4 uses `/opsdesk-apim-secure/mcp` and API-scoped JWT, role, rate-budget and diagnostic policies. It reuses `demo-open`, its existing key, and the existing Application Insights resources, as requested. No new monitoring resource is needed.
- Entra provisioning was rerun successfully without changing existing client IDs or local secrets. Secrets have a 30-day lifetime when created; use explicit rotation before expiry.
- `configure-apim.ps1 -EnterpriseOnly` was exercised to avoid rewriting earlier demo routes or settings. Replacing an API-generated MCP API was observed to clear its tools; provisioning now preserves existing generated APIs and reconciles tools non-destructively.
- An interrupted full provisioning run temporarily reduced the earlier generated catalog. All eleven tools were restored, followed by real briefing/write/notify regression tests. No Demo 1/2 application-source changes remain.
- API, Web, NoTools, Tools and endpoint-shaped MCP source trees match the pre-task baseline. Shared MCP-specific instructions and the 1200-token output reservation are isolated from the earlier console hosts.
- The previous draft's separate secure subscription was suspended; it is not used by the final shared-key configuration.

## Checks and limitations

The complete nine-project solution built with zero warnings/errors using isolated artifacts because existing local server processes lock their normal outputs. All 16 PowerShell scripts parsed, policy XML and Enterprise JSON parsed, and git diff whitespace checks passed.

Local isolated services passed readiness, ten/five-tool discovery, inventory briefing/NextAction, typo and no-mutation checks before cloud validation. API readiness uses `/SystemStatus/inventory`; unlike the two MCP servers, the frozen API has no `/health` endpoint.

Live helper checks covered rate-limit display, six successful cheap reads followed by the seventh shaped error, and unaffected legacy endpoints. The trace query uses `targetTraceId`: naming the variable `id` incorrectly compares against telemetry's span-ID column and returns no rows.

API-generated MCP telemetry also emitted zero-duration `forward-request` dependencies marked unsuccessful with no result code, alongside successful HTTP 200 APIM/API requests. Those rows remain visible in the trace query; they are not treated as proof that the successful tool calls failed.

Desktop-only VS Code trust/approval interaction and physical terminal layout remain unverified. No credentials, account identifiers or deployment-specific hostnames are included in this report.

## Resource-aligned display names

The APIM display names now exactly match their resource IDs: `opsdesk-api`, `opsdesk-apim`, `opsdesk-apim-secure`, `opsdesk-code` and `opsdesk-tasks`. API Center's synchronized titles caught up to those names; the separately registered secure demo entry uses the same title.

Renames used display-name-only PATCH requests, not generated-server replacement. Before/after comparisons confirmed that all non-name APIM properties, both generated tool collections and the secure policy were unchanged. Provisioning now reconciles generated-server names through PATCH and leaves matching names alone; local mocked tests covered rename, no-op and initial-creation paths.

All available Demo 1-4 runtime scenarios were repeated against the renamed resources with the existing built console hosts: grounded briefings, incident/notification approvals, three typo rehearsals, 401/401/403, curated write refusal without mutation, discovery and three real-model rate-limit rehearsals. Earlier endpoints still served reads without JWT after the secure budget was exhausted. A new printed question trace again joined the agent, four APIM requests and four API requests. Desktop-only checks remain manual.

## Caller-supplied resource uniqueness

Bicep and the PowerShell deployment, configuration and demo helpers now share the caller-selected `BaseName`/`UniqueSuffix` naming rule. A complete legacy `BaseName` with no suffix still produces the same names. Existing Azure resources were not renamed or reprovisioned.

Bicep compilation and non-deploying ARM Template-level validation passed with a separate caller suffix. Parameter-file compilation passed both with an explicit suffix and with the suffix environment variable absent. All 17 PowerShell scripts parsed. Focused checks covered invalid names, newline rejection, the 45-character boundary, all eight generated URLs, APIM configuration targets, mocked app deployment and mocked API Center registration. Splitting the existing deployment's full stem into base/suffix reproduced its live endpoint.

All available Demo 1-4 runtime scenarios passed again using the existing built hosts: both Demo 1 agents, all three MCP catalogs, grounded briefings, two write/notification approvals, three typo/correction rehearsals, the 401/401/403 cases, read-only refusal without mutation, API Center discovery, and three real-model rate-limit rehearsals. Code, task and open-generated reads still worked without JWT while the dedicated secure budget was exhausted. Live catalog metadata matched the generated endpoint; the printed question trace contained one agent request, four gateway requests and four API requests, and the per-tool counts/p95 query returned data. Incident state remained clean afterward.

This naming-only change did not rebuild application binaries or create a new Azure environment. The original technical manual remains unchanged; the previously noted desktop-only checks remain manual.

## API Center browser CORS

Wildcard CORS is deployed as an APIM global policy, not an application change. It permits any origin with cookie credentials disabled; subscription-key, JWT, role and rate-budget checks remain enabled. Provisioning preserves existing non-CORS policy content and all five API registrations. Focused tests covered BOM decoding, ETags, idempotency despite XML formatting, replacement, missing optional sections, initial creation and explicit read failures. All 18 PowerShell scripts parsed.

From both the Azure portal origin and an arbitrary test origin, all four MCP routes passed unauthenticated POST/GET/DELETE preflights, explicitly allowing authorization and custom request headers. Initialization and tool catalogs returned wildcard response headers and unchanged tool counts. Code/task endpoints also passed the initialized notification. Browser-readable missing-key, missing-token and no-role responses remained 401/401/403. The custom rate-limit response now sets CORS explicitly: browser-style requests saw HTTP 200, the matching JSON-RPC ID and `isError`; earlier routes still served JWT-free reads while the secure budget was exhausted.

**Remaining browser limitation:** generated MCP routes return HTTP 202 for `notifications/initialized` without a CORS header. Neither API-scoped CORS nor explicit global outbound headers fixed that response. Consequently the API Center console is not claimed fully unblocked for generated servers. No protocol short-circuit or authentication bypass was added. The temporary API-scoped experiment was reset to base-only inheritance; Azure required MFA to delete the policy resource, so that default-inheritance resource remains.

Existing console/protocol regressions were rerun with built hosts: Demo 1 agents, Demo 2 catalogs/briefings/write/notify, Demo 3 scorecards and three typo rehearsals, and Demo 4 authorization, curation, discovery and rate limits. The post-change printed trace joined one agent request, four gateway requests and four API requests; the per-tool p95 query returned data and incident state remained clean. The original technical manual and application sources/binaries were not changed.

## Fresh-build factory repair

The earlier rehearsals used existing compiled hosts, not a fresh build of the then-current working tree. A fresh Enterprise build reproduced CS1739 because the local factory edit removed `maxOutputTokens` while callers still supplied it. Restoring that contract exposed two further runtime regressions: ignoring `Foundry:Credential` produced `Workspace not found`, and omitting the deployment argument to `AsIChatClient` produced `Missing required parameter: 'model'`. Restoring all three behaviors returns `AgentConfig.cs` to its already-committed implementation; no additional factory source diff remains.

The repaired nine-project solution built into isolated artifacts with zero warnings/errors. The ordinary `OpsBot.Enterprise` project build also passed with zero warnings/errors. An ordinary full-solution build was additionally attempted: console outputs refreshed, but pre-existing local API and both MCP server processes locked their own output dependencies (MSB3021/MSB3027). Those processes were not stopped; this is separate from the repaired compiler error.

All available Demo 1-4 runtime scenarios then passed using the newly built isolated hosts, not the previous binaries. The harness explicitly selected the new artifact directory, and all four console output folders contained the exact rebuilt Shared assembly. Checks included real grounded briefings/usage, write and notification approvals, three typo rehearsals, 401/401/403, curated no-mutation refusal, catalog discovery and three real-model rate-limit rehearsals. A question from the fresh Enterprise host correlated one agent request, four gateway requests and four API requests; the per-tool telemetry query returned data.

The existing desktop-only checks and generated-server browser-notification CORS limitation remain unchanged. No server application source or Azure deployment was changed by this factory repair.

## Presentation settings completeness

All four console hosts now have credential-free `appsettings.json` templates using the actual Foundry configuration contract. Demo 3 includes its label, accent and approval settings; Enterprise includes both endpoint placeholders and all six presentation switches. Existing local development files were populated from those templates, with every previous value verified unchanged and all four files confirmed Git-ignored. Setup now layers template defaults, existing local values, then explicit provisioning updates, so future setup runs also include missing keys.

Checks verified every key in the presentation's JSON examples exists in both the template and development settings. All eight application templates parsed, private-value placeholders were checked, and build-output settings matched their source files exactly. The isolated nine-project solution and all four ordinary console project builds passed with zero warnings/errors. Focused helper tests covered initial creation, nested value preservation, explicit update precedence, additional keys, idempotency, absent templates and the development-file-only write guard.

All four freshly built console hosts launched with no application arguments and returned real model usage for their briefing questions. Enterprise was additionally exercised by changing only its copied development JSON: missing key returned 401, missing token returned 401, no-role identity returned 403, open mode discovered eleven tools, API Center discovery resolved the endpoint with `Mcp:Endpoint` empty, and normal mode discovered five tools and answered the grounded briefing with a trace ID. Copied settings were restored afterward. No incident writes, database resets, server deployments or policy changes were needed for this settings-only rehearsal.
