param(
  [Parameter(Mandatory)][string]$TenantId,
  [ValidateRange(1, 180)][int]$SecretDays = 30,
  [switch]$RotateSecrets
)
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\local-settings.ps1"
$repoRoot = Split-Path -Parent $PSScriptRoot
$settingsPath = Join-Path $repoRoot "src\OpsBot.Enterprise\appsettings.Development.json"
$settings = if (Test-Path -LiteralPath $settingsPath) {
  Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
} else { $null }
$currentTenant = az account show --query tenantId -o tsv --only-show-errors
if ($LASTEXITCODE -ne 0 -or $currentTenant -ne $TenantId) { throw "Sign Azure CLI into the requested demo tenant first." }

function Get-OrCreateApp([string]$Name) {
  $apps = @(az ad app list --display-name $Name -o json --only-show-errors | ConvertFrom-Json |
    Where-Object displayName -eq $Name)
  if ($LASTEXITCODE -ne 0 -or $apps.Count -gt 1) { throw "Cannot uniquely resolve Entra app '$Name'." }
  if ($apps.Count -eq 1) { return $apps[0] }
  $app = az ad app create --display-name $Name --sign-in-audience AzureADMyOrg -o json --only-show-errors | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0) { throw "Could not create Entra app '$Name'." }
  return $app
}
function Get-OrCreatePrincipal([string]$AppId) {
  $list = Invoke-DemoRest "GET" "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId%20eq%20'$AppId'" $null
  if (@($list.value).Count -eq 1) { return $list.value[0] }
  return Invoke-DemoRest "POST" "https://graph.microsoft.com/v1.0/servicePrincipals" @{ appId = $AppId }
}

$resource = Get-OrCreateApp "app-opsdesk-mcp"
$roles = @($resource.appRoles)
$role = $roles | Where-Object value -eq "Ops.Agent"
if (-not $role) {
  $role = @{ id = [guid]::NewGuid().ToString(); allowedMemberTypes = @("Application"); displayName = "Ops agent"
    value = "Ops.Agent"; description = "Read curated OpsDesk MCP tools."; isEnabled = $true }
  $roles += $role
}
$uris = @($resource.identifierUris)
if ($uris -notcontains "api://opsdesk-mcp") { $uris += "api://opsdesk-mcp" }
$null = Invoke-DemoRest "PATCH" "https://graph.microsoft.com/v1.0/applications/$($resource.id)" @{
  appRoles = $roles; identifierUris = $uris; api = @{ requestedAccessTokenVersion = 2 }
}
$resourcePrincipal = Get-OrCreatePrincipal $resource.appId
$null = Invoke-DemoRest "PATCH" "https://graph.microsoft.com/v1.0/servicePrincipals/$($resourcePrincipal.id)" @{
  appRoleAssignmentRequired = $false
}
$identities = @{}
foreach ($entry in @(@{ name = "opsdesk-agent"; key = "Default" }, @{ name = "opsdesk-agent-norole"; key = "NoRole" })) {
  $app = Get-OrCreateApp $entry.name
  $principal = Get-OrCreatePrincipal $app.appId
  if ($entry.key -eq "Default") {
    $null = Invoke-DemoRest "PATCH" "https://graph.microsoft.com/v1.0/applications/$($app.id)" @{
      requiredResourceAccess = @(@{ resourceAppId = $resource.appId; resourceAccess = @(@{ id = $role.id; type = "Role" }) })
    }
    $assignments = Invoke-DemoRest "GET" "https://graph.microsoft.com/v1.0/servicePrincipals/$($principal.id)/appRoleAssignments" $null
    if (-not ($assignments.value | Where-Object { $_.resourceId -eq $resourcePrincipal.id -and $_.appRoleId -eq $role.id })) {
      $null = Invoke-DemoRest "POST" "https://graph.microsoft.com/v1.0/servicePrincipals/$($principal.id)/appRoleAssignments" @{
        principalId = $principal.id; resourceId = $resourcePrincipal.id; appRoleId = $role.id
      }
    }
  } else {
    $assignments = Invoke-DemoRest "GET" "https://graph.microsoft.com/v1.0/servicePrincipals/$($principal.id)/appRoleAssignments" $null
    if ($assignments.value | Where-Object resourceId -eq $resourcePrincipal.id) {
      throw "The no-role identity already has an assignment to the demo resource. Remove it explicitly before using this identity."
    }
  }
  $existing = $settings.Mcp.Entra.Identities.($entry.key)
  if (-not $RotateSecrets -and $existing.ClientId -eq $app.appId -and -not [string]::IsNullOrWhiteSpace($existing.ClientSecret)) {
    $secret = $existing.ClientSecret
  } else {
    $expires = (Get-Date).ToUniversalTime().AddDays($SecretDays).ToString("yyyy-MM-ddTHH:mm:ssZ")
    $credential = az ad app credential reset --id $app.appId --append --display-name opsdesk-local-demo `
      --end-date $expires -o json --only-show-errors | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($credential.password)) {
      throw "Could not provision local credentials for '$($entry.name)'."
    }
    $secret = $credential.password
  }
  $identities[$entry.key] = @{ ClientId = $app.appId; ClientSecret = $secret }
}
Update-LocalSettings $settingsPath @{
  Mcp = @{ Entra = @{ TenantId = $TenantId; ResourceAppId = $resource.appId
      Scope = "api://opsdesk-mcp/.default"; Identities = $identities } }
}
Write-Host "Entra demo resource and Default/NoRole identities configured. Secrets are only in ignored local settings." -ForegroundColor Green
