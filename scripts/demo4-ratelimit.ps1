param(
  [Parameter(Mandatory)][string]$BaseName,
  [ValidateRange(1, 1000)][int]$Calls = 6,
  [ValidateRange(1, 300)][int]$Period = 60,
  [switch]$Show,
  [string]$UniqueSuffix = ""
)
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\..\infra\naming.ps1"
$BaseName = Resolve-DemoBaseName $BaseName $UniqueSuffix
$apim = "apim-$BaseName"
$group = az apim list --query "[?name=='$apim'].resourceGroup | [0]" -o tsv --only-show-errors
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($group)) { throw "Could not resolve the demo APIM service." }
foreach ($entry in @(@{ name = "opsdesk-ratelimit-calls"; value = "$Calls" }, @{ name = "opsdesk-ratelimit-period"; value = "$Period" })) {
  if ($Show) {
    $value = az apim nv show -g $group --service-name $apim --named-value-id $entry.name --query value -o tsv --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw "Could not read $($entry.name)." }
    Write-Host "$($entry.name) = $value"
  } else {
    az apim nv update -g $group --service-name $apim --named-value-id $entry.name --value $entry.value --only-show-errors -o none
    if ($LASTEXITCODE -ne 0) { throw "Could not update $($entry.name)." }
    Write-Host "$($entry.name) = $($entry.value)"
  }
}
