# Demo 3 rehearsal: measure a read-only briefing and write a sanitized report.
param([Parameter(Mandatory)][string]$BaseName, [string]$UniqueSuffix = "")
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\urls.ps1" -BaseName $BaseName -UniqueSuffix $UniqueSuffix
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src\OpsBot.Mcp'
$report = Join-Path $root 'artifacts\demo3-measurements.md'
$output = @(& dotnet run --no-build --project $project -- --compare $McpCode $McpTasks --permissions auto --ask $Question --measurement true)
$exitCode = $LASTEXITCODE
$output | ForEach-Object { Write-Host $_ }
if ($exitCode -ne 0) { throw "Measurement failed with exit code $exitCode; report not written." }
$lines = @($output | Where-Object { $_ -is [string] -and $_.StartsWith('MEASUREMENT_JSON:') })
if ($lines.Count -ne 1) { throw 'Measurement output is missing or ambiguous; report not written.' }
$data = $lines[0].Substring('MEASUREMENT_JSON:'.Length) | ConvertFrom-Json
if ($data.Deployment -notmatch '^[a-zA-Z0-9._-]+$' -or $data.Runs.Count -ne 2 -or $data.Catalogues.Count -ne 2) {
  throw 'Invalid measurement metadata; report not written.'
}
$seconds = @()
for ($i = 0; $i -lt 2; $i++) {
  $run = $data.Runs[$i]
  $catalogue = $data.Catalogues[$i]
  $elapsed = [TimeSpan]::Zero
  if (-not $run.Success -or $run.ToolCalls -le 0 -or $run.ModelTurns -le 0 -or $run.InputTokens -le 0 -or $run.OutputTokens -le 0 -or $catalogue.ToolCount -le 0 -or $catalogue.CatalogTokens -le 0 -or -not [TimeSpan]::TryParse($run.Elapsed, [ref]$elapsed) -or $elapsed.TotalSeconds -le 0) {
    throw 'Incomplete measurement; report not written.'
  }
  $seconds += $elapsed.TotalSeconds.ToString('0.00', [Globalization.CultureInfo]::InvariantCulture)
}
$reportText = @"
# Demo 3 measurements

Date: $(Get-Date -Format 'yyyy-MM-dd')
Model deployment: $($data.Deployment)

Endpoints: ``https://<apim-host>/opsdesk-code/mcp`` and ``https://<apim-host>/opsdesk-tasks/mcp``.

| Metric | Endpoint-shaped | Task-shaped |
| --- | ---: | ---: |
| Tools | $($data.Catalogues[0].ToolCount) | $($data.Catalogues[1].ToolCount) |
| Catalogue tokens | $($data.Catalogues[0].CatalogTokens) | $($data.Catalogues[1].CatalogTokens) |
| Tool calls | $($data.Runs[0].ToolCalls) | $($data.Runs[1].ToolCalls) |
| Model turns | $($data.Runs[0].ModelTurns) | $($data.Runs[1].ModelTurns) |
| Input tokens | $($data.Runs[0].InputTokens) | $($data.Runs[1].InputTokens) |
| Output tokens | $($data.Runs[0].OutputTokens) | $($data.Runs[1].OutputTokens) |
| Wall time (s) | $($seconds[0]) | $($seconds[1]) |
"@
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $report) -Force
Set-Content -LiteralPath $report -Value $reportText -Encoding utf8
Write-Host "Wrote $report"
