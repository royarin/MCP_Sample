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
