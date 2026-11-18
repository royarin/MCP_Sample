# Post-deploy: import the OpsDesk API into APIM, apply policies, register in API Center, print laptop config.
# MCP-server creation in APIM is still portal-driven in many tenants – this script prints the exact steps
# when the REST call is not available (see apim/README.md). Requires: az CLI (az login), Bicep deployed.
param(
  [Parameter(Mandatory)][string]$ResourceGroup,
  [string]$BaseName = "opsdesk-mcp",
  [Parameter(Mandatory)][string]$TenantId,
  [Parameter(Mandatory)][string]$McpResourceAppId      # app-opsdesk-mcp (api://opsdesk-mcp)
)
$ErrorActionPreference = "Stop"
$apim = "apim-$BaseName"; $apic = "apic-$BaseName"
$out  = az deployment group show -g $ResourceGroup -n main --query properties.outputs -o json | ConvertFrom-Json
$apiUrl = $out.apiUrl.value; $tasksUrl = $out.mcpTasksUrl.value

Write-Host "1. Import OpsDesk API" -ForegroundColor Cyan
az apim api import -g $ResourceGroup -n $apim --path opsdesk --api-id opsdesk-api --display-name "OpsDesk API" `
  --specification-format OpenApiJson --specification-url "$apiUrl/openapi/v1.json" --service-url $apiUrl | Out-Null

Write-Host "2. Named values for the policies" -ForegroundColor Cyan
az apim nv create -g $ResourceGroup -n $apim --named-value-id tenant-id --display-name tenant-id --value $TenantId | Out-Null
az apim nv create -g $ResourceGroup -n $apim --named-value-id opsdesk-mcp-app-id --display-name opsdesk-mcp-app-id --value $McpResourceAppId | Out-Null

Write-Host "3. Policies (merged auth + governance for opsdesk-secure)" -ForegroundColor Cyan
Write-Host "   → open APIM → MCP Servers → opsdesk-secure → Policies and paste apim/policy-mcp-auth.xml inbound first, then apim/policy-mcp-governance.xml."
Write-Host "   (The MCP-server policy scope has no stable CLI yet; the API-scope policy can be set with: az apim api policy ...)"

Write-Host "4. MCP servers" -ForegroundColor Cyan
Write-Host "   → APIM → APIs → MCP Servers → Create: 'opsdesk-apim' (expose OpsDesk API, all operations, product demo-open)"
Write-Host "   → APIM → APIs → MCP Servers → Create: 'opsdesk-secure' (expose OpsDesk API or pass-through to $tasksUrl), subscription required = off"

Write-Host "5. API Center registration" -ForegroundColor Cyan
az apic api create -g $ResourceGroup -n $apic --api-id opsdesk-secure --title "OpsDesk MCP (secure)" --type mcp `
  --custom-properties '{"owner":"Platform Ops","dataClassification":"internal","writeTools":"raise_incident, update_system_status"}' 2>$null
az apic api version create -g $ResourceGroup -n $apic --api-id opsdesk-secure --version-id v1 --title "1.0" --lifecycle-stage production 2>$null
az apic environment create -g $ResourceGroup -n $apic --environment-id demo --title "Demo" --type testing 2>$null
az apic api deployment create -g $ResourceGroup -n $apic --api-id opsdesk-secure --version-id v1 --definition-id openapi `
  --deployment-id apim --title "APIM gateway" --environment-id /workspaces/default/environments/demo `
  --server "{\"runtimeUri\":[\"$($out.apimGateway.value)/opsdesk-secure/mcp\"]}" 2>$null

Write-Host "6. Diagnostics" -ForegroundColor Cyan
Write-Host "   → APIM → APIs → OpsDesk API → Settings → Diagnostics: App Insights logger 'appinsights', sampling 100 %, correlation W3C."

Write-Host "`nLaptop configuration (run from any OpsBot.* project folder – secrets are shared):" -ForegroundColor Green
@"
dotnet user-secrets set OpsDesk:BaseUrl $apiUrl
dotnet user-secrets set Mcp:Endpoint $($out.mcpUrl.value)
dotnet user-secrets set APPLICATIONINSIGHTS_CONNECTION_STRING "$($out.appInsightsConnectionString.value)"
dotnet user-secrets set Mcp:Entra:TenantId $TenantId
# plus AzureOpenAI:Endpoint / ApiKey / DeploymentName and Mcp:Entra:ClientId / ClientSecret (see infra/entra.md)
"@
