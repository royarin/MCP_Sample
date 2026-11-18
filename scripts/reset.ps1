# Puts the OpsDesk data back to seed state (inventory Red, no incidents). Run before every rehearsal/demo.
param([Parameter(Mandatory)][string]$BaseName)
. "$PSScriptRoot/urls.ps1" -BaseName $BaseName
Invoke-RestMethod -Method Post -Uri "$ApiDirectUrl/Database/reset" | Out-Null
Write-Host "OpsDesk reset at $ApiDirectUrl – inventory is Red, incidents cleared" -ForegroundColor Green
