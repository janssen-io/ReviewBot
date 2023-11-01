@description('Cosmos DB account name')
param accountName string = 'janssenio-flockbots-cdb'

@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

@description('The name for the SQL API database')
param databaseName string

@description('The name for the SQL API container')
param containerName string

resource account 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' = {
  name: toLower(accountName)
  location: location
  properties: {
    enableFreeTier: true
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
      }
    ]
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2023-04-15' = {
  parent: account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
    options: {
      throughput: 1000
    }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2023-04-15' = {
  parent: database
  name: containerName
  properties: {
    resource: {
      id: containerName
      indexes: [
        {
          key: {
            keys: [
              'author'
              'link'
              'bottle'
              'region'
            ]
          }
        }
      ]
    }
  }
}
