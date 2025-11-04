#!/bin/bash

# Verification script for Google Connector Service
# This script checks if the solution can be built and run locally

set -e

echo "=================================================="
echo "  Google Connector Service - Setup Verification"
echo "=================================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track status
ALL_CHECKS_PASSED=true

# Function to print status
print_status() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓${NC} $2"
    else
        echo -e "${RED}✗${NC} $2"
        ALL_CHECKS_PASSED=false
    fi
}

echo "Step 1: Checking Prerequisites"
echo "--------------------------------"

# Check .NET SDK
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✓${NC} .NET SDK installed: $DOTNET_VERSION"
    
    # Check if it's .NET 8
    if [[ $DOTNET_VERSION == 8.* ]]; then
        echo -e "${GREEN}✓${NC} .NET 8 detected"
    else
        echo -e "${YELLOW}⚠${NC} .NET 8 recommended (you have $DOTNET_VERSION)"
    fi
else
    echo -e "${RED}✗${NC} .NET SDK not found"
    echo "  Install from: https://dotnet.microsoft.com/download/dotnet/8.0"
    ALL_CHECKS_PASSED=false
fi

# Check Azure Functions Core Tools
if command -v func &> /dev/null; then
    FUNC_VERSION=$(func --version)
    echo -e "${GREEN}✓${NC} Azure Functions Core Tools installed: $FUNC_VERSION"
else
    echo -e "${YELLOW}⚠${NC} Azure Functions Core Tools not found"
    echo "  Install: npm install -g azure-functions-core-tools@4 --unsafe-perm true"
fi

echo ""
echo "Step 2: Checking Solution Structure"
echo "------------------------------------"

# Check if we're in the right directory
if [ ! -f "google-connector-service.sln" ]; then
    echo -e "${RED}✗${NC} Not in the correct directory"
    echo "  Run this script from: api-googleconnector-service/"
    exit 1
fi
echo -e "${GREEN}✓${NC} Solution file found"

# Check key directories
DIRS=(
    "src/Core/Domain"
    "src/Core/Application"
    "src/Infrastructure/Persistence"
    "src/FunctionApp"
    "tests/UnitTests"
    "tests/IntegrationTests"
)

for dir in "${DIRS[@]}"; do
    if [ -d "$dir" ]; then
        echo -e "${GREEN}✓${NC} $dir exists"
    else
        echo -e "${RED}✗${NC} $dir missing"
        ALL_CHECKS_PASSED=false
    fi
done

echo ""
echo "Step 3: Restoring NuGet Packages"
echo "---------------------------------"

if dotnet restore > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} Package restore succeeded"
else
    echo -e "${RED}✗${NC} Package restore failed"
    echo "  Run: dotnet restore --verbosity detailed"
    ALL_CHECKS_PASSED=false
fi

echo ""
echo "Step 4: Building Solution"
echo "-------------------------"

if dotnet build --no-restore > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} Solution build succeeded"
else
    echo -e "${RED}✗${NC} Solution build failed"
    echo "  Run: dotnet build --verbosity detailed"
    ALL_CHECKS_PASSED=false
fi

echo ""
echo "Step 5: Running Tests"
echo "---------------------"

if dotnet test --no-build --verbosity quiet > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} All tests passed"
else
    echo -e "${YELLOW}⚠${NC} Some tests failed or couldn't run"
    echo "  Run: dotnet test --verbosity detailed"
fi

echo ""
echo "Step 6: Checking Configuration"
echo "-------------------------------"

# Check if local.settings.json exists
if [ -f "src/FunctionApp/local.settings.json" ]; then
    echo -e "${GREEN}✓${NC} local.settings.json exists"
    
    # Check if it has required settings
    if grep -q "CosmosDb:ConnectionString" src/FunctionApp/local.settings.json; then
        echo -e "${GREEN}✓${NC} CosmosDB configuration found"
    else
        echo -e "${YELLOW}⚠${NC} CosmosDB configuration may need updating"
    fi
    
    if grep -q "Google:CredentialsJson" src/FunctionApp/local.settings.json; then
        echo -e "${GREEN}✓${NC} Google API configuration found"
    else
        echo -e "${YELLOW}⚠${NC} Google API configuration may need updating"
    fi
else
    echo -e "${YELLOW}⚠${NC} local.settings.json not found"
    echo "  Copy from: local.settings.json.example"
    echo "  Run: cp local.settings.json.example src/FunctionApp/local.settings.json"
fi

echo ""
echo "Step 7: Project Files Verification"
echo "-----------------------------------"

# Count key files
DOMAIN_FILES=$(find src/Core/Domain -name "*.cs" -not -path "*/obj/*" | wc -l | tr -d ' ')
APPLICATION_FILES=$(find src/Core/Application -name "*.cs" -not -path "*/obj/*" | wc -l | tr -d ' ')
FUNCTION_FILES=$(find src/FunctionApp/Functions -name "*.cs" 2>/dev/null | wc -l | tr -d ' ')
TEST_FILES=$(find tests -name "*Tests.cs" -not -path "*/obj/*" 2>/dev/null | wc -l | tr -d ' ')

echo -e "${GREEN}✓${NC} Domain entities: $DOMAIN_FILES files"
echo -e "${GREEN}✓${NC} Application layer: $APPLICATION_FILES files"
echo -e "${GREEN}✓${NC} Function endpoints: $FUNCTION_FILES files"
echo -e "${GREEN}✓${NC} Test files: $TEST_FILES files"

echo ""
echo "=================================================="
echo "  Verification Summary"
echo "=================================================="

if [ "$ALL_CHECKS_PASSED" = true ]; then
    echo -e "${GREEN}✓ All critical checks passed!${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Update src/FunctionApp/local.settings.json with your credentials"
    echo "  2. Start CosmosDB Emulator or configure Azure CosmosDB"
    echo "  3. Run the Functions:"
    echo "     cd src/FunctionApp"
    echo "     func start"
    echo ""
    echo "See RUN_LOCALLY.md for detailed instructions."
else
    echo -e "${RED}✗ Some checks failed${NC}"
    echo ""
    echo "Please fix the issues above before running locally."
    echo "See RUN_LOCALLY.md for troubleshooting."
    exit 1
fi

echo ""
echo "=================================================="



