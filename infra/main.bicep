// OpsDesk MCP demo environment – Builders Foundry London
// az deployment group create -g rg-opsdesk-mcp -f infra/main.bicep -p infra/main.bicepparam
// Creates: Log Analytics, App Insights, Container Apps env + 4 apps, APIM, API Center.
// Entra app registrations are manual (infra/entra.md); APIM MCP servers + policies via infra/postdeploy.ps1.

@description('Azure region')
param location string = resourceGroup().location
@description('Short name used in resource names')
param baseName string = 'opsdesk-mcp'
@description('Container registry login server (ACR) that holds the four images')
param registry string
@description('Image tag')
param imageTag string = 'latest'
@description('APIM tier – must support MCP servers (verify at deployment time)')
@allowed(['BasicV2', 'StandardV2', 'PremiumV2', 'Developer', 'Premium'])
param apimSku string = 'StandardV2'
param apimPublisherEmail string
param apimPublisherName string = 'OpsDesk demo'
@description('Keep one replica warm on demo day')
param minReplicas int = 1

var law = 'log-${baseName}'
var appi = 'appi-${baseName}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: law
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appi
  location: location
  kind: 'web'
  properties: { Application_Type: 'web', WorkspaceResourceId: logAnalytics.id }
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${baseName}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// one module-less loop for the four apps; env vars wired to each other's FQDNs where needed
var apps = [
  { name: 'ca-opsdesk-api',       image: 'opsdesk-api',       port: 8080, env: [] }
  { name: 'ca-opsdesk-web',       image: 'opsdesk-web',       port: 8080, env: [ { name: 'ApiBaseUrl',  value: 'https://ca-opsdesk-api.${env.properties.defaultDomain}' } ] }
  { name: 'ca-opsdesk-mcp',       image: 'opsdesk-mcp',       port: 8080, env: [ { name: 'Api__BaseUrl', value: 'https://ca-opsdesk-api.${env.properties.defaultDomain}' } ] }
  { name: 'ca-opsdesk-mcp-tasks', image: 'opsdesk-mcp-tasks', port: 8080, env: [ { name: 'Api__BaseUrl', value: 'https://ca-opsdesk-api.${env.properties.defaultDomain}' } ] }
]

resource containerApps 'Microsoft.App/containerApps@2024-03-01' = [for app in apps: {
  name: app.name
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: { external: true, targetPort: app.port, transport: 'auto' }
    }
    template: {
      containers: [
        {
          name: app.image
          image: '${registry}/${app.image}:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat(app.env, [
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
            { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
          ])
        }
      ]
      scale: { minReplicas: minReplicas, maxReplicas: 2 }
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
    resourceId: appInsights.id
    credentials: { instrumentationKey: appInsights.properties.InstrumentationKey }
  }
}

resource apimNamedValues 'Microsoft.ApiManagement/service/namedValues@2024-05-01' = {
  parent: apim
  name: 'opsdesk-api-url'
  properties: { displayName: 'opsdesk-api-url', value: 'https://ca-opsdesk-api.${env.properties.defaultDomain}' }
}

resource apiCenter 'Microsoft.ApiCenter/services@2024-03-01' = {
  name: 'apic-${baseName}'
  location: location
  properties: {}
}

output apiUrl string = 'https://ca-opsdesk-api.${env.properties.defaultDomain}'
output webUrl string = 'https://ca-opsdesk-web.${env.properties.defaultDomain}'
output mcpUrl string = 'https://ca-opsdesk-mcp.${env.properties.defaultDomain}/mcp'
output mcpTasksUrl string = 'https://ca-opsdesk-mcp-tasks.${env.properties.defaultDomain}/mcp'
output apimGateway string = apim.properties.gatewayUrl
output appInsightsConnectionString string = appInsights.properties.ConnectionString
