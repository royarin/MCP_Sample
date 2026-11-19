using './main.bicep'

param baseName = readEnvironmentVariable('OPSDESK_BASE_NAME', 'opsdesk-demo')
param uniqueSuffix = readEnvironmentVariable('OPSDESK_UNIQUE_SUFFIX', '')
param location = 'swedencentral'
param apiCenterLocation = 'eastus'
param apimSku = 'Developer'
param apimPublisherEmail = 'opsdesk-demo@example.com'
