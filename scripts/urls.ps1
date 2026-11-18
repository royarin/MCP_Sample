# Deployed endpoints – fill after `az deployment group show` (or paste the Bicep outputs).
$script:ApiUrl   = "https://ca-opsdesk-api.<env>.azurecontainerapps.io"
$script:WebUrl   = "https://ca-opsdesk-web.<env>.azurecontainerapps.io"
$script:McpCode  = "https://ca-opsdesk-mcp.<env>.azurecontainerapps.io/mcp"
$script:McpTasks = "https://ca-opsdesk-mcp-tasks.<env>.azurecontainerapps.io/mcp"
$script:ApimMcp  = "https://<apim>.azure-api.net/opsdesk-apim/mcp"
$script:Secure   = "https://<apim>.azure-api.net/opsdesk-secure/mcp"
$script:Question = "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?"
$script:WriteAsk = "Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner."
