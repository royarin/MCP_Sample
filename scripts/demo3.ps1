# Demo 3: launch two interactive hosts; servers are already deployed.
param([Parameter(Mandatory)][string]$BaseName, [string]$UniqueSuffix = "")
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\urls.ps1" -BaseName $BaseName -UniqueSuffix $UniqueSuffix
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src\OpsBot.Mcp'
$endpointArgs = @('run', '--no-build', '--project', $project, '--', '--Mcp:Endpoint', $McpCode, '--label', 'endpoint-shaped', '--accent', 'yellow')
$taskArgs = @('run', '--no-build', '--project', $project, '--', '--Mcp:Endpoint', $McpTasks, '--label', 'task-shaped', '--accent', 'cyan')
if (Get-Command wt -ErrorAction SilentlyContinue) {
  & wt -w 0 new-tab --title 'endpoint-shaped' --startingDirectory $root dotnet @endpointArgs ';' split-pane -V --title 'task-shaped' --startingDirectory $root dotnet @taskArgs
  if ($LASTEXITCODE -ne 0) { throw "Windows Terminal failed with exit code $LASTEXITCODE" }
} else {
  # Start-Process joins arguments into one command line; quote the project path for spaces.
  $endpointArgs[3] = '"' + $project + '"'
  $taskArgs[3] = '"' + $project + '"'
  Start-Process dotnet -WorkingDirectory $root -ArgumentList $endpointArgs
  Start-Process dotnet -WorkingDirectory $root -ArgumentList $taskArgs
}
