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

# Clean up Azurite data files completely
echo "Cleaning up Azurite storage data..."
rm -rf "${HOME}/.azurite" 2>/dev/null

echo "Creating clean Azurite directories..."
mkdir -p "${HOME}/.azurite/__blobstorage__"
mkdir -p "${HOME}/.azurite/__queuestorage__"
mkdir -p "${HOME}/.azurite/__tablestorage__"

# Also clean up any Azurite files in the current directory
echo "Cleaning up Azurite storage data from project directory..."
cd "$(dirname "$0")/.." # Move to project root
rm -f __azurite_db_blob__* 2>/dev/null
rm -f __azurite_db_queue__* 2>/dev/null
rm -f __azurite_db_table__* 2>/dev/null
rm -rf __blobstorage__ 2>/dev/null
rm -rf __queuestorage__ 2>/dev/null
rm -rf __tablestorage__ 2>/dev/null

# Start Azurite in the background
echo "Starting Azurite..."
azurite --silent --location ~/.azurite --debug ~/.azurite/debug.log &
AZURITE_PID=$!

# Wait for Azurite to be ready
echo "Waiting for Azurite to start..."
for i in {1..30}; do
    if curl -s http://127.0.0.1:10002/devstoreaccount1 > /dev/null; then
        echo "Azurite is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "Timed out waiting for Azurite"
        exit 1
    fi
    sleep 1
done

# Create table initialization program that creates empty tables
echo "Creating table initialization program..."
cat > init-tables.cs << 'EOF'
using Azure.Data.Tables;
using System.Threading.Tasks;
using System;

class Program {
    static async Task Main() {
        var connString = "UseDevelopmentStorage=true";
        var tableServiceClient = new TableServiceClient(connString);
        
        string[] tables = new[] {
            "checkinrecords",
            "userpreferences", 
            "auditlogs",
            "headcountcampaigns"
        };

        foreach (var tableName in tables) {
            try {
                // Create the table
                Console.WriteLine($"Creating table: {tableName}");
                await tableServiceClient.CreateTableAsync(tableName);
                
                // Verify table is empty
                var tableClient = tableServiceClient.GetTableClient(tableName);
                var count = 0;
                await foreach (var entity in tableClient.QueryAsync<TableEntity>()) {
                    count++;
                }
                Console.WriteLine($"Table {tableName} created and has {count} records");
                if (count > 0) {
                    throw new Exception($"Table {tableName} should be empty but has {count} records!");
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error with table {tableName}: {ex.Message}");
                throw;
            }
        }
        
        Console.WriteLine("All tables initialized successfully!");
    }
}
EOF

# Compile and run the table initialization program
echo "Initializing Azure Storage tables..."
dotnet new console -n TableInit -o .temptableinit --force
cp init-tables.cs .temptableinit/Program.cs
dotnet add .temptableinit/TableInit.csproj package Azure.Data.Tables
dotnet run --project .temptableinit/TableInit.csproj

# Check if table initialization was successful
if [ $? -ne 0 ]; then
    echo "Table initialization failed!"
    kill $AZURITE_PID
    rm -rf .temptableinit init-tables.cs
    exit 1
fi

# Clean up temporary files
rm -rf .temptableinit init-tables.cs

# Stop Azurite
kill $AZURITE_PID
sleep 2

echo "Rebuilding the solution..."
dotnet build CheckinBlaze.sln

echo "======================================================"
echo "   ENVIRONMENT RESET COMPLETE"
echo "======================================================"
echo "The environment has been reset with clean tables."
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