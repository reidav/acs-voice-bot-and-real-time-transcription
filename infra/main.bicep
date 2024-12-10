targetScope = 'subscription'

@description('Name of the environment used to generate a short unique hash for resources.')
@minLength(1)
@maxLength(64)
param environmentName string

@description('Primary location for all resources')
@allowed([
    'eastus2'
    'swedencentral'
  ])
param location string

@description('Name of the resource group')
param resourceGroupName string = ''

/* Azure communication resource details */
@description('Name of the Azure Communication service')
param acsServiceName string = ''

@description('Name of the resource group for the Azure Communication service')
param acsServiceResourceGroupName string = ''

/* Azure AI Multi service resource details */

@description('Name of the resource group for the AI Multi Service')
param aiServiceResourceGroupName string = ''

@description('Name of the AI Multi Service')
param aiServiceName string = ''

@description('SKU name for the AI Multi Service. Default: S0')
param aiServiceSkuName string = 'S0'

/* Azure search service resource details */

@description('Name of the search index. Default: solar-index')
param searchIndexName string = 'solar-index'

@description('Name of the Azure Cognitive Search service')
param searchServiceName string = ''

@description('Name of the resource group for the Azure Cognitive Search service')
param searchServiceResourceGroupName string = ''

@description('SKU name for the Azure Cognitive Search service. Default: standard')
param searchServiceSkuName string = 'standard'

/* Azure storage service resource details */

@description('Name of the storage account')
param storageAccountName string = ''

@description('Name of the storage container. Default: content')
param storageContainerName string = 'content'

@description('Location of the resource group for the storage account')
param storageResourceGroupLocation string = location

@description('Name of the resource group for the storage account')
param storageResourceGroupName string = ''

/* Azure openai service resource details */

@description('Capacity of the gpt4o-realtime deployment. Default: 3')
param gpt4oRealtimeDeploymentCapacity int = 3

@description('Name of the gpt4o-realtime  deployment')
param gpt4oRealtimeDeploymentName string = 'gpt-4o-realtime-deployment'

@description('Name of the gpt4o-realtime  model. Default: gpt-4o-realtime-preview')
param gpt4oRealtimeModelName string = 'gpt-4o-realtime-preview'

@description('Capacity of the gpt-4o deployment. Default: 30')
param gpt4oDeploymentCapacity int = 30

@description('Name of the gpt-4o deployment')
param gpt4oDeploymentName string = 'gpt-4o-deployment'

@description('Name of the gpt-4o  model. Default: gpt-4o')
param gpt4oModelName string = 'gpt-4o'

@description('Location of the resource group for the OpenAI resources')
param openAiResourceGroupLocation string = location

@description('Name of the resource group for the OpenAI resources')
param openAiResourceGroupName string = ''

@description('Name of the OpenAI service')
param openAiServiceName string = ''

@description('SKU name for the OpenAI service. Default: S0')
param openAiSkuName string = 'S0'

/* Azure app service resource details */

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Organize resources in a resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
    name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
    location: location
}

resource communicationServiceResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(acsServiceResourceGroupName)) {
    name: !empty(acsServiceResourceGroupName) ? acsServiceResourceGroupName : resourceGroup.name
}

resource cognitiveServiceResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(aiServiceResourceGroupName)) {
    name: !empty(aiServiceResourceGroupName) ? aiServiceResourceGroupName : resourceGroup.name
}

resource searchServiceResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(searchServiceResourceGroupName)) {
    name: !empty(searchServiceResourceGroupName) ? searchServiceResourceGroupName : resourceGroup.name
}

resource storageResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(storageResourceGroupName)) {
    name: !empty(storageResourceGroupName) ? storageResourceGroupName : resourceGroup.name
}

resource openAiResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(openAiResourceGroupName)) {
    name: !empty(openAiResourceGroupName) ? openAiResourceGroupName : resourceGroup.name
}

module communicationServive 'core/communication-services.bicep' = {
    name: 'communicationservice'
    scope: communicationServiceResourceGroup
    params: {
        communicationServiceName: !empty(acsServiceName) ? acsServiceName : '${abbrs.communicationServiceAccounts}${resourceToken}'
    }
}

// Not all region are supported for the cognitive service integration with azure communication service
// https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/azure-communication-services-azure-cognitive-services-integration#azure-ai-services-regions-supported
// uksouth enforced for the cognitive service
module cognitiveMultiService 'core/cognitive-services.bicep' = {
    name: 'cognitive-service'
    scope: cognitiveServiceResourceGroup
    params: {
        name: !empty(aiServiceName) ? aiServiceName : '${abbrs.cognitiveMultiServiceAccount}${resourceToken}'
        kind: 'CognitiveServices'
        location: 'uksouth'
        sku: {
            name: aiServiceSkuName
        }
    }
}

// Not all region are supported for azure search service using semantic search
// https://learn.microsoft.com/en-us/azure/search/search-region-support#europe
// francecentral enforced for the search service
module searchService 'core/search-services.bicep' = {
    name: 'search-service'
    scope: searchServiceResourceGroup
    params: {
        name: !empty(searchServiceName) ? searchServiceName : '${abbrs.searchSearchServices}${resourceToken}'
        location: 'francecentral'
        authOptions: {
            aadOrApiKey: {
                aadAuthFailureMode: 'http401WithBearerChallenge'
            }
        }
        sku: {
            name: searchServiceSkuName
        }
        semanticSearch: 'free'
    }
}

module storage 'core/storage-account.bicep' = {
    name: 'storage'
    scope: storageResourceGroup
    params: {
        name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'
        location: storageResourceGroupLocation
        publicNetworkAccess: 'Enabled'
        sku: {
            name: 'Standard_ZRS'
        }
    }
}

module openAi 'core/cognitive-services.bicep' = {
   name: 'openai'
   scope: openAiResourceGroup
    params: {
        name: !empty(openAiServiceName) ? openAiServiceName : '${abbrs.openai}${resourceToken}'
        location: openAiResourceGroupLocation
        sku: {
            name: openAiSkuName
        }
        deployments: [
            {
                name: gpt4oRealtimeDeploymentName
                model: {
                    format: 'OpenAI'
                    name: gpt4oRealtimeModelName
                    version: '2024-10-01'
                }
                sku: {
                    name: 'GlobalStandard'
                    capacity: gpt4oRealtimeDeploymentCapacity
                }
            }
            {
                name: gpt4oDeploymentName
                model: {
                    format: 'OpenAI'
                    name: gpt4oModelName
                    version: '2024-08-06'
                }
                sku: {
                    name: 'DataZoneStandard'
                    capacity: gpt4oDeploymentCapacity
                }
            }
        ]
    }
}

output AZURE_LOCATION string = location
output AZURE_OPENAI_GPT4O_RT_DEPLOYMENT_NAME string = gpt4oRealtimeDeploymentName
output AZURE_OPENAI_GPT4O_DEPLOYMENT_NAME string = gpt4oDeploymentName
output AZURE_OPENAI_ENDPOINT string = openAi.outputs.endpoint
output AZURE_OPENAI_KEY string = openAi.outputs.key

output AZURE_OPENAI_SERVICE string = openAi.outputs.name
output AZURE_RESOURCE_GROUP string = resourceGroup.name

output AZURE_SEARCH_INDEX string = searchIndexName
output AZURE_SEARCH_SERVICE_ENDPOINT string = searchService.outputs.endpoint
output AZURE_SEARCH_SERVICE_KEY string = searchService.outputs.key

output AZURE_STORAGE_ACCOUNT_CONNECTIONSTRING string = storage.outputs.blobStorageConnectionString
output AZURE_STORAGE_CONTAINER string = storageContainerName

output AZURE_AI_SERVICE_ENDPOINT string = cognitiveMultiService.outputs.endpoint
output AZURE_AI_SERVICE_KEY string = cognitiveMultiService.outputs.key

output ACS_CONNECTIONSTRING string = communicationServive.outputs.primaryConnectionString
output ACS_ENDPOINT string = communicationServive.outputs.endpoint
output ACS_SERVICE_NAME string = communicationServive.outputs.communicationServiceName

