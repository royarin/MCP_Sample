# Builds and pushes the four images to ACR (run from the repo root). Requires: az login, docker.
param([Parameter(Mandatory)][string]$Acr, [string]$Tag = "latest")
az acr login --name $Acr | Out-Null
$images = @{
  "opsdesk-api"       = "src/OpsDesk.Api/Dockerfile"
  "opsdesk-web"       = "src/OpsDesk.Web/Dockerfile"
  "opsdesk-mcp"       = "src/OpsDesk.Mcp/Dockerfile"
  "opsdesk-mcp-tasks" = "src/OpsDesk.Mcp.Tasks/Dockerfile"
}
foreach ($name in $images.Keys) {
  $ref = "$Acr.azurecr.io/$name`:$Tag"
  Write-Host "building $ref" -ForegroundColor Cyan
  docker build -f $images[$name] -t $ref . ; if ($LASTEXITCODE) { throw "build failed: $name" }
  docker push $ref ; if ($LASTEXITCODE) { throw "push failed: $name" }
}
