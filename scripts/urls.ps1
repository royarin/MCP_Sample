# Deployed endpoints – fill after `az deployment group show` (or paste the Bicep outputs).
$script:ApiUrl   = "https://app-<base-name>-api.azurewebsites.net"
$script:WebUrl   = "https://app-<base-name>-web.azurewebsites.net"
$script:McpCode  = "https://app-<base-name>-mcp.azurewebsites.net/mcp"
$script:McpTasks = "https://app-<base-name>-mcp-tasks.azurewebsites.net/mcp"
$script:ApimMcp  = "https://<apim-name>.azure-api.net/opsdesk-apim/mcp"
$script:Secure   = "https://<apim-name>.azure-api.net/opsdesk-secure/mcp"
$script:Question = "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?"
$script:WriteAsk = "Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner."
