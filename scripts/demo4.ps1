# Demo 4 · Enterprise-grade MCP on Azure – opsdesk-secure, pre-provisioned. Config is shown in the portal; this is the "in action" part.
param([switch]$Operator)
. "$PSScriptRoot/urls.ps1"
$settingsPath = Join-Path $PSScriptRoot '..\src\OpsBot.Mcp\appsettings.Development.json'
$localSettings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$role = if ($Operator) { "Operator" } else { "Reader" }
$identity = $localSettings.Mcp.Entra.Identities.$role
if ([string]::IsNullOrWhiteSpace($identity.ClientId) -or [string]::IsNullOrWhiteSpace($identity.ClientSecret)) {
  throw "Configure the $role identity under Mcp:Entra:Identities in src/OpsBot.Mcp/appsettings.Development.json."
}
$roleArg = "--Mcp:Entra:Role"

Write-Host "`n[Security] anonymous tools/list → expect 401" -ForegroundColor Cyan
try {
  Invoke-WebRequest -Method Post -Uri $Secure -Headers @{ "Ocp-Apim-Subscription-Key" = $localSettings.Mcp.ApimSubscriptionKey } `
    -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | Out-Null
}
catch { Write-Host "HTTP $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red }

Write-Host "`n[Security] authenticated agent – roles printed at startup" -ForegroundColor Cyan
dotnet run --no-build --project src/OpsBot.Mcp -- --Mcp:Endpoint $Secure --Mcp:Auth entra $roleArg $role --ask "$Question"

Write-Host "`n[Governance] write action as $(if ($Operator) {'Operator → succeeds'} else {'Reader → blocked by policy'})" -ForegroundColor Cyan
dotnet run --no-build --project src/OpsBot.Mcp -- --Mcp:Endpoint $Secure --Mcp:Auth entra $roleArg $role --ask "$WriteAsk"

Write-Host "`n[Discoverability] API Center → opsdesk-secure   [Observability] App Insights → transaction search (scripts/appinsights.kql)" -ForegroundColor Yellow
