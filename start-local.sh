#!/bin/bash

# Quick start script for running the Google Connector Service locally

echo "🚀 Starting Google Connector Service..."
echo ""

# Check if in correct directory
if [ ! -f "google-connector-service.sln" ]; then
    echo "❌ Error: Must run from the api-googleconnector-service directory"
    exit 1
fi

# Check if local.settings.json exists
if [ ! -f "src/FunctionApp/local.settings.json" ]; then
    echo "⚠️  local.settings.json not found. Creating from example..."
    cp local.settings.json.example src/FunctionApp/local.settings.json
    echo "✅ Created local.settings.json"
    echo ""
    echo "⚠️  IMPORTANT: You need to update src/FunctionApp/local.settings.json with:"
    echo "   - Your CosmosDB connection string"
    echo "   - Your Google API credentials"
    echo ""
    echo "For quick testing without real services, you can proceed, but database"
    echo "operations will fail."
    echo ""
    read -p "Press Enter to continue or Ctrl+C to cancel..."
fi

# Build the solution
echo "🔨 Building solution..."
if ! dotnet build --verbosity quiet; then
    echo "❌ Build failed. Run 'dotnet build' for details."
    exit 1
fi
echo "✅ Build succeeded"
echo ""

# Check if func is installed
if ! command -v func &> /dev/null; then
    echo "❌ Azure Functions Core Tools not found"
    echo "   Install: npm install -g azure-functions-core-tools@4 --unsafe-perm true"
    exit 1
fi

echo "🎯 Starting Azure Functions..."
echo ""
echo "Available endpoints:"
echo "  GET  http://localhost:7071/api/reviews/{businessId}"
echo "  POST http://localhost:7071/api/reviews/ingest"
echo ""
echo "Press Ctrl+C to stop"
echo ""
echo "=========================================="
echo ""

# Start the functions
cd src/FunctionApp
func start



