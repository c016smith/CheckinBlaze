#!/bin/bash

# Create directory for Azurite data if it doesn't exist
mkdir -p ~/.azurite

# Check if Azurite is already running
if lsof -i:10000 > /dev/null || lsof -i:10001 > /dev/null || lsof -i:10002 > /dev/null; then
  echo "Azurite is already running. Stopping existing instance..."
  pkill -f azurite
  # Wait a moment to ensure ports are released
  sleep 2
fi

# Start Azurite in the foreground (no & at the end)
echo "Starting Azurite (Azure Storage Emulator)..."
azurite --location ~/.azurite --debug ~/.azurite/debug.log
