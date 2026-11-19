# Optional clipboard helper; the Enterprise host acquires and refreshes tokens itself.
param([ValidateSet("Default", "NoRole")][string]$Identity = "Default")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\mcp-client.ps1"
$settings = Get-Content (Join-Path $PSScriptRoot "..\src\OpsBot.Enterprise\appsettings.Development.json") -Raw | ConvertFrom-Json
$token = Get-EnterpriseToken $settings $Identity
Set-Clipboard -Value $token
Write-Host "The $Identity app token is on the clipboard, not in terminal output. Clear the clipboard after use." -ForegroundColor Cyan
