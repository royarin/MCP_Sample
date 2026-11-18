# Delegated tokens for Copilot's ${input:token}: sign in as the reader or operator demo user first (az login).
param([ValidateSet("reader","operator")][string]$As = "reader")
Write-Host "Token for $As (paste into VS Code when opsdesk-secure asks):" -ForegroundColor Cyan
az account get-access-token --scope "api://opsdesk-mcp/.default" --query accessToken -o tsv
