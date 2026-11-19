# Publishes the four .NET 10 apps and deploys ZIP packages to the Linux App Service apps.
param(
  [Parameter(Mandatory)][string]$SubscriptionId,
  [Parameter(Mandatory)][string]$ResourceGroup,
  [Parameter(Mandatory)][string]$BaseName,
  [string]$Configuration = "Release",
  [ValidateSet("api", "web", "mcp", "mcp-tasks")][string[]]$AppNames = @("api", "web", "mcp", "mcp-tasks"),
  [string]$UniqueSuffix = ""
)
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\naming.ps1"
$BaseName = Resolve-DemoBaseName $BaseName $UniqueSuffix
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$staging = Join-Path ([System.IO.Path]::GetTempPath()) "opsdesk-appservice-deploy-$([guid]::NewGuid().ToString('N'))"
$apps = @(
  @{ Key = "api"; Name = "app-$BaseName-api";       Project = "src/OpsDesk.Api/OpsDesk.Api.csproj" }
  @{ Key = "web"; Name = "app-$BaseName-web";       Project = "src/OpsDesk.Web/OpsDesk.Web.csproj" }
  @{ Key = "mcp"; Name = "app-$BaseName-mcp";       Project = "src/OpsDesk.Mcp/OpsDesk.Mcp.csproj" }
  @{ Key = "mcp-tasks"; Name = "app-$BaseName-mcp-tasks"; Project = "src/OpsDesk.Mcp.Tasks/OpsDesk.Mcp.Tasks.csproj" }
)

New-Item -ItemType Directory -Path $staging | Out-Null
try {
  foreach ($app in ($apps | Where-Object { $_.Key -in $AppNames })) {
    $publishDir = Join-Path $staging $app.Name
    $zipPath = Join-Path $staging "$($app.Name).zip"
    $projectPath = Join-Path $root $app.Project

    Write-Host "Publishing $($app.Project)" -ForegroundColor Cyan
    dotnet publish $projectPath --configuration $Configuration --output $publishDir
    if ($LASTEXITCODE) { throw "dotnet publish failed: $($app.Project)" }

    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
    Write-Host "Deploying $($app.Name)" -ForegroundColor Cyan
    az webapp deploy --subscription $SubscriptionId --resource-group $ResourceGroup `
      --name $app.Name --src-path $zipPath --type zip
    if ($LASTEXITCODE) { throw "App Service deployment failed: $($app.Name)" }
  }
}
finally {
  Remove-Item -LiteralPath $staging -Recurse -Force
}
