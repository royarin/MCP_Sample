# Demo 2 · From existing API to MCP server – two ways. Copilot is the host; this script covers the Inspector + console parts.
. "$PSScriptRoot/urls.ps1"
Write-Host "`nSwagger:   $ApiUrl/openapi/v1.json" -ForegroundColor DarkGray
Write-Host "Dashboard: $WebUrl" -ForegroundColor DarkGray
Write-Host "Inspector: npx @modelcontextprotocol/inspector  → $McpCode" -ForegroundColor DarkGray
Write-Host "`n[2a] Copilot chat → opsdesk-code (mcp.json) → `"$Question`"  then the write action → approval dialog → dashboard" -ForegroundColor Cyan
Write-Host "[2c] Copilot chat → opsdesk-apim → same question" -ForegroundColor Cyan
Write-Host "[2d] Inspector side by side: $ApimMcp vs $McpCode – generated vs hand-written descriptions" -ForegroundColor Cyan
