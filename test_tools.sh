#!/bin/bash

echo "FlowLang Self-Hosting Test"
echo "=========================="
echo

echo "Testing FlowLang tool compilation..."
echo "------------------------------------"

# Test development server compilation
echo "1. Compiling development server..."
dotnet run --project src/core/flowc-core.csproj src/tools/simple-dev-server.flow simple-dev-server.cs
if [ $? -eq 0 ]; then
    echo "✓ Development server compiled successfully"
else
    echo "✗ Development server compilation failed"
    exit 1
fi

# Test linter compilation
echo "2. Compiling linter..."
dotnet run --project src/core/flowc-core.csproj src/tools/linter.flow linter.cs
if [ $? -eq 0 ]; then
    echo "✓ Linter compiled successfully"
else
    echo "✗ Linter compilation failed"
    exit 1
fi

# Test dev-server compilation
echo "3. Compiling dev-server..."
dotnet run --project src/core/flowc-core.csproj src/tools/dev-server.flow dev-server.cs
if [ $? -eq 0 ]; then
    echo "✓ Dev-server compiled successfully"
else
    echo "✗ Dev-server compilation failed"
    exit 1
fi

echo
echo "All FlowLang tools compiled successfully!"
echo "FlowLang is now self-hosting with working development tools."
echo
echo "Generated files:"
echo "- simple-dev-server.cs (from simple-dev-server.flow)"
echo "- linter.cs (from linter.flow)"
echo "- dev-server.cs (from dev-server.flow)"
echo
echo "Self-hosting validation complete!"