#!/bin/bash

# Setup script for local development environment
echo "Setting up CheckinBlaze local development environment..."

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Please install .NET SDK 9.0 or later."
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "Using .NET SDK version: $DOTNET_VERSION"

# Check if Azurite is installed (Azure Storage Emulator for macOS)
if ! command -v npm &> /dev/null; then
    echo "Error: npm not found. Please install Node.js and npm."
    exit 1
fi

# Install Azurite if not already installed
if ! command -v azurite &> /dev/null; then
    echo "Installing Azurite (Azure Storage Emulator)..."
    npm install -g azurite
fi

# Create data directory for Azurite
mkdir -p ~/.azurite

# Start Azurite in the background
echo "Starting Azurite in the background..."
azurite --silent --location ~/.azurite --debug ~/.azurite/debug.log &
AZURITE_PID=$!

# Give Azurite time to start
echo "Waiting for Azurite to start..."
sleep 5

# Initialize Azure Tables
echo "Initializing Azure Tables..."

# Define the required tables based on our constants
TABLES=("checkinrecords" "userpreferences" "auditlogs" "headcountcampaigns")

# Install the Azure CLI Table extension if needed
if ! command -v az extension show --name storage-preview &> /dev/null; then
    echo "Installing Azure Storage CLI extension..."
    az extension add --name storage-preview
fi

# Create each table if it doesn't exist
for TABLE in "${TABLES[@]}"; do
    echo "Creating table: $TABLE"
    az storage table create --name $TABLE \
        --connection-string "UseDevelopmentStorage=true" \
        --only-show-errors || echo "Table $TABLE may already exist"
done

# Validate tables were created
echo "Validating Azure Tables..."
TABLES_CREATED=0
for TABLE in "${TABLES[@]}"; do
    if az storage table exists --name $TABLE \
        --connection-string "UseDevelopmentStorage=true" \
        --only-show-errors > /dev/null; then
        echo "✅ Table $TABLE exists"
        ((TABLES_CREATED++))
    else
        echo "❌ Table $TABLE does not exist"
    fi
done

# Stop the background Azurite process
if [ -n "$AZURITE_PID" ]; then
    echo "Stopping background Azurite process..."
    kill $AZURITE_PID
fi

# Create development certificates
echo "Setting up HTTPS development certificates..."
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore ../CheckinBlaze.sln

# Build solution
echo "Building solution..."
dotnet build ../CheckinBlaze.sln

echo ""
echo "==============================================="
echo "Local dev environment setup complete!"
if [ $TABLES_CREATED -eq ${#TABLES[@]} ]; then
    echo "✅ All Azure Tables initialized successfully."
else
    echo "⚠ Some Azure Tables may not have been initialized."
    echo "   Tables will be created automatically on first run."
fi
echo ""
echo "To start the application locally:"
echo "1. Start Azurite by running './start_azurite.sh' in a terminal"
echo "2. Start the Functions API by running './start_functions.sh'"
echo "3. Start the Client by running './start_client.sh'"
echo "   or run the start-local-dev.sh script to start all components"
echo "==============================================="