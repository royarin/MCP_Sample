using './main.bicep'

param registry = '<acr-name>.azurecr.io'
param imageTag = 'latest'
param apimSku = 'StandardV2'
param apimPublisherEmail = '<you@example.com>'
