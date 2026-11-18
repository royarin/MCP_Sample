# Azure API Management – Demo 2 (no-code server) and Demo 4 (pre-provisioned controls)

One APIM instance (`apim-opsdesk-mcp`), two MCP servers:

| MCP server | Demo | Protection | Backend |
|---|---|---|---|
| `opsdesk-apim` | 2 | subscription key, product *demo-open* | OpsDesk API (generated from OpenAPI) |
| `opsdesk-secure` | 4 | Entra JWT only + governance policy | OpsDesk API *or* `OpsDesk.Mcp.Tasks` (nicer messages) – decide once |

## Demo 2 – expose the existing API as an MCP server (no code)

1. Export the OpenAPI document from the deployed API: `GET https://<ca-opsdesk-api>/openapi/v1.json` → `apim/opsdesk-api.openapi.json`.
2. APIM → **APIs → + Add API → OpenAPI** → upload → display name `OpsDesk API`, suffix `opsdesk`, product *demo-open*.
3. Named value `opsdesk-api-url` = Container App URL; apply `policy-backend.xml` at API scope.
4. APIM → **APIs → MCP Servers → + Create MCP server → Expose an API as an MCP server** → `OpsDesk API` → select all operations → name `opsdesk-apim`, base path `opsdesk-apim`.
5. Endpoint `https://<apim>.azure-api.net/opsdesk-apim/mcp` → `vscode/mcp.json` (`opsdesk-apim`) and `OpsBot.Mcp` (`--Mcp:Endpoint … --Mcp:ApimSubscriptionKey …`).
6. Verify in MCP Inspector with header `Ocp-Apim-Subscription-Key`. Observe: tool names = operation ids, descriptions = OpenAPI summaries.
7. Keep a hidden `opsdesk-apim-backup` so step 4 never has to succeed live.

> Portal labels move – re-check this click path the week before, and record a 60-second clip as fallback.

## Demo 4 – opsdesk-secure, configured beforehand

Applied by `infra/postdeploy.ps1`; nothing is created live. Walk the config, then show it in action.

| Control | Config to show | Action to show |
|---|---|---|
| Security | policy editor: `validate-jwt`, audience `api://opsdesk-mcp`, roles | anonymous `tools/list` → 401; Copilot/OpsBot.Mcp with Reader token answers |
| Governance | same policy: `rate-limit-by-key`, write-tool/role check | Reader raises incident → policy message; Operator → incident on dashboard |
| Discoverability | API Center entry `opsdesk-secure` (type MCP, owner, version, endpoint, metadata) | copy endpoint from API Center into `mcp.json` |
| Observability | API → Diagnostics: App Insights, 100 % sampling, W3C correlation | end-to-end transaction APIM → MCP server → API |

Order inside `<inbound>` when merged: `validate-jwt` first (sets `jwt`), then rate limit, then the tool check.
Subscription keys off on `opsdesk-secure` so there is one identity model.
