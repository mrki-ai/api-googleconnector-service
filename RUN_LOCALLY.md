# Running the Google Connector Service Locally

## Quick Verification

The solution has been created and verified with:
- ✅ No linter errors
- ✅ Proper project references
- ✅ Clean architecture structure
- ✅ All required files present

## Prerequisites

Before running, ensure you have:

```bash
# Check .NET SDK
dotnet --version
# Should show: 8.x.x

# Check Azure Functions Core Tools
func --version
# Should show: 4.x.x

# If func is not installed:
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

## Step-by-Step Local Run

### 1. Navigate to the Project

```bash
cd /Users/dapi.ai/mrki-ai/api-googleconnector-service
```

### 2. Restore Dependencies

```bash
dotnet restore
```

Expected output: "Restore succeeded" for all projects.

### 3. Build the Solution

```bash
dotnet build
```

Expected output: Build succeeded with no errors.

### 4. Run Tests (Optional but Recommended)

```bash
dotnet test
```

Expected output: All tests passing.

### 5. Configure Local Settings

The `local.settings.json` is already created in `src/FunctionApp/`. You need to update it with real credentials:

```bash
# Edit the file
code src/FunctionApp/local.settings.json
# or
nano src/FunctionApp/local.settings.json
```

**Minimal configuration for testing (without real APIs):**

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDb:ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "CosmosDb:DatabaseName": "GoogleConnectorDb",
    "CosmosDb:ReviewsContainerName": "Reviews",
    "CosmosDb:BusinessesContainerName": "Businesses",
    "Google:CredentialsJson": "{}",
    "KeyVault:Uri": ""
  }
}
```

### 6. Start CosmosDB Emulator (For Full Testing)

**Option A: Download CosmosDB Emulator (Windows/Mac with Docker)**

```bash
# Using Docker (works on Mac)
docker pull mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
docker run -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 \
  -m 3g --cpus=2.0 --name=cosmosdb-emulator \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator

# Access at: https://localhost:8081/_explorer/index.html
```

**Option B: Skip CosmosDB for initial testing**

The functions will start but database operations will fail. Good for verifying the function app starts correctly.

### 7. Run the Azure Functions

```bash
cd src/FunctionApp
func start
```

**Expected Output:**

```
Azure Functions Core Tools
Core Tools Version:       4.x.x
Function Runtime Version: 4.x.x

Functions:

  GetGoogleReviews: [GET] http://localhost:7071/api/reviews/{businessId}

  IngestGoogleReviews: [POST] http://localhost:7071/api/reviews/ingest

  IngestGoogleReviewsTimer: timerTrigger

For detailed output, run func with --verbose flag.
```

### 8. Test the Endpoints

Once running, open a new terminal:

**Test Health (GET):**
```bash
curl http://localhost:7071/api/reviews/test-business-id
```

**Test Ingestion (POST):**
```bash
curl -X POST http://localhost:7071/api/reviews/ingest \
  -H "Content-Type: application/json" \
  -d '{"businessId":"test-business-123"}'
```

## Common Issues and Solutions

### Issue: "Unable to find project.assets.json"

**Solution:**
```bash
dotnet restore
dotnet build
```

### Issue: "Functions host didn't start"

**Solution:**
```bash
# Check port 7071 is not in use
lsof -i :7071

# If in use, kill the process or use a different port
func start --port 7072
```

### Issue: "CosmosDB connection failed"

**Solution:**
This is expected if you haven't set up CosmosDB. The functions will start but database operations will fail. To fix:
- Use CosmosDB Emulator (Docker command above)
- Or use Azure CosmosDB and update connection string

### Issue: "Google API credentials error"

**Solution:**
For initial testing, the function will handle the error gracefully and return empty results. To fix:
- Get Google Cloud Service Account credentials
- Paste the JSON into `local.settings.json` under `Google:CredentialsJson`

## Verification Checklist

Run these commands to verify everything:

```bash
# 1. Check solution builds
cd /Users/dapi.ai/mrki-ai/api-googleconnector-service
dotnet build

# 2. Run tests
dotnet test

# 3. Check FunctionApp builds
cd src/FunctionApp
dotnet build

# 4. Start functions (requires func tools installed)
func start
```

## Expected Results

### Successful Build Output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Successful Test Output:
```
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4
```

### Successful Function Start:
```
Functions:
  GetGoogleReviews: [GET] http://localhost:7071/api/reviews/{businessId}
  IngestGoogleReviews: [POST] http://localhost:7071/api/reviews/ingest
  IngestGoogleReviewsTimer: timerTrigger
```

## Production-Ready Checklist

Before deploying to production:

- [ ] Set up real CosmosDB instance in Azure
- [ ] Get Google Business Profile API credentials
- [ ] Configure Azure Key Vault for secrets
- [ ] Implement actual Google API calls in `GoogleApiClient.cs`
- [ ] Add comprehensive error handling
- [ ] Set up Application Insights
- [ ] Configure CI/CD pipeline
- [ ] Add authentication to endpoints

## Next Steps

1. **Run the commands above** to verify the solution builds and runs
2. **Set up CosmosDB** (emulator or Azure)
3. **Get Google API credentials** for real data
4. **Implement the Google API client** in `src/Infrastructure/Persistence/GoogleApiClient.cs`
5. **Deploy to Azure** using the deployment guide in README.md

## Need Help?

- See [README.md](README.md) for full documentation
- See [QUICKSTART.md](QUICKSTART.md) for step-by-step setup
- Check [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines



