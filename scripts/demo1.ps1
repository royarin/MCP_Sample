# Demo 1 · "An agent without your systems is just a chatbot" – console only; API runs in Azure.
. "$PSScriptRoot/urls.ps1"
Write-Host "`n[1a] no tools" -ForegroundColor Cyan
dotnet run --no-build --project src/OpsBot.NoTools -- --ask "$Question"
Write-Host "`n[1b] hand-wired functions" -ForegroundColor Cyan
dotnet run --no-build --project src/OpsBot.Tools -- --OpsDesk:BaseUrl "$ApiUrl" --ask "$Question"
Write-Host "`n[1c] open src/OpsBot.Tools/OpsDeskFunctions.cs – count what is bespoke to this host." -ForegroundColor Yellow
