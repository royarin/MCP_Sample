param(
  [Parameter(Mandatory)][string]$SubscriptionId,
  [Parameter(Mandatory)][string]$ResourceGroup,
  [Parameter(Mandatory)][string]$BaseName,
  [string]$UniqueSuffix = ""
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\naming.ps1"
$BaseName = Resolve-DemoBaseName $BaseName $UniqueSuffix
$uri = "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.ApiManagement/service/apim-$BaseName/policies/policy?api-version=2024-05-01"
$token = az account get-access-token --subscription $SubscriptionId --resource https://management.azure.com/ --query accessToken -o tsv --only-show-errors
if ($LASTEXITCODE -ne 0) { throw "Could not acquire an ARM token for the CORS update." }
$headers = @{ Authorization = "Bearer $token"; Accept = "application/json" }
$response = Invoke-WebRequest -Method Get -Uri "$uri&format=xml" -Headers $headers -SkipHttpErrorCheck
$policy = [System.Xml.XmlDocument]::new()
$policy.PreserveWhitespace = $true
if ($response.StatusCode -eq 404) {
  $policy.LoadXml("<policies><inbound /><backend><forward-request /></backend><outbound /><on-error /></policies>")
} elseif ($response.StatusCode -eq 200) {
  # Decode the raw stream because APIM can return BOM-prefixed policy content.
  $json = [System.Text.Encoding]::UTF8.GetString($response.RawContentStream.ToArray()).TrimStart([char]0xFEFF)
  $current = $json | ConvertFrom-Json
  $policy.LoadXml($current.properties.value)
} else {
  throw "Could not read the global APIM policy: HTTP $([int]$response.StatusCode)."
}
$inbound = $policy.SelectSingleNode("/policies/inbound")
if ($null -eq $inbound) { throw "The global APIM policy has no inbound section." }
[xml]$before = $policy.OuterXml
[xml]$snippet = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\apim\cors-demo.xml") -Raw
$existing = @($inbound.SelectNodes("cors"))
foreach ($cors in $existing) { $null = $inbound.RemoveChild($cors) }
$null = $inbound.PrependChild($policy.ImportNode($snippet.DocumentElement, $true))
# Subscription-key failures can occur before the inbound CORS policy runs.
foreach ($sectionName in @("outbound", "on-error")) {
  $section = $policy.SelectSingleNode("/policies/$sectionName")
  if ($null -eq $section) {
    $section = $policy.CreateElement($sectionName)
    $onError = $policy.SelectSingleNode("/policies/on-error")
    if ($sectionName -eq "outbound" -and $null -ne $onError) {
      $null = $policy.DocumentElement.InsertBefore($section, $onError)
    } else {
      $null = $policy.DocumentElement.AppendChild($section)
    }
  }
  foreach ($header in @($section.SelectNodes("set-header[@name='Access-Control-Allow-Origin']"))) {
    $null = $section.RemoveChild($header)
  }
  $header = $policy.CreateElement("set-header")
  $header.SetAttribute("name", "Access-Control-Allow-Origin")
  $header.SetAttribute("exists-action", "override")
  $value = $policy.CreateElement("value")
  $value.InnerText = $snippet.cors.'allowed-origins'.origin
  $null = $header.AppendChild($value)
  $null = $section.PrependChild($header)
}
[xml]$after = $policy.OuterXml
if ($after.OuterXml -ceq $before.OuterXml) {
  Write-Host "Demo wildcard CORS is already configured."
  return
}
$etag = $response.Headers["ETag"] | Select-Object -First 1
if ($etag) { $headers["If-Match"] = $etag }
$body = @{ properties = @{ format = "xml"; value = $policy.OuterXml } } | ConvertTo-Json -Depth 10
$null = Invoke-WebRequest -Method Put -Uri $uri -Headers $headers -ContentType "application/json" -Body $body
Write-Host "Demo wildcard CORS configured globally; existing non-CORS policies preserved."
