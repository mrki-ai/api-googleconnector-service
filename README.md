# Google Connector Service

A .NET 8 microservice for retrieving and processing Google Business Profile reviews using Azure Functions, built with Onion Architecture and CQRS pattern.

## 🚀 Quick Start

```bash
# Verify everything is set up correctly
./verify-setup.sh

# Start the service locally
./start-local.sh
```

See [RUN_LOCALLY.md](RUN_LOCALLY.md) for detailed local development instructions.

## Architecture

This solution follows Clean Architecture (Onion Architecture) principles with clear separation of concerns:

```
src/
├── Core/
│   ├── Domain/           # Domain entities (GoogleReview, GoogleBusiness)
│   └── Application/      # Business logic, CQRS commands/queries, interfaces
├── Infrastructure/
│   └── Persistence/      # CosmosDB repositories, Google API client
└── FunctionApp/          # Azure Functions HTTP triggers and timers
```

### Layers

- **Domain Layer**: Pure domain entities with no dependencies
- **Application Layer**: Business logic, MediatR commands/queries, repository interfaces
- **Infrastructure Layer**: External dependencies (CosmosDB, Google API, Key Vault)
- **FunctionApp**: Azure Functions entry points (HTTP triggers, timer triggers)

## Features

- **IngestGoogleReviews**: Command to fetch and store reviews from Google Business Profile API
- **GetGoogleReviews**: Query to retrieve stored reviews with pagination
- **Scheduled Sync**: Timer-triggered function to periodically sync reviews (every 6 hours)
- **CosmosDB Storage**: Persist reviews and business information
- **Key Vault Integration**: Secure secret management
- **CQRS Pattern**: Separate read and write operations using MediatR

## Prerequisites

Before running this project, ensure you have:

1. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Azure Functions Core Tools v4** - Install via:
   ```bash
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```
3. **Azure Cosmos DB** - Create a Cosmos DB account or use the [Cosmos DB Emulator](https://docs.microsoft.com/azure/cosmos-db/local-emulator)
4. **Google API Credentials**:
   - Create a Google Cloud Project
   - Enable the Google My Business API
   - Create a Service Account and download credentials JSON
   - Or set up OAuth2 credentials

## Local Development Setup

### 1. Clone and Restore

```bash
cd api-googleconnector-service
dotnet restore
```

### 2. Configure Local Settings

Update `src/FunctionApp/local.settings.json` with your credentials:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDb:ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=YOUR_KEY",
    "CosmosDb:DatabaseName": "GoogleConnectorDb",
    "CosmosDb:ReviewsContainerName": "Reviews",
    "CosmosDb:BusinessesContainerName": "Businesses",
    "Google:CredentialsJson": "{\"type\":\"service_account\",\"project_id\":\"...\"}",
    "KeyVault:Uri": "https://your-keyvault.vault.azure.net/"
  }
}
```

### 3. Create Cosmos DB Database and Containers

If using Cosmos DB Emulator or Azure Cosmos DB, create:

- **Database**: `GoogleConnectorDb`
- **Container**: `Reviews` (Partition Key: `/id`)
- **Container**: `Businesses` (Partition Key: `/businessId`)

You can use the Azure portal or this Azure CLI command:

```bash
# Create database
az cosmosdb sql database create \
  --account-name YOUR_ACCOUNT \
  --name GoogleConnectorDb \
  --resource-group YOUR_RG

# Create Reviews container
az cosmosdb sql container create \
  --account-name YOUR_ACCOUNT \
  --database-name GoogleConnectorDb \
  --name Reviews \
  --partition-key-path "/id" \
  --resource-group YOUR_RG

# Create Businesses container
az cosmosdb sql container create \
  --account-name YOUR_ACCOUNT \
  --database-name GoogleConnectorDb \
  --name Businesses \
  --partition-key-path "/businessId" \
  --resource-group YOUR_RG
```

### 4. Run Locally

```bash
cd src/FunctionApp
func start
```

The Functions will be available at:
- `http://localhost:7071/api/reviews/ingest` (POST)
- `http://localhost:7071/api/reviews/{businessId}` (GET)

### 5. Test the API

**Ingest Reviews:**
```bash
curl -X POST http://localhost:7071/api/reviews/ingest \
  -H "Content-Type: application/json" \
  -d '{"businessId": "your-business-id"}'
```

**Get Reviews:**
```bash
curl http://localhost:7071/api/reviews/your-business-id?skip=0&take=10
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/UnitTests/UnitTests.csproj

# Run integration tests only
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```

## Deployment to Azure

### 1. Create Azure Resources

```bash
# Variables
RG="rg-googleconnector"
LOCATION="eastus"
STORAGE="stgoogleconnector"
FUNCTIONAPP="func-googleconnector"
COSMOSDB="cosmos-googleconnector"

# Create resource group
az group create --name $RG --location $LOCATION

# Create storage account
az storage account create \
  --name $STORAGE \
  --resource-group $RG \
  --location $LOCATION \
  --sku Standard_LRS

# Create Cosmos DB account
az cosmosdb create \
  --name $COSMOSDB \
  --resource-group $RG \
  --locations regionName=$LOCATION

# Create Function App
az functionapp create \
  --name $FUNCTIONAPP \
  --resource-group $RG \
  --storage-account $STORAGE \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4
```

### 2. Configure Application Settings

```bash
# Get Cosmos DB connection string
COSMOS_CONN=$(az cosmosdb keys list \
  --name $COSMOSDB \
  --resource-group $RG \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv)

# Set app settings
az functionapp config appsettings set \
  --name $FUNCTIONAPP \
  --resource-group $RG \
  --settings \
    "CosmosDb:ConnectionString=$COSMOS_CONN" \
    "CosmosDb:DatabaseName=GoogleConnectorDb" \
    "CosmosDb:ReviewsContainerName=Reviews" \
    "CosmosDb:BusinessesContainerName=Businesses" \
    "Google:CredentialsJson=@path/to/credentials.json"
```

### 3. Deploy the Function App

```bash
cd src/FunctionApp
func azure functionapp publish $FUNCTIONAPP
```

## Project Structure

```
api-googleconnector-service/
├── google-connector-service.sln
├── README.md
├── src/
│   ├── Core/
│   │   ├── Domain/
│   │   │   ├── Domain.csproj
│   │   │   ├── GoogleReview.cs
│   │   │   └── GoogleBusiness.cs
│   │   └── Application/
│   │       ├── Application.csproj
│   │       ├── Commands/
│   │       │   └── IngestGoogleReviewsCommand.cs
│   │       ├── Queries/
│   │       │   └── GetGoogleReviewsQuery.cs
│   │       ├── IGoogleReviewRepository.cs
│   │       ├── IGoogleBusinessRepository.cs
│   │       └── IGoogleApiClient.cs
│   ├── Infrastructure/
│   │   └── Persistence/
│   │       ├── Persistence.csproj
│   │       ├── GoogleReviewRepository.cs
│   │       ├── GoogleBusinessRepository.cs
│   │       ├── GoogleApiClient.cs
│   │       └── KeyVaultSecretProvider.cs
│   └── FunctionApp/
│       ├── FunctionApp.csproj
│       ├── Program.cs
│       ├── host.json
│       ├── local.settings.json
│       └── Functions/
│           ├── IngestReviewsFunction.cs
│           └── GetReviewsFunction.cs
└── tests/
    ├── UnitTests/
    │   ├── UnitTests.csproj
    │   ├── Commands/
    │   │   └── IngestGoogleReviewsCommandHandlerTests.cs
    │   └── Queries/
    │       └── GetGoogleReviewsQueryHandlerTests.cs
    └── IntegrationTests/
        ├── IntegrationTests.csproj
        └── FunctionTests.cs
```

## Technologies Used

- **.NET 8** - Modern framework
- **Azure Functions v4** - Serverless compute
- **MediatR** - CQRS implementation
- **Azure Cosmos DB** - NoSQL database
- **Google My Business API** - Review data source
- **Azure Key Vault** - Secret management
- **xUnit & Moq** - Testing framework

## API Endpoints

### POST /api/reviews/ingest
Ingests reviews for a specific business from Google Business Profile API.

**Request:**
```json
{
  "businessId": "your-google-business-id"
}
```

**Response:**
```json
{
  "reviewsIngested": 15,
  "syncDate": "2025-10-16T10:30:00Z",
  "success": true,
  "errorMessage": null
}
```

### GET /api/reviews/{businessId}
Retrieves stored reviews for a business with pagination.

**Query Parameters:**
- `skip` (optional): Number of records to skip (default: 0)
- `take` (optional): Number of records to return (default: 100)

**Response:**
```json
{
  "reviews": [
    {
      "id": "review-123",
      "reviewerName": "John Doe",
      "rating": 5,
      "comment": "Excellent service!",
      "reviewDate": "2025-10-15T14:30:00Z",
      "reply": "Thank you!",
      "businessId": "business-456",
      "createdAt": "2025-10-16T10:30:00Z",
      "updatedAt": null
    }
  ],
  "totalCount": 1
}
```

## License

MIT

