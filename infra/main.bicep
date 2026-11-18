// OpsDesk MCP demo environment – App Service layout for a caller-selected Azure resource group.
// Deploy with a caller-supplied subscription ID and resource group; see README.md.
// Creates: two Log Analytics/App Insights pairs, one Linux App Service plan + four apps,
// APIM, and API Center. Entra app registrations are manual (infra/entra.md);
// APIM MCP servers + policies are configured by infra/postdeploy.ps1.

@description('Azure region')
param location string = resourceGroup().location
@description('Region for Azure API Center, matching the source environment')
param apiCenterLocation string = 'eastus'
@description('Short name used in resource names')
param baseName string = 'opsdesk-demo'
@description('APIM tier; Developer matches the source demo environment')
@allowed(['Developer', 'StandardV2', 'PremiumV2', 'Premium'])
param apimSku string = 'Developer'
param apimPublisherEmail string
param apimPublisherName string = 'OpsDesk demo'

var appsWorkspaceName = 'la-${baseName}-apps'
var mcpWorkspaceName = 'la-${baseName}-mcp'
var appsInsightsName = 'appi-${baseName}-apps'
var mcpInsightsName = 'appi-${baseName}-mcp'
var apiAppName = 'app-${baseName}-api'
var webAppName = 'app-${baseName}-web'
var mcpAppName = 'app-${baseName}-mcp'
var mcpTasksAppName = 'app-${baseName}-mcp-tasks'
var apiEndpoint = 'https://${apiAppName}.azurewebsites.net'
var webEndpoint = 'https://${webAppName}.azurewebsites.net'
var mcpEndpoint = 'https://${mcpAppName}.azurewebsites.net/mcp'
var mcpTasksEndpoint = 'https://${mcpTasksAppName}.azurewebsites.net/mcp'

resource appsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: appsWorkspaceName
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource mcpWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: mcpWorkspaceName
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource appsInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appsInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: appsWorkspace.id
    RetentionInDays: 90
  }
}

resource mcpInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: mcpInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: mcpWorkspace.id
    RetentionInDays: 90
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: 'plan-${baseName}'
  location: location
  kind: 'linux'
  sku: {
    name: 'P0v3'
    tier: 'Premium0V3'
    capacity: 1
  }
  properties: { reserved: true }
}

var apps = [
  { name: apiAppName, insights: 'apps', settings: [] }
  { name: webAppName, insights: 'apps', settings: [{ name: 'ApiBaseUrl', value: apiEndpoint }] }
  { name: mcpAppName, insights: 'mcp', settings: [{ name: 'Api__BaseUrl', value: apiEndpoint }] }
  { name: mcpTasksAppName, insights: 'mcp', settings: [{ name: 'Api__BaseUrl', value: apiEndpoint }] }
]

resource webApps 'Microsoft.Web/sites@2025-03-01' = [for app in apps: {
  name: app.name
  location: location
  kind: 'app,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: concat(app.settings, [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: app.insights == 'apps' ? appsInsights.properties.ConnectionString : mcpInsights.properties.ConnectionString
        }
      ])
    }
  }
}]

resource apim 'Microsoft.ApiManagement/service@2024-05-01' = {
  name: 'apim-${baseName}'
  location: location
  sku: { name: apimSku, capacity: 1 }
  properties: { publisherEmail: apimPublisherEmail, publisherName: apimPublisherName }
}

resource apimLogger 'Microsoft.ApiManagement/service/loggers@2024-05-01' = {
  parent: apim
  name: 'appinsights'
  properties: {
    loggerType: 'applicationInsights'
    resourceId: mcpInsights.id
    credentials: { instrumentationKey: mcpInsights.properties.InstrumentationKey }
  }
}

resource apimNamedValues 'Microsoft.ApiManagement/service/namedValues@2024-05-01' = {
  parent: apim
  name: 'opsdesk-api-url'
  properties: { displayName: 'opsdesk-api-url', value: apiEndpoint }
}

resource apiCenter 'Microsoft.ApiCenter/services@2024-03-01' = {
  name: 'apic-${baseName}'
  location: apiCenterLocation
  properties: {}
}

output apiUrl string = apiEndpoint
output webUrl string = webEndpoint
output mcpUrl string = mcpEndpoint
output mcpTasksUrl string = mcpTasksEndpoint
output apimGateway string = apim.properties.gatewayUrl
output appInsightsConnectionString string = mcpInsights.properties.ConnectionString
output apiAppName string = apiAppName
output webAppName string = webAppName
output mcpAppName string = mcpAppName
output mcpTasksAppName string = mcpTasksAppName
