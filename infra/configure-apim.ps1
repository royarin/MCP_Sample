param(
  [Parameter(Mandatory)][string]$SubscriptionId,
  [Parameter(Mandatory)][string]$ResourceGroup,
  [Parameter(Mandatory)][string]$BaseName,
  [Parameter(Mandatory)][string]$FoundryProjectEndpoint,
  [string]$FoundryDeploymentName = "gpt-4o",
  [string]$TenantId,
  [string]$McpResourceAppId,
  [switch]$EnterpriseOnly,
  [switch]$RefreshApi,
  [string]$UniqueSuffix = ""
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\naming.ps1"
$BaseName = Resolve-DemoBaseName $BaseName $UniqueSuffix
$apiVersion = "2025-09-01-preview"
$subscriptionApiVersion = "2022-08-01"
$ApimName = "apim-$BaseName"
$ApiUrl = "https://app-$BaseName-api.azurewebsites.net"
$McpCodeUrl = "https://app-$BaseName-mcp.azurewebsites.net"
$McpTasksUrl = "https://app-$BaseName-mcp-tasks.azurewebsites.net"
$baseUrl = "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.ApiManagement/service/$ApimName"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot\local-settings.ps1"

function Invoke-AzRestPut([string]$Uri, [object]$Body) {
  $json = $Body | ConvertTo-Json -Depth 20 -Compress
  if ($Uri -match "/policies/policy\?") {
    # APIM can return BOM-prefixed XML here, which az rest cannot decode on Windows.
    $token = az account get-access-token --resource https://management.azure.com/ --query accessToken -o tsv --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw "Could not acquire an ARM token for the policy update." }
    $null = Invoke-WebRequest -Method Put -Uri $Uri -ContentType "application/json" -Body $json `
      -Headers @{ Authorization = "Bearer $token"; "If-Match" = "*" }
    return
  }
  $bodyPath = Join-Path $env:TEMP ("opsdesk-apim-body-" + [guid]::NewGuid().ToString("N") + ".json")
  [System.IO.File]::WriteAllText($bodyPath, $json, [System.Text.UTF8Encoding]::new($false))
  az rest --method put --url $Uri --headers "If-Match=*" "Content-Type=application/json" --body "@$bodyPath" --only-show-errors -o none
  $exitCode = $LASTEXITCODE
  Remove-Item -LiteralPath $bodyPath -Force -ErrorAction SilentlyContinue
  if ($exitCode -ne 0) { throw "APIM update failed: $Uri" }
}

function Set-McpPassthroughServer([hashtable]$Server) {
  $backendId = "$($Server.Id)-backend"
  Invoke-AzRestPut "$baseUrl/backends/${backendId}?api-version=$apiVersion" @{
    properties = @{
      protocol = "http"
      title = "$($Server.Name) backend"
      url = $Server.Backend.TrimEnd("/")
    }
  }

  Invoke-AzRestPut "$baseUrl/apis/$($Server.Id)?api-version=$apiVersion" @{
    properties = @{
      type = "mcp"
      displayName = $Server.Name
      description = "Streamable HTTP passthrough to the deployed OpsDesk MCP server."
      path = $Server.Path
      protocols = @("https")
      backendId = $backendId
      serviceUrl = $null
      subscriptionRequired = $true
      mcpProperties = @{
        endpoints = @{ mcp = @{ uriTemplate = "/mcp" } }
      }
    }
  }
}

function Set-LocalSettings([string]$RelativePath, [object]$Settings) {
  Update-LocalSettings (Join-Path $repoRoot $RelativePath) $Settings
}

function Set-GeneratedMcpServer([string]$Id, [hashtable]$Properties) {
  $uri = "$baseUrl/apis/${Id}?api-version=$apiVersion"
  $existingJson = az rest --method get --url $uri --only-show-errors -o json 2>$null
  if ($LASTEXITCODE -eq 0) {
    $current = ($existingJson | ConvertFrom-Json).properties
    if ($current.type -eq "mcp" -and $current.path -eq $Properties.path -and $current.subscriptionRequired) {
      # Replacing a generated MCP API clears its tool collection, even for an identical PUT.
      if ($current.displayName -ne $Properties.displayName) {
        $null = Invoke-DemoRest "PATCH" $uri @{ properties = @{ displayName = $Properties.displayName } } @{ "If-Match" = "*" }
      }
      return
    }
  }
  Invoke-AzRestPut $uri @{ properties = $Properties }
}

function Convert-NullableTypes([object]$Node) {
  if ($null -eq $Node) { return }
  if ($Node -is [System.Collections.IList] -and $Node -isnot [string]) {
    foreach ($item in $Node) { Convert-NullableTypes $item }
    return
  }
  if ($Node -is [pscustomobject]) {
    $typeProperty = $Node.PSObject.Properties["type"]
    if ($typeProperty -and $typeProperty.Value -is [array]) {
      $types = @($typeProperty.Value)
      $nullable = $types -contains "null"
      $nonNullTypes = @($types | Where-Object { $_ -ne "null" })
      if ($nonNullTypes.Count -eq 0) { throw "OpenAPI schema contains a type union with no non-null value." }
      if ($nonNullTypes.Count -gt 1) {
        Write-Warning "OpenAPI 3.1 type union '$($nonNullTypes -join ',')' normalized to '$($nonNullTypes[0])' for APIM."
      }
      $typeProperty.Value = $nonNullTypes[0]
      if ($nullable) { $Node | Add-Member -NotePropertyName nullable -NotePropertyValue $true -Force }
    }
    foreach ($property in @($Node.PSObject.Properties)) { Convert-NullableTypes $property.Value }
  }
}

az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) { throw "Could not select subscription '$SubscriptionId'." }

if (-not $EnterpriseOnly) {
$product = az apim product show -g $ResourceGroup -n $ApimName --product-id demo-open --query name -o tsv 2>$null
if ($LASTEXITCODE -ne 0) {
  az apim product create -g $ResourceGroup -n $ApimName --product-id demo-open --product-name "OpsDesk Demo" `
    --description "Subscription-key access for the OpsDesk MCP demonstrations." --subscription-required true --state published --only-show-errors -o none
  if ($LASTEXITCODE -ne 0) { throw "Could not create APIM product 'demo-open'." }
}

$currentApiJson = az apim api show -g $ResourceGroup -n $ApimName --api-id opsdesk-api -o json --only-show-errors 2>$null
$currentApi = if ($LASTEXITCODE -eq 0) { $currentApiJson | ConvertFrom-Json } else { $null }
if ($RefreshApi -or -not $currentApi -or
    ([string]$currentApi.serviceUrl).TrimEnd("/") -ne $ApiUrl.TrimEnd("/") -or -not $currentApi.subscriptionRequired) {
Write-Host "Importing OpsDesk API with subscription-key enforcement..." -ForegroundColor Cyan
$specification = Invoke-RestMethod "$ApiUrl/openapi/v1.json"
$specification.openapi = "3.0.3"
$specification.PSObject.Properties.Remove("jsonSchemaDialect")
Convert-NullableTypes $specification
$specificationPath = Join-Path $env:TEMP "opsdesk-api-apim.openapi.json"
$specification | ConvertTo-Json -Depth 100 | Set-Content -Path $specificationPath -Encoding utf8
az apim api import -g $ResourceGroup -n $ApimName --api-id opsdesk-api --path opsdesk --display-name "opsdesk-api" `
  --protocols https --subscription-required true --specification-format OpenApiJson `
  --specification-path $specificationPath --service-url $ApiUrl --only-show-errors -o none
$importExitCode = $LASTEXITCODE
Remove-Item -LiteralPath $specificationPath -Force -ErrorAction SilentlyContinue
if ($importExitCode -ne 0) { throw "Could not import OpsDesk API into APIM." }
} else {
  Write-Host "Keeping the existing frozen OpsDesk API import." -ForegroundColor Cyan
  if ($currentApi.displayName -ne "opsdesk-api") {
    $null = Invoke-DemoRest "PATCH" "$baseUrl/apis/opsdesk-api?api-version=$apiVersion" `
      @{ properties = @{ displayName = "opsdesk-api" } } @{ "If-Match" = "*" }
  }
}

$apiProduct = az apim product api check -g $ResourceGroup -n $ApimName --product-id demo-open --api-id opsdesk-api -o tsv 2>$null
if ($LASTEXITCODE -ne 0 -or $apiProduct -ne "true") {
  az apim product api add -g $ResourceGroup -n $ApimName --product-id demo-open --api-id opsdesk-api --only-show-errors -o none
  if ($LASTEXITCODE -ne 0) { throw "Could not bind OpsDesk API to product 'demo-open'." }
}

$servers = @(
  @{ Id = "opsdesk-code"; Name = "opsdesk-code"; Path = "opsdesk-code"; Backend = $McpCodeUrl },
  @{ Id = "opsdesk-tasks"; Name = "opsdesk-tasks"; Path = "opsdesk-tasks"; Backend = $McpTasksUrl }
)

foreach ($server in $servers) {
  Set-McpPassthroughServer $server

  $productBinding = az apim product api check -g $ResourceGroup -n $ApimName --product-id demo-open --api-id $server.Id -o tsv 2>$null
  if ($LASTEXITCODE -ne 0 -or $productBinding -ne "true") {
    az apim product api add -g $ResourceGroup -n $ApimName --product-id demo-open --api-id $server.Id --only-show-errors -o none
    if ($LASTEXITCODE -ne 0) { throw "Could not bind MCP server '$($server.Id)' to product 'demo-open'." }
  }
}

$apiMcpBody = @{
  properties = @{
    type = "mcp"
    displayName = "opsdesk-apim"
    description = "MCP tools generated from the subscription-protected OpsDesk REST API."
    path = "opsdesk-apim"
    protocols = @("https")
    subscriptionRequired = $true
  }
}
Set-GeneratedMcpServer "opsdesk-apim" $apiMcpBody.properties
$existingOpenTools = Invoke-DemoRest "GET" "$baseUrl/apis/opsdesk-apim/tools?api-version=$apiVersion" $null

$operations = az apim api operation list -g $ResourceGroup -n $ApimName --api-id opsdesk-api -o json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Could not list imported OpsDesk API operations." }
$toolCount = 0
foreach ($operation in $operations) {
  $method = $operation.method
  $template = $operation.urlTemplate
  if ($method -eq "POST" -and $template -eq "/Database/reset") { continue }

  $toolId = $operation.name
  $displayName = $toolId
  $description = $operation.description
  if ([string]::IsNullOrWhiteSpace($description)) { $description = "$method $template" }
  $operationId = "$baseUrl/apis/opsdesk-api/operations/$toolId"
  $toolBody = @{
    properties = @{
      displayName = $displayName
      description = $description
      operationId = $operationId
    }
  }
  $existing = $existingOpenTools.value | Where-Object name -eq $toolId
  if (-not $existing -or
      $existing.properties.operationId.Replace("https://management.azure.com", "") -ne $operationId.Replace("https://management.azure.com", "") -or
      $existing.properties.description -ne $description) {
    Write-Host "  Open tool: $toolId" -ForegroundColor Cyan
    Invoke-AzRestPut "$baseUrl/apis/opsdesk-apim/tools/${toolId}?api-version=$apiVersion" $toolBody
  }
  $toolCount++
}
if ($toolCount -eq 0) { throw "No safe OpsDesk operations were found to expose as MCP tools." }

$productBinding = az apim product api check -g $ResourceGroup -n $ApimName --product-id demo-open --api-id opsdesk-apim -o tsv 2>$null
if ($LASTEXITCODE -ne 0 -or $productBinding -ne "true") {
  az apim product api add -g $ResourceGroup -n $ApimName --product-id demo-open --api-id opsdesk-apim --only-show-errors -o none
  if ($LASTEXITCODE -ne 0) { throw "Could not bind API-backed MCP server to product 'demo-open'." }
}

$subscriptionUrl = "$baseUrl/subscriptions/opsdesk-demo-local?api-version=$subscriptionApiVersion"
$subscriptionJson = az rest --method get --url $subscriptionUrl --only-show-errors -o json 2>$null
if ($LASTEXITCODE -ne 0) {
  $subscriptionBody = @{
    properties = @{
      displayName = "OpsDesk local demo client"
      scope = "/products/demo-open"
      state = "active"
    }
  }
  Invoke-AzRestPut $subscriptionUrl $subscriptionBody
}

$secretsUrl = "$baseUrl/subscriptions/opsdesk-demo-local/listSecrets?api-version=$subscriptionApiVersion"
$secretsJson = az rest --method post --url $secretsUrl --only-show-errors -o json
if ($LASTEXITCODE -ne 0) { throw "Could not retrieve the demo subscription key for local configuration." }
$subscriptionKey = ($secretsJson | ConvertFrom-Json).primaryKey
if ([string]::IsNullOrWhiteSpace($subscriptionKey)) { throw "APIM returned an empty primary subscription key." }
} else {
  Write-Host "Leaving the existing Demo 1-3 API registrations and tool catalogs unchanged." -ForegroundColor Cyan
  $operations = az apim api operation list -g $ResourceGroup -n $ApimName --api-id opsdesk-api -o json --only-show-errors | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0) { throw "The existing imported OpsDesk API is required for EnterpriseOnly." }
  $openTools = Invoke-DemoRest "GET" "$baseUrl/apis/opsdesk-apim/tools?api-version=$apiVersion" $null
  $toolCount = @($openTools.value).Count
  if ($toolCount -ne 11) { throw "Expected the existing open twin to retain eleven tools." }
  $openSecrets = Invoke-DemoRest "POST" "$baseUrl/subscriptions/opsdesk-demo-local/listSecrets?api-version=$subscriptionApiVersion" $null
  $subscriptionKey = $openSecrets.primaryKey
  if ([string]::IsNullOrWhiteSpace($subscriptionKey)) { throw "The existing demo-open subscription is required." }
}

$foundry = @{ ProjectEndpoint = $FoundryProjectEndpoint; DeploymentName = $FoundryDeploymentName; Credential = "AzureCliCredential" }
if (-not $EnterpriseOnly) {
Set-LocalSettings "src\OpsBot.NoTools\appsettings.Development.json" @{ Foundry = $foundry }
Set-LocalSettings "src\OpsBot.Tools\appsettings.Development.json" @{
  Foundry = $foundry
  OpsDesk = @{ BaseUrl = "https://$($ApimName).azure-api.net/opsdesk/" }
  Apim = @{ SubscriptionKey = $subscriptionKey }
}
Set-LocalSettings "src\OpsBot.Mcp\appsettings.Development.json" @{
  Foundry = $foundry
  Mcp = @{
    Endpoint = "https://$($ApimName).azure-api.net/opsdesk-code/mcp"
    ApimSubscriptionKey = $subscriptionKey
  }
}
}

Write-Host "Configuring the curated enterprise MCP server..." -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($TenantId)) {
  $TenantId = az account show --query tenantId -o tsv --only-show-errors
  if ($LASTEXITCODE -ne 0) { throw "Could not resolve the demo tenant." }
}
if ([string]::IsNullOrWhiteSpace($McpResourceAppId)) {
  $identityPath = Join-Path $repoRoot "src\OpsBot.Enterprise\appsettings.Development.json"
  if (Test-Path -LiteralPath $identityPath) {
    $identitySettings = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
    $McpResourceAppId = $identitySettings.Mcp.Entra.ResourceAppId
  }
}
if ([string]::IsNullOrWhiteSpace($McpResourceAppId)) {
  throw "Run infra\configure-entra.ps1 first, or supply McpResourceAppId."
}
Set-GeneratedMcpServer "opsdesk-apim-secure" @{
    type = "mcp"; displayName = "opsdesk-apim-secure"
    description = "Read-only OpsDesk MCP tools protected by Entra identity and a per-agent budget."
    path = "opsdesk-apim-secure"; protocols = @("https"); subscriptionRequired = $true
}
$readTemplates = @("/SystemStatus", "/SystemStatus/{systemId}", "/SystemOwner/{systemId}", "/Runbook/{systemId}", "/Incident")
$readOperations = @($operations | Where-Object { $_.method -eq "GET" -and $_.urlTemplate -in $readTemplates })
if ($readOperations.Count -ne 5) { throw "Expected exactly five curated GET operations, found $($readOperations.Count)." }
$existingTools = Invoke-DemoRest "GET" "$baseUrl/apis/opsdesk-apim-secure/tools?api-version=$apiVersion" $null
$readToolIds = @($readOperations | ForEach-Object { "secure-$($_.name)" })
foreach ($tool in $existingTools.value) {
  if ($tool.name -notin $readToolIds) {
    $null = Invoke-DemoRest "DELETE" "$baseUrl/apis/opsdesk-apim-secure/tools/$($tool.name)?api-version=$apiVersion" $null
  }
}
foreach ($operation in $readOperations) {
  # This APIM preview requires tool resource IDs to be unique across MCP servers.
  $toolId = "secure-$($operation.name)"
  $existing = $existingTools.value | Where-Object name -eq $toolId
  $expectedOperation = "$baseUrl/apis/opsdesk-api/operations/$($operation.name)"
  if ($existing -and $existing.properties.operationId.Replace("https://management.azure.com", "") -eq
      $expectedOperation.Replace("https://management.azure.com", "") -and
      $existing.properties.description -eq "$($operation.method) $($operation.urlTemplate)") {
    continue
  }
  Write-Host "  Curated tool: $($operation.name)" -ForegroundColor Cyan
  Invoke-AzRestPut "$baseUrl/apis/opsdesk-apim-secure/tools/${toolId}?api-version=$apiVersion" @{
    properties = @{ displayName = $operation.name; description = "$($operation.method) $($operation.urlTemplate)"
      operationId = "$baseUrl/apis/opsdesk-api/operations/$($operation.name)" }
  }
}
Invoke-AzRestPut "$baseUrl/products/demo-open/apis/opsdesk-apim-secure?api-version=$subscriptionApiVersion" @{}
$retiredSubscription = "$baseUrl/subscriptions/opsdesk-demo-secure?api-version=$subscriptionApiVersion"
$null = az rest --method get --url $retiredSubscription --only-show-errors -o none 2>$null
if ($LASTEXITCODE -eq 0) {
  Invoke-AzRestPut $retiredSubscription @{
    properties = @{ displayName = "OpsDesk Enterprise client (retired)"; scope = "/products/demo-secure"; state = "suspended" }
  }
}
$retiredBinding = "$baseUrl/products/demo-secure/apis/opsdesk-apim-secure?api-version=$subscriptionApiVersion"
$null = az rest --method get --url $retiredBinding --only-show-errors -o none 2>$null
if ($LASTEXITCODE -eq 0) {
  $null = Invoke-DemoRest "DELETE" $retiredBinding $null
}
foreach ($entry in @(
  @{ name = "tenant-id"; value = $TenantId },
  @{ name = "opsdesk-mcp-app-id"; value = $McpResourceAppId },
  @{ name = "opsdesk-ratelimit-calls"; value = "6" },
  @{ name = "opsdesk-ratelimit-period"; value = "60" }
)) {
  Invoke-AzRestPut "$baseUrl/namedValues/$($entry.name)?api-version=$subscriptionApiVersion" @{
    properties = @{ displayName = $entry.name; value = $entry.value; secret = $false }
  }
}
$policy = Get-Content (Join-Path $repoRoot "apim\policy-mcp-secure.xml") -Raw
Invoke-AzRestPut "$baseUrl/apis/opsdesk-apim-secure/policies/policy?api-version=$subscriptionApiVersion" @{
  properties = @{ format = "rawxml"; value = $policy }
}
$loggerId = "$($baseUrl.Replace('https://management.azure.com', ''))/loggers/appinsights"
Invoke-AzRestPut "$baseUrl/apis/opsdesk-apim-secure/diagnostics/applicationinsights?api-version=$subscriptionApiVersion" @{
  properties = @{ loggerId = $loggerId; alwaysLog = "allErrors"; httpCorrelationProtocol = "W3C"
    logClientIp = $false; verbosity = "information"
    sampling = @{ samplingType = "fixed"; percentage = 100 }
    frontend = @{ request = @{ headers = @("traceparent"); body = @{ bytes = 0 } }
      response = @{ headers = @(); body = @{ bytes = 0 } } }
    backend = @{ request = @{ headers = @("traceparent"); body = @{ bytes = 0 } }
      response = @{ headers = @(); body = @{ bytes = 0 } } }
  }
}
$insights = Invoke-DemoRest "GET" "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Insights/components/appi-$BaseName-apps?api-version=2020-02-02" $null
$appsInsights = $insights.properties.ConnectionString
if ([string]::IsNullOrWhiteSpace($appsInsights)) { throw "Could not resolve existing apps telemetry configuration." }
Set-LocalSettings "src\OpsBot.Enterprise\appsettings.Development.json" @{
  Foundry = $foundry
  Mcp = @{ Endpoint = "https://$ApimName.azure-api.net/opsdesk-apim-secure/mcp"
    ApimSubscriptionKey = $subscriptionKey; OpenEndpoint = "https://$ApimName.azure-api.net/opsdesk-apim/mcp"
    OpenSubscriptionKey = $subscriptionKey; Entra = @{ TenantId = $TenantId; ResourceAppId = $McpResourceAppId } }
  ApiCenter = @{ SubscriptionId = $SubscriptionId; ResourceGroup = $ResourceGroup; Name = "apic-$BaseName"; ApiId = "opsdesk-apim-secure" }
  APPLICATIONINSIGHTS_CONNECTION_STRING = $appsInsights
}

& "$PSScriptRoot\configure-cors.ps1" -SubscriptionId $SubscriptionId -ResourceGroup $ResourceGroup -BaseName $BaseName

Write-Host "APIM configured: REST API, endpoint-shaped MCP, task-shaped MCP, API-backed MCP, and curated enterprise MCP." -ForegroundColor Green
Write-Host "Product 'demo-open' requires a subscription key. The key was written only to ignored appsettings.Development.json files." -ForegroundColor Green
Write-Host "Enterprise reuses demo-open and existing App Insights; Entra Ops.Agent, the 6/min budget, and diagnostics apply only to opsdesk-apim-secure." -ForegroundColor Green
Write-Host "Created $toolCount API-backed MCP tools; Database/reset is intentionally excluded."
