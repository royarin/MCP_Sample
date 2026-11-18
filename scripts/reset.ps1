# Puts the OpsDesk data back to seed state (inventory Red, no incidents). Run before every rehearsal/demo.
. "$PSScriptRoot/urls.ps1"
Invoke-RestMethod -Method Post -Uri "$ApiUrl/Database/reset" | Out-Null
Write-Host "OpsDesk reset at $ApiUrl – inventory is Red, incidents cleared" -ForegroundColor Green
