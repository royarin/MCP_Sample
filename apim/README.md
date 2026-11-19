# Azure API Management – API and MCP routes

All OpsDesk routes are published through `apim-<base-name>`. Every route requires a subscription key.

| MCP server | Demo | Protection | Backend |
|---|---|---|---|
| `opsdesk-code` | 2, 3 | product `demo-open` subscription key | Endpoint-shaped MCP server |
| `opsdesk-tasks` | 3 | product `demo-open` subscription key | Task-shaped MCP server |
| `opsdesk-apim` | 2 | product `demo-open` subscription key | MCP tools mapped to OpsDesk API operations |
| `opsdesk-apim-secure` | 4 | shared `demo-open` key + Entra `Ops.Agent` + six tool calls/minute/agent | Five curated GET operations from the same API |

## Configure API and MCP routes

Run `.\infra\configure-apim.ps1` after the API and MCP App Services are deployed. It imports the OpenAPI API, creates the `demo-open` product, publishes the API-backed and passthrough MCP servers, excludes `Database/reset` from generated tools, creates a local subscription, and writes its key only into ignored `appsettings.Development.json` files.

`opsdesk-code` and `opsdesk-tasks` are **existing MCP servers**, not API-generated tool catalogs. Each registration references an APIM backend through `backendId` and maps `mcpProperties.endpoints.mcp.uriTemplate` to `/mcp`; the backend URL is the corresponding App Service origin. Both registrations still use resource type `mcp`, but the backend reference distinguishes them from `opsdesk-apim`, whose tools reference REST API operations. After provisioning, check `tools/list` through APIM: expect 10 endpoint-shaped tools and 5 task-shaped tools, not an empty catalog.

The key is never printed. `OpsBot.Tools` sends it as `Ocp-Apim-Subscription-Key`; `OpsBot.Mcp` sends it for every APIM MCP call. `vscode/mcp.json` prompts for the same key.

## Demo 4 – curated enterprise twin

Run `infra\configure-entra.ps1` first (see `infra\entra.md`), then `configure-apim.ps1 -EnterpriseOnly` with its required deployment parameters. The dedicated secure route reuses the existing `demo-open` product/key but additionally requires a valid `Ops.Agent` token. Subscription validation stays enabled. The open twin and existing MCP backends are unchanged.

| Control | Config to show | Action to show |
|---|---|---|
| Security | `policy-mcp-secure.xml`: JWT audience/issuer, role check | No key → 401; no token → 401; no role → 403; normal Enterprise connection works |
| Curation | Five GET tools; no incident creation/notification or status mutation | Write prompt gets a read-only explanation; dashboard unchanged |
| Rate budget | Named values, `tools/call` only, counter keyed by token `azp` | Seventh call returns HTTP 200 JSON-RPC `isError`; model explains the limit |
| Discoverability | API Center entry: owner, version, environment, runtime, metadata | `OpsBot.Enterprise --discover opsdesk-apim-secure` |
| Observability | Secure API diagnostics: App Insights, 100 %, W3C, traceparent only | Question trace joins agent → APIM → API across two resources |

Inbound order is subscription validation, JWT validation, role check, then the tool-call budget. Initialization, notifications, and tool discovery do not consume the budget. The policy does not overwrite `traceparent`; diagnostics never log authorization headers, keys, or bodies.

Isolation is endpoint-scoped, not key-scoped: the counter includes `opsdesk-apim-secure` plus the token's agent identity, and only that API has the policy and diagnostics. Existing App Insights resources are reused. Exhausting Demo 4's budget must not affect `opsdesk-code`, `opsdesk-tasks`, or `opsdesk-apim`.

Generated MCP tool resource IDs are prefixed with `secure-` because this APIM preview requires IDs to be globally unique across servers; the tool display names still describe the same GET operations. Existing generated APIs are not replaced on reruns, since a PUT would erase their tool collection.

`postdeploy.ps1` registers the secure server in API Center. `scripts\demo4-ratelimit.ps1 -BaseName "<base-name>" -Show` displays the live budget; the optional burst helper verifies the error shape. Use `demo4-trace.kql` and `demo4-tools.kql` with your own App Insights names.
