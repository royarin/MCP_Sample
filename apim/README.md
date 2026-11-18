# Azure API Management – API and MCP routes

All OpsDesk routes are published through `apim-<base-name>`. Every route requires a subscription key.

| MCP server | Demo | Protection | Backend |
|---|---|---|---|
| `opsdesk-code` | 2, 3 | product `demo-open` subscription key | Endpoint-shaped MCP server |
| `opsdesk-tasks` | 3 | product `demo-open` subscription key | Task-shaped MCP server |
| `opsdesk-apim` | 2 | product `demo-open` subscription key | MCP tools mapped to OpsDesk API operations |
| `opsdesk-secure` | 4 | subscription key + Entra JWT + governance policy | Task-shaped MCP server |

## Configure API and MCP routes

Run `.\infra\configure-apim.ps1` after the API and MCP App Services are deployed. It imports the OpenAPI API, creates the `demo-open` product, publishes the API-backed and passthrough MCP servers, excludes `Database/reset` from generated tools, creates a local subscription, and writes its key only into ignored `appsettings.Development.json` files.

The key is never printed. `OpsBot.Tools` sends it as `Ocp-Apim-Subscription-Key`; `OpsBot.Mcp` sends it for every APIM MCP call. `vscode/mcp.json` prompts for the same key.

## Demo 4 – opsdesk-secure, configured beforehand

Create only after the Entra resource app, role assignments, and service identities are configured by `infra/entra.md`. The route must require both the `demo-open` subscription key and a valid role-bearing JWT; do not turn off subscription validation.

| Control | Config to show | Action to show |
|---|---|---|
| Security | policy editor: `validate-jwt`, audience `api://opsdesk-mcp`, roles | subscription-key-only `tools/list` → 401; Copilot/OpsBot.Mcp with Reader token answers |
| Governance | same policy: `rate-limit-by-key`, write-tool/role check | Reader raises incident → policy message; Operator → incident on dashboard |
| Discoverability | API Center entry `opsdesk-secure` (type MCP, owner, version, endpoint, metadata) | copy endpoint from API Center into `mcp.json` |
| Observability | API → Diagnostics: App Insights, 100 % sampling, W3C correlation | end-to-end transaction APIM → MCP server → API |

Order inside `<inbound>` when merged: `validate-jwt` first (sets `jwt`), then rate limit, then the tool check.
Subscription keys off on `opsdesk-secure` so there is one identity model.
