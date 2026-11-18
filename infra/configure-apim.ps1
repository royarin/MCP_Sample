param(
  [string]$SubscriptionId = "<subscription-id>",
  [string]$ResourceGroup = "<resource-group>",
  [string]$ApimName = "<apim-name>",
  [string]$ApiUrl = "https://app-<base-name>-api.azurewebsites.net",
  [string]$McpCodeUrl = "https://app-<base-name>-mcp.azurewebsites.net",
  [string]$McpTasksUrl = "https://app-<base-name>-mcp-tasks.azurewebsites.net",
  [string]$FoundryProjectEndpoint = "<foundry-project-endpoint>",
  [string]$FoundryDeploymentName = "gpt-4o"
)

$ErrorActionPreference = "Stop"
$apiVersion = "2025-09-01-preview"
$subscriptionApiVersion = "2022-08-01"
$baseUrl = "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.ApiManagement/service/$ApimName"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Invoke-AzRestPut([string]$Uri, [object]$Body) {
  $json = $Body | ConvertTo-Json -Depth 20 -Compress
  $bodyPath = Join-Path $env:TEMP ("opsdesk-apim-body-" + [guid]::NewGuid().ToString("N") + ".json")
  [System.IO.File]::WriteAllText($bodyPath, $json, [System.Text.UTF8Encoding]::new($false))
  az rest --method put --url $Uri --headers "If-Match=*" "Content-Type=application/json" --body "@$bodyPath" --only-show-errors -o none
  $exitCode = $LASTEXITCODE
  Remove-Item -LiteralPath $bodyPath -Force -ErrorAction SilentlyContinue
  if ($exitCode -ne 0) { throw "APIM update failed: $Uri" }
}

function Set-LocalSettings([string]$RelativePath, [object]$Settings) {
  $path = Join-Path $repoRoot $RelativePath
  $json = $Settings | ConvertTo-Json -Depth 10
  Set-Content -Path $path -Value $json -Encoding utf8
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

$product = az apim product show -g $ResourceGroup -n $ApimName --product-id demo-open --query name -o tsv 2>$null
if ($LASTEXITCODE -ne 0) {
  az apim product create -g $ResourceGroup -n $ApimName --product-id demo-open --product-name "OpsDesk Demo" `
    --description "Subscription-key access for the OpsDesk MCP demonstrations." --subscription-required true --state published --only-show-errors -o none
  if ($LASTEXITCODE -ne 0) { throw "Could not create APIM product 'demo-open'." }
}

Write-Host "Importing OpsDesk API with subscription-key enforcement..." -ForegroundColor Cyan
$specification = Invoke-RestMethod "$ApiUrl/openapi/v1.json"
$specification.openapi = "3.0.3"
$specification.PSObject.Properties.Remove("jsonSchemaDialect")
Convert-NullableTypes $specification
$specificationPath = Join-Path $env:TEMP "opsdesk-api-apim.openapi.json"
$specification | ConvertTo-Json -Depth 100 | Set-Content -Path $specificationPath -Encoding utf8
az apim api import -g $ResourceGroup -n $ApimName --api-id opsdesk-api --path opsdesk --display-name "OpsDesk API" `
  --protocols https --subscription-required true --specification-format OpenApiJson `
  --specification-path $specificationPath --service-url $ApiUrl --only-show-errors -o none
$importExitCode = $LASTEXITCODE
Remove-Item -LiteralPath $specificationPath -Force -ErrorAction SilentlyContinue
if ($importExitCode -ne 0) { throw "Could not import OpsDesk API into APIM." }

$apiProduct = az apim product api check -g $ResourceGroup -n $ApimName --product-id demo-open --api-id opsdesk-api -o tsv 2>$null
if ($LASTEXITCODE -ne 0 -or $apiProduct -ne "true") {
  az apim product api add -g $ResourceGroup -n $ApimName --product-id demo-open --api-id opsdesk-api --only-show-errors -o none
  if ($LASTEXITCODE -ne 0) { throw "Could not bind OpsDesk API to product 'demo-open'." }
}

$servers = @(
  @{ Id = "opsdesk-code"; Name = "OpsDesk endpoint tools"; Path = "opsdesk-code"; Backend = $McpCodeUrl },
  @{ Id = "opsdesk-tasks"; Name = "OpsDesk task tools"; Path = "opsdesk-tasks"; Backend = $McpTasksUrl }
)

foreach ($server in $servers) {
  $body = @{
    properties = @{
      type = "mcp"
      displayName = $server.Name
      description = "Streamable HTTP passthrough to the deployed OpsDesk MCP server."
      path = $server.Path
      protocols = @("https")
      serviceUrl = $server.Backend.TrimEnd("/")
      subscriptionRequired = $true
      mcpProperties = @{
        transportType = "streamable"
        endpoints = @{ message = @{ uriTemplate = "/mcp" } }
      }
    }
  }
  Invoke-AzRestPut "$baseUrl/apis/$($server.Id)?api-version=$apiVersion" $body

  $productBinding = az apim product api check -g $ResourceGroup -n $ApimName --product-id demo-open --api-id $server.Id -o tsv 2>$null
  if ($LASTEXITCODE -ne 0 -or $productBinding -ne "true") {
    az apim product api add -g $ResourceGroup -n $ApimName --product-id demo-open --api-id $server.Id --only-show-errors -o none
    if ($LASTEXITCODE -ne 0) { throw "Could not bind MCP server '$($server.Id)' to product 'demo-open'." }
  }
}

$apiMcpBody = @{
  properties = @{
    type = "mcp"
    displayName = "OpsDesk API tools"
    description = "MCP tools generated from the subscription-protected OpsDesk REST API."
    path = "opsdesk-apim"
    protocols = @("https")
    subscriptionRequired = $true
  }
}
Invoke-AzRestPut "$baseUrl/apis/opsdesk-apim?api-version=$apiVersion" $apiMcpBody

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
  Invoke-AzRestPut "$baseUrl/apis/opsdesk-apim/tools/${toolId}?api-version=$apiVersion" $toolBody
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

$foundry = @{ ProjectEndpoint = $FoundryProjectEndpoint; DeploymentName = $FoundryDeploymentName }
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
    Auth = "none"
    Entra = @{
      TenantId = ""
      Scope = "api://opsdesk-mcp/.default"
      Role = "Reader"
      Identities = @{
        Reader = @{ ClientId = ""; ClientSecret = "" }
        Operator = @{ ClientId = ""; ClientSecret = "" }
      }
    }
  }
}

Write-Host "APIM configured: REST API, endpoint-shaped MCP, task-shaped MCP, and API-backed MCP." -ForegroundColor Green
Write-Host "Product 'demo-open' requires a subscription key. The key was written only to ignored appsettings.Development.json files." -ForegroundColor Green
Write-Host "The Entra-protected opsdesk-secure server was not created; the resource app and role identities are not configured." -ForegroundColor Yellow
Write-Host "Created $toolCount API-backed MCP tools; Database/reset is intentionally excluded."
