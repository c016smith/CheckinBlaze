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
echo ""
echo "To start the application locally:"
echo "1. Start Azurite by running 'azurite' in a terminal"
echo "2. Press F5 in VS Code to run both client and functions"
echo "   or run 'dotnet run' in the client and functions directories"
echo "==============================================="