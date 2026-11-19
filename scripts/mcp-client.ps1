function Get-EnterpriseToken([object]$Settings, [string]$Identity = "Default") {
  $principal = $Settings.Mcp.Entra.Identities.$Identity
  if ([string]::IsNullOrWhiteSpace($principal.ClientSecret)) { throw "Configure the $Identity enterprise identity first." }
  $response = Invoke-RestMethod -Method Post `
    -Uri "https://login.microsoftonline.com/$($Settings.Mcp.Entra.TenantId)/oauth2/v2.0/token" `
    -Body @{ grant_type = "client_credentials"; client_id = $principal.ClientId
      client_secret = $principal.ClientSecret; scope = $Settings.Mcp.Entra.Scope }
  if ([string]::IsNullOrWhiteSpace($response.access_token)) { throw "Entra returned no access token." }
  return $response.access_token
}

function New-McpProbe([string]$Endpoint, [string]$SubscriptionKey, [string]$Token) {
  $headers = @{ Accept = "application/json, text/event-stream" }
  if ($SubscriptionKey) { $headers["Ocp-Apim-Subscription-Key"] = $SubscriptionKey }
  if ($Token) { $headers.Authorization = "Bearer $Token" }
  return [pscustomobject]@{ Endpoint = $Endpoint; Headers = $headers; NextId = 1 }
}

function Invoke-McpRpc([object]$Client, [string]$Method, [object]$Params, [switch]$Notification, [switch]$AllowHttpError) {
  $request = @{ jsonrpc = "2.0"; method = $Method }
  if (-not $Notification) { $request.id = $Client.NextId; $Client.NextId++ }
  if ($null -ne $Params) { $request.params = $Params }
  $response = Invoke-WebRequest -Method Post -Uri $Client.Endpoint -Headers $Client.Headers `
    -ContentType "application/json" -Body ($request | ConvertTo-Json -Depth 30 -Compress) -SkipHttpErrorCheck
  if (-not $AllowHttpError -and [int]$response.StatusCode -ge 400) {
    throw "MCP $Method returned HTTP $([int]$response.StatusCode): $($response.Content)"
  }
  $json = $null
  if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
    $content = $response.Content.Trim()
    if ($content.StartsWith("{")) {
      $json = $content | ConvertFrom-Json
    } else {
      foreach ($line in ($content -split "\r?\n")) {
        if ($line.StartsWith("data:")) {
          $candidate = $line.Substring(5).Trim()
          if ($candidate.StartsWith("{")) { $json = $candidate | ConvertFrom-Json }
        }
      }
      if ($null -eq $json) { throw "MCP returned neither a JSON response nor a JSON SSE event." }
    }
  }
  if ($response.Headers.ContainsKey("Mcp-Session-Id")) {
    $Client.Headers["Mcp-Session-Id"] = $response.Headers["Mcp-Session-Id"][0]
  }
  if ($Method -eq "initialize" -and $json.result.protocolVersion) {
    $Client.Headers["MCP-Protocol-Version"] = $json.result.protocolVersion
  }
  return [pscustomobject]@{ Status = [int]$response.StatusCode; Json = $json; RequestId = $request.id }
}

function Initialize-McpProbe([object]$Client) {
  $response = Invoke-McpRpc $Client "initialize" @{
    protocolVersion = "2025-11-25"; capabilities = @{}
    clientInfo = @{ name = "opsdesk-rehearsal"; version = "1.0" }
  }
  if ($response.Json.error) { throw "MCP initialization failed: $($response.Json.error.message)" }
  $null = Invoke-McpRpc $Client "notifications/initialized" $null -Notification
}
