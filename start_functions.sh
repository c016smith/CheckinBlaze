#!/bin/bash
# Start the functions application
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
func start --script-root "${SCRIPT_DIR}/src/CheckinBlaze.Functions"
