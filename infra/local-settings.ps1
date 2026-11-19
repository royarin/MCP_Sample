function Update-LocalSettings([string]$Path, [System.Collections.IDictionary]$Settings) {
  if ([System.IO.Path]::GetFileName($Path) -ne "appsettings.Development.json") {
    throw "Local credentials must be written only to appsettings.Development.json."
  }
  function Merge-Settings([System.Collections.IDictionary]$Target, [System.Collections.IDictionary]$Patch) {
    foreach ($key in $Patch.Keys) {
      if ($Patch[$key] -is [System.Collections.IDictionary] -and $Target[$key] -is [System.Collections.IDictionary]) {
        Merge-Settings $Target[$key] $Patch[$key]
      } else {
        $Target[$key] = $Patch[$key]
      }
    }
  }
  $templatePath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($Path), "appsettings.json")
  $current = if (Test-Path -LiteralPath $templatePath) {
    Get-Content -LiteralPath $templatePath -Raw | ConvertFrom-Json -AsHashtable
  } else { @{} }
  if (Test-Path -LiteralPath $Path) {
    Merge-Settings $current (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable)
  }
  Merge-Settings $current $Settings
  $current | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $Path -Encoding utf8
}

function Invoke-DemoRest([string]$Method, [string]$Uri, [object]$Body, [hashtable]$Headers = @{}) {
  $path = Join-Path ([System.IO.Path]::GetTempPath()) "opsdesk-rest-$([guid]::NewGuid().ToString('N')).json"
  try {
    $arguments = @("rest", "--method", $Method, "--url", $Uri, "--only-show-errors", "-o", "json")
    if ($null -ne $Body) {
      $Body | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $path -Encoding utf8
      $arguments += @("--body", "@$path")
      $Headers = @{ "Content-Type" = "application/json" } + $Headers
    }
    if ($Headers.Count -gt 0) {
      $arguments += "--headers"
      foreach ($header in $Headers.GetEnumerator()) { $arguments += "$($header.Key)=$($header.Value)" }
    }
    $result = & az @arguments
    if ($LASTEXITCODE -ne 0) { throw "Azure REST $Method failed." }
    if ($result) { return ($result | ConvertFrom-Json) }
  }
  finally {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
  }
}
