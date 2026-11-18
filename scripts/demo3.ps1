# Demo 3 · Tool design is the UX for the model – two servers, one console host.
param([switch]$Local)
. "$PSScriptRoot/urls.ps1"
$a = $Local ? "http://localhost:5001/mcp" : $McpCode
$b = $Local ? "http://localhost:5002/mcp" : $McpTasks
if ($Local) {
  Start-Process dotnet -ArgumentList "run --project src/OpsDesk.Mcp" ; Start-Process dotnet -ArgumentList "run --project src/OpsDesk.Mcp.Tasks"; Start-Sleep 8
}
Write-Host "`n[3a/3b] same question, both servers – catalogue tokens, calls, turns, tokens, time" -ForegroundColor Cyan
dotnet run --no-build --project src/OpsBot.Mcp -- --compare $a $b --permissions auto --ask "$Question"
Write-Host "`n[3c] the typo – endpoint-shaped vs task-shaped error" -ForegroundColor Cyan
dotnet run --no-build --project src/OpsBot.Mcp -- --compare $a $b --ask "Raise a high incident for inventry: replication lag, reported by Arin, do not notify yet."
Write-Host "`n[3d] confirm with y when the permission gate asks → incident appears on $WebUrl" -ForegroundColor Yellow
