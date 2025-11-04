#!/bin/bash

# Script to create CosmosDB database and containers for local development
# Requires Azure CLI to be installed and authenticated

set -e

echo "🚀 Setting up CosmosDB for Google Connector Service..."

# Variables
RESOURCE_GROUP="${COSMOS_RESOURCE_GROUP:-rg-googleconnector}"
COSMOS_ACCOUNT="${COSMOS_ACCOUNT_NAME:-cosmos-googleconnector}"
DATABASE_NAME="GoogleConnectorDb"
REVIEWS_CONTAINER="Reviews"
BUSINESSES_CONTAINER="Businesses"

# Check if running locally with emulator or Azure
if [ "$1" == "--local" ]; then
    echo "📦 Setting up with CosmosDB Emulator..."
    echo "⚠️  Make sure CosmosDB Emulator is running on https://localhost:8081"
    echo ""
    echo "You'll need to manually create:"
    echo "  - Database: $DATABASE_NAME"
    echo "  - Container: $REVIEWS_CONTAINER (partition key: /id)"
    echo "  - Container: $BUSINESSES_CONTAINER (partition key: /businessId)"
    echo ""
    echo "Open: https://localhost:8081/_explorer/index.html"
    exit 0
fi

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI not found. Please install: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in
if ! az account show &> /dev/null; then
    echo "❌ Not logged into Azure. Run 'az login' first."
    exit 1
fi

echo "Creating database: $DATABASE_NAME"
az cosmosdb sql database create \
    --account-name "$COSMOS_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DATABASE_NAME" \
    --throughput 400 \
    2>/dev/null || echo "Database already exists"

echo "Creating container: $REVIEWS_CONTAINER"
az cosmosdb sql container create \
    --account-name "$COSMOS_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --database-name "$DATABASE_NAME" \
    --name "$REVIEWS_CONTAINER" \
    --partition-key-path "/id" \
    --throughput 400 \
    2>/dev/null || echo "Container already exists"

echo "Creating container: $BUSINESSES_CONTAINER"
az cosmosdb sql container create \
    --account-name "$COSMOS_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --database-name "$DATABASE_NAME" \
    --name "$BUSINESSES_CONTAINER" \
    --partition-key-path "/businessId" \
    --throughput 400 \
    2>/dev/null || echo "Container already exists"

echo ""
echo "✅ CosmosDB setup complete!"
echo ""
echo "Connection string:"
az cosmosdb keys list \
    --name "$COSMOS_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --type connection-strings \
    --query "connectionStrings[0].connectionString" \
    --output tsv

echo ""
echo "Add this to your local.settings.json under CosmosDb:ConnectionString"



