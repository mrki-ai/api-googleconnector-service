# Quick Start Guide

Get the Google Connector Service up and running in 5 minutes!

## Prerequisites Check

```bash
# Check .NET version
dotnet --version  # Should be 8.x

# Check Azure Functions Core Tools
func --version    # Should be 4.x

# Optional: Check Node.js (if you need to install func tools)
node --version
```

If you're missing any prerequisites, see [README.md](README.md) for installation instructions.

## Step 1: Clone and Build

```bash
cd api-googleconnector-service
dotnet restore
dotnet build
```

## Step 2: Configure Local Settings

Copy the example settings:

```bash
cp local.settings.json.example src/FunctionApp/local.settings.json
```

Edit `src/FunctionApp/local.settings.json`:

### For CosmosDB Emulator (Easiest for local dev):

```json
{
  "Values": {
    "CosmosDb:ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```

### For Azure CosmosDB:

```bash
# Get your connection string
az cosmosdb keys list \
  --name YOUR_COSMOS_ACCOUNT \
  --resource-group YOUR_RG \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv

# Paste it into local.settings.json
```

## Step 3: Setup Database

### Option A: CosmosDB Emulator (Recommended for local dev)

1. Download and install [Azure Cosmos DB Emulator](https://aka.ms/cosmosdb-emulator)
2. Start the emulator
3. Open https://localhost:8081/_explorer/index.html
4. Create database: `GoogleConnectorDb`
5. Create containers:
   - `Reviews` with partition key `/id`
   - `Businesses` with partition key `/businessId`

### Option B: Azure CosmosDB

```bash
chmod +x scripts/setup-cosmosdb.sh
./scripts/setup-cosmosdb.sh
```

## Step 4: Configure Google API Credentials

You need Google Business Profile API credentials. Two options:

### Option A: Service Account (Recommended)

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (or select existing)
3. Enable "Google My Business API"
4. Create Service Account:
   - IAM & Admin → Service Accounts → Create Service Account
   - Grant necessary permissions
   - Create JSON key
5. Copy the entire JSON content into `local.settings.json`:

```json
{
  "Values": {
    "Google:CredentialsJson": "{\"type\":\"service_account\",\"project_id\":\"your-project\",...}"
  }
}
```

### Option B: Mock for Testing (Quick POC)

For initial testing without real Google API access, the service will return empty results but won't crash.

## Step 5: Run the Functions

```bash
cd src/FunctionApp
func start
```

You should see:

```
Functions:
  GetGoogleReviews: [GET] http://localhost:7071/api/reviews/{businessId}
  IngestGoogleReviews: [POST] http://localhost:7071/api/reviews/ingest
  IngestGoogleReviewsTimer: timerTrigger
```

## Step 6: Test the API

### Test Ingestion (POST):

```bash
curl -X POST http://localhost:7071/api/reviews/ingest \
  -H "Content-Type: application/json" \
  -d '{"businessId": "test-business-123"}'
```

Expected response:
```json
{
  "reviewsIngested": 0,
  "syncDate": "2025-10-16T10:30:00Z",
  "success": true,
  "errorMessage": null
}
```

### Test Retrieval (GET):

```bash
curl http://localhost:7071/api/reviews/test-business-123
```

Expected response:
```json
{
  "reviews": [],
  "totalCount": 0
}
```

## Step 7: Run Tests

```bash
# From project root
dotnet test

# Should see all tests passing
```

## Troubleshooting

### "CosmosDB connection failed"
- Make sure CosmosDB Emulator is running, or
- Verify your Azure CosmosDB connection string is correct

### "Google API error"
- Check that your credentials JSON is valid
- Ensure the Google My Business API is enabled in your Google Cloud project
- Verify your service account has the necessary permissions

### "Function host didn't start"
- Make sure port 7071 isn't already in use
- Check that Azure Functions Core Tools v4 is installed
- Verify .NET 8 SDK is installed

### "Build failed"
- Run `dotnet clean` then `dotnet restore`
- Make sure you're using .NET 8 SDK

## Next Steps

1. Review the [Architecture Documentation](README.md#architecture)
2. Add real Google Business Profile integration
3. Implement proper error handling and retry logic
4. Add authentication/authorization
5. Deploy to Azure (see [README.md](README.md#deployment-to-azure))

## Common Development Tasks

```bash
# Clean build
dotnet clean && dotnet build

# Run specific test
dotnet test --filter "FullyQualifiedName~IngestGoogleReviewsCommandHandlerTests"

# Run with verbose logging
cd src/FunctionApp
func start --verbose

# Deploy to Azure
func azure functionapp publish YOUR_FUNCTION_APP_NAME
```

## Support

- See [README.md](README.md) for detailed documentation
- See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines
- Check Azure Functions documentation: https://docs.microsoft.com/azure/azure-functions/



