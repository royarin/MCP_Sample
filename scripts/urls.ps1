param(
  [Parameter(Mandatory)][string]$BaseName
)

$apiHost = "https://app-$BaseName-api.azurewebsites.net"
$webHost = "https://app-$BaseName-web.azurewebsites.net"
$mcpHost = "https://app-$BaseName-mcp.azurewebsites.net"
$mcpTasksHost = "https://app-$BaseName-mcp-tasks.azurewebsites.net"
$apimHost = "https://apim-$BaseName.azure-api.net"
$script:ApiDirectUrl = $apiHost
$script:ApiUrl = "$apimHost/opsdesk/"
$script:WebUrl = $webHost
$script:McpCodeDirect = "$mcpHost/mcp"
$script:McpTasksDirect = "$mcpTasksHost/mcp"
$script:McpCode = "$apimHost/opsdesk-code/mcp"
$script:McpTasks = "$apimHost/opsdesk-tasks/mcp"
$script:ApimMcp = "$apimHost/opsdesk-apim/mcp"
$script:Secure = "$apimHost/opsdesk-secure/mcp"
$script:Question = "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?"
$script:WriteAsk = "Raise a Critical incident for inventory - replication lag over threshold - reported by Arin, and notify the owner."
