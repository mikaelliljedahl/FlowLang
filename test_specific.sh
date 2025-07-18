#!/bin/bash

# Helper script to run specific tests and show results
# Usage: bash test_specific.sh [test_name]

TEST_NAME=${1:-"Transpiler_ShouldTranspileResultTypes"}

echo "Running test: $TEST_NAME"
echo "----------------------------------------"

dotnet test --filter "$TEST_NAME" --logger "console;verbosity=normal" 2>&1 | grep -A 25 -B 5 -E "(Passed|Failed|Expected|But was)"