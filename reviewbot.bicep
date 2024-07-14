param botId string
param location string = resourceGroup().location

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'jio-reddit-logs'
  location: location
  properties: {
    sku: { name: 'pergb2018' }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: 1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'web'
  name: 'jio-reddit-appi'
  location: location
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    IngestionMode: 'LogAnalytics'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'jio-reddit-kv'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 30
  }
}

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: 'jio-flockbots-cdb'
}

var connectionStringSecretName = 'StoreConnectionString'
resource connectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: connectionStringSecretName
  properties: {
    value: cosmosDb.listConnectionStrings().connectionStrings[0].connectionString
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'jioredditst'
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'jio-reddit-asp'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'jio-reddit-func'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    reserved: true
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'jio-reddit-func'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: 'https://github.com/janssen-io/ReviewBot/releases/latest/download/functions.zip'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'Download__Location'
          value: 'https://docs.google.com/spreadsheets/d/1X1HTxkI6SqsdpNSkSSivMzpxNT-oeTbjFFDdEkXD30o/gviz/tq'
        }
        {
          name: 'ReviewBot__AppId'
          value: botId
        }
        {
          name: 'ReviewBot__AppSecret'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}/secrets/AppSecret)'
        }
        {
          name: 'ReviewBot__RefreshToken'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}/secrets/RefreshToken)'
        }
        {
          name: 'Store__ConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}/secrets/${connectionStringSecretName})'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

@description('This is the built-in Key Vault Secret User role. See https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#key-vault-secrets-user')
resource secretUserRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}

resource functionAppSecretUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, functionApp.id, secretUserRole.id)
  properties: {
    roleDefinitionId: secretUserRole.id
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
