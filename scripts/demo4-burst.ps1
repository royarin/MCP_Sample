param(
  [Parameter(Mandatory)][string]$BaseName,
  [ValidateRange(1, 100)][int]$Calls = 10,
  [string]$UniqueSuffix = ""
)
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\urls.ps1" -BaseName $BaseName -UniqueSuffix $UniqueSuffix
. "$PSScriptRoot\mcp-client.ps1"
$settings = Get-Content (Join-Path $PSScriptRoot "..\src\OpsBot.Enterprise\appsettings.Development.json") -Raw | ConvertFrom-Json
$token = Get-EnterpriseToken $settings
$client = New-McpProbe $ApimSecure $settings.Mcp.ApimSubscriptionKey $token
Initialize-McpProbe $client
$list = Invoke-McpRpc $client "tools/list" @{}
$tool = $list.Json.result.tools | Where-Object { @($_.inputSchema.required).Where({ $null -ne $_ }).Count -eq 0 } | Select-Object -First 1
if (-not $tool) { throw "No argument-free read tool was discovered." }
for ($n = 1; $n -le $Calls; $n++) {
  $response = Invoke-McpRpc $client "tools/call" @{ name = $tool.name; arguments = @{} }
  if ($response.Json.error) { throw "Tool protocol error: $($response.Json.error.message)" }
  $text = ($response.Json.result.content | ForEach-Object text) -join " "
  if ($response.Json.result.isError -and $text -match "Rate limit exceeded") {
    Write-Host "$n : HTTP $($response.Status) - $text" -ForegroundColor Red
    if ($response.Json.id -ne $response.RequestId) { throw "Rate-limit result has the wrong request id." }
    return
  }
  if ($response.Json.result.isError) { throw "Read tool failed: $text" }
  Write-Host "$n : HTTP $($response.Status) - $($tool.name) succeeded" -ForegroundColor Green
}
throw "The gateway did not return a rate-limit tool result within $Calls calls."
