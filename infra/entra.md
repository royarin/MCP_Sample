# Entra ID setup for Demo 4

## 1. MCP resource app – `app-opsdesk-mcp`

```bash
APP=$(az ad app create --display-name app-opsdesk-mcp --query appId -o tsv)
az ad app update --id $APP --identifier-uris "api://opsdesk-mcp"
az ad sp create --id $APP
```

Portal → the app → **App roles** → add two roles (allowed member types: *Both*):

| Display name | Value |
|---|---|
| Ops reader | `Ops.Reader` |
| Ops operator | `Ops.Operator` |

Optional (user flows via Copilot): **Expose an API** → scope `Ops.Access`, admin consent.

## 2. Service agents (for `OpsBot.Mcp --Mcp:Auth entra`)

```bash
for A in opsdesk-agent-reader opsdesk-agent-operator; do
  ID=$(az ad app create --display-name $A --query appId -o tsv); az ad sp create --id $ID
  az ad app credential reset --id $ID --display-name demo --years 1      # copy password → user-secrets
done
```

Portal → each agent app → **API permissions → Add → My APIs → app-opsdesk-mcp → Application permissions** →
`Ops.Reader` for the reader, `Ops.Operator` for the operator → **Grant admin consent**.

## 3. Demo users (for Copilot via `${input:token}`)

Assign `Ops.Reader` to `opsdesk.reader@<tenant>` and `Ops.Operator` to `opsdesk.operator@<tenant>` under
**Enterprise applications → app-opsdesk-mcp → Users and groups**. Tokens: `scripts/tokens.ps1`.

## 4. Local agent configuration

Set `Mcp:Entra:TenantId`, `Scope`, and the `Identities.Reader` and `Identities.Operator` client IDs/secrets in the ignored `src/OpsBot.Mcp/appsettings.Development.json`. Set `Mcp:Entra:Role` to `Reader` or `Operator` for the selected run. The Demo 4 script reads these values locally and never prints or passes secrets on its command line.

All calls still include the APIM product subscription key; the JWT supplies the caller identity and role.
