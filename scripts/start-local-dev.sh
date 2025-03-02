#!/bin/bash

# Get the project root directory
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Starting CheckinBlaze development environment..."

# Kill any existing tmux sessions
tmux kill-session -t local-dev 2>/dev/null

# Configure tmux
tmux set -g mouse on
tmux set -g terminal-overrides 'xterm*:smcup@:rmcup@'

# Create a new tmux session named 'local-dev' with Azurite
echo "Creating tmux session..."
tmux new-session -d -s local-dev "cd '${PROJECT_ROOT}' && ./start_azurite.sh"

# Split vertically for Functions
tmux split-window -v -p 66 "cd '${PROJECT_ROOT}' && sleep 5 && ./start_functions.sh"

# Split again for Client
tmux split-window -v -p 50 "cd '${PROJECT_ROOT}' && sleep 10 && ./start_client.sh"

# Set mouse mode and scrolling
tmux set-window-option -g mouse on

# Set the layout and attach to the session
tmux select-layout even-vertical
echo "Attaching to tmux session..."
tmux attach-session -t local-dev