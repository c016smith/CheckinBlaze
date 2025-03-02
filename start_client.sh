#!/bin/bash
# Start the client application
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${SCRIPT_DIR}/src/CheckinBlaze.Client"
if [ $? -eq 0 ]; then
  dotnet run
else
  echo "Failed to navigate to Client directory"
fi
