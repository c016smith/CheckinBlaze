#!/bin/bash
# This script resets the Azurite storage and restarts the application

echo "======================================================"
echo "   RESET CHECKINBLAZE DEVELOPMENT ENVIRONMENT"
echo "======================================================"

# Check if Azurite is running and stop it
echo "Checking for running Azurite processes..."
if pgrep -f azurite > /dev/null; then
    echo "Stopping Azurite processes..."
    pkill -f azurite
    sleep 2
fi

# Check for any running Functions processes
echo "Checking for running Functions processes..."
if pgrep -f "func start" > /dev/null; then
    echo "Stopping Functions processes..."
    pkill -f "func start"
    sleep 2
fi

# Check for any running Client processes
echo "Checking for running Client processes..."
if pgrep -f "dotnet run.*CheckinBlaze.Client" > /dev/null; then
    echo "Stopping Client processes..."
    pkill -f "dotnet run.*CheckinBlaze.Client" 
    sleep 2
fi

# Clean up Azurite data files
echo "Cleaning up Azurite storage data..."
rm -f "${HOME}/.azurite/__azurite_db_blob__*" 2>/dev/null
rm -f "${HOME}/.azurite/__azurite_db_queue__*" 2>/dev/null
rm -f "${HOME}/.azurite/__azurite_db_table__*" 2>/dev/null
rm -f "${HOME}/.azurite/__blobstorage__/*" 2>/dev/null
rm -f "${HOME}/.azurite/__queuestorage__/*" 2>/dev/null
rm -rf "${HOME}/.azurite/__blobstorage__" 2>/dev/null
rm -rf "${HOME}/.azurite/__queuestorage__" 2>/dev/null

echo "Creating clean Azurite directories..."
mkdir -p "${HOME}/.azurite/__blobstorage__"
mkdir -p "${HOME}/.azurite/__queuestorage__"

# Also clean up any Azurite files in the current directory
echo "Cleaning up Azurite storage data from project directory..."
cd "$(dirname "$0")/.." # Move to project root
rm -f __azurite_db_blob__* 2>/dev/null
rm -f __azurite_db_queue__* 2>/dev/null
rm -f __azurite_db_table__* 2>/dev/null
rm -rf __blobstorage__ 2>/dev/null
rm -rf __queuestorage__ 2>/dev/null

echo "Rebuilding the solution..."
dotnet build CheckinBlaze.sln

echo "======================================================"
echo "   ENVIRONMENT RESET COMPLETE"
echo "======================================================"
echo "You can now start the development environment with:"
echo "   ./scripts/start-local-dev.sh"
echo ""
echo "Or run each component separately:"
echo "1. Start Azurite:   ./start_azurite.sh"
echo "2. Start Functions: ./start_functions.sh"
echo "3. Start Client:    ./start_client.sh"
echo "======================================================"

# Ask if user wants to start the development environment
read -p "Do you want to start the development environment now? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Starting development environment..."
    chmod +x ./scripts/start-local-dev.sh
    ./scripts/start-local-dev.sh
fi