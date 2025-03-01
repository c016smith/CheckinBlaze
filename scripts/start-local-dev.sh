#!/bin/bash

# Start local development environment using tmux

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