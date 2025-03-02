#!/bin/bash

# Start local development environment using tmux

# First, check if initialization is needed
echo "Checking if Azure Tables are initialized..."
TABLES=("checkinrecords" "userpreferences" "auditlogs" "headcountcampaigns")
TABLES_MISSING=false

# Start Azurite temporarily for checking tables
echo "Starting Azurite temporarily to check tables..."
azurite --silent --location ~/.azurite --debug ~/.azurite/debug.log &
AZURITE_PID=$!

# Give Azurite time to start
sleep 5

# Check if tables exist
for TABLE in "${TABLES[@]}"; do
    echo "Checking table: $TABLE"
    if ! az storage table exists --name $TABLE \
        --connection-string "UseDevelopmentStorage=true" \
        --only-show-errors > /dev/null; then
        echo "❌ Table $TABLE not found, initialization required"
        TABLES_MISSING=true
        break
    fi
done

# Stop the temporary Azurite instance
kill $AZURITE_PID

# Run setup if tables are missing
if $TABLES_MISSING; then
    echo "Some tables are missing. Running setup-local-dev.sh first..."
    chmod +x ./setup-local-dev.sh
    ./setup-local-dev.sh
fi

# Create a new tmux session named 'local-dev'
tmux new-session -d -s local-dev

# Split the window into three panes
tmux split-window -h
tmux split-window -v

# Run Azurite in the first pane
tmux select-pane -t 0
tmux send-keys 'sh ../start_azurite.sh' C-m

# Run the functions application in the second pane
tmux select-pane -t 1
tmux send-keys 'sh ../start_functions.sh' C-m

# Run the client application in the third pane
tmux select-pane -t 2
tmux send-keys 'sh ../start_client.sh' C-m

# Attach to the tmux session
tmux attach-session -t local-dev