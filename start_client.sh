#!/bin/bash

# Start the client application
cd /Users/csmith/Documents/code/checkinBlaze/src/CheckinBlaze.Client
if [ $? -eq 0 ]; then
  dotnet run
else
  echo "Failed to navigate to /Users/csmith/Documents/code/checkinBlaze/src/CheckinBlaze.Client"
fi
