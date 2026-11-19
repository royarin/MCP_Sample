# Entra ID setup for Demo 4

Sign Azure CLI into the demo tenant, then run:

```powershell
az login --tenant "<tenant-id>"
.\infra\configure-entra.ps1 -TenantId "<tenant-id>"
```

The idempotent script creates or reuses these dedicated demo registrations:

| App | Purpose |
|---|---|
| `app-opsdesk-mcp` | Resource `api://opsdesk-mcp`, v2 access tokens, application role `Ops.Agent` |
| `opsdesk-agent` | Client-credentials identity granted `Ops.Agent` |
| `opsdesk-agent-norole` | Authenticated client without the resource role; demonstrates HTTP 403 |

The caller needs permission to create application registrations/service principals and grant an application-role assignment. An administrator may need to run the setup. The no-role identity is intentionally allowed to obtain a token; the gateway, not token issuance, rejects its missing role.

Client secrets are appended with a 30-day lifetime and written only into ignored `src\OpsBot.Enterprise\appsettings.Development.json`. Existing local secrets are preserved; `-RotateSecrets` appends new credentials explicitly. Remove expired demo credentials in Entra after use. Never paste secrets into a command, terminal transcript, or Git.

Run `configure-apim.ps1 -EnterpriseOnly` next for an existing demo deployment. It merges the shared `demo-open` key, dedicated secure/open endpoints, and existing apps telemetry configuration into that same ignored file without overwriting identities or earlier hosts' settings. `OpsBot.Mcp` remains subscription-key-only; only `OpsBot.Enterprise` acquires Entra tokens.

```powershell
dotnet run --no-build --project src\OpsBot.Enterprise -- --no-key
dotnet run --no-build --project src\OpsBot.Enterprise -- --no-token
dotnet run --no-build --project src\OpsBot.Enterprise -- --identity norole
dotnet run --no-build --project src\OpsBot.Enterprise
```

Expected connection results: 401, 401, 403, then five read-only tools. Each process stays interactive after a rejected connection; `reconnect` reloads local configuration, preserving launch flags.
