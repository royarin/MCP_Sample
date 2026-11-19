param(
  [Parameter(Mandatory)][string]$BaseName,
  [ValidateSet("normal", "no-key", "no-token", "norole", "open", "discover")][string]$Mode = "normal",
  [string]$UniqueSuffix = ""
)
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\urls.ps1" -BaseName $BaseName -UniqueSuffix $UniqueSuffix
$project = Join-Path $PSScriptRoot "..\src\OpsBot.Enterprise\OpsBot.Enterprise.csproj"
$launch = switch ($Mode) {
  "no-key" { @("--no-key") }
  "no-token" { @("--no-token") }
  "norole" { @("--identity", "norole") }
  "open" { @("--open", "--Mcp:OpenEndpoint", $ApimMcp) }
  "discover" { @("--discover", "opsdesk-apim-secure") }
  default { @("--Mcp:Endpoint", $ApimSecure) }
}
Write-Host "Demo 4: $Mode. Interactive only; type exit to finish." -ForegroundColor Cyan
dotnet run --no-build --project $project -- @launch
if ($LASTEXITCODE -ne 0) { throw "OpsBot.Enterprise exited with code $LASTEXITCODE." }
