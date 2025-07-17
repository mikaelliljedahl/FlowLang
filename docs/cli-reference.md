# Cadenza CLI Reference

The Cadenza CLI (`cadenzac`) provides a comprehensive set of commands for creating, building, and managing Cadenza projects. This reference documents all available commands, options, and usage patterns.

## Table of Contents

1. [Installation and Setup](#installation-and-setup)
2. [Global Options](#global-options)
3. [Commands Overview](#commands-overview)
4. [Command Reference](#command-reference)
5. [Configuration](#configuration)
6. [Workflow Examples](#workflow-examples)
7. [Troubleshooting](#troubleshooting)

## Installation and Setup

### Prerequisites

- .NET 10.0 SDK or later
- Cadenza source code (cloned repository)

### Building the CLI

```bash
# Build the Cadenza.Core project (the compiler itself)
cd src/Cadenza.Core
dotnet build

# Create standalone executable for the compiler
dotnet publish cadenzac-core.csproj -c Release -o ../../bin/release --self-contained false
```

The standalone executable for the compiler will be available at `bin/release/cadenzac-core` (relative to the project root).

### Running Commands

Commands can be run through the .NET CLI or using the standalone executable:

```bash
# Using dotnet run (development)
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- <inputfile.cdz> <outputfile.cs>
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --compile <inputfile.cdz>
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run <inputfile.cdz>

# Using standalone executable (recommended)
./bin/release/cadenzac-core <inputfile.cdz> <outputfile.cs>
./bin/release/cadenzac-core --compile <inputfile.cdz>
./bin/release/cadenzac-core --run <inputfile.cdz>

# Or use the simple wrapper script
./cadenzac <inputfile.cdz>
```

For convenience, you can create an alias:

```bash
# Linux/macOS (using standalone executable)
alias cadenzac="$(pwd)/bin/release/cadenzac-core"

# Linux/macOS (using dotnet run)
alias cadenzac="dotnet run --project $(pwd)/src/Cadenza.Core/cadenzac-core.csproj --"

# Windows (PowerShell - using standalone executable)
function cadenzac { "$PSScriptRoot\bin\release\cadenzac-core.exe" $args }

# Windows (PowerShell - using dotnet run)
function cadenzac { dotnet run --project "$PSScriptRoot\src\Cadenza.Core\cadenzac-core.csproj" -- $args }
```

## Global Options

These options are available for all commands:

| Option | Description |
|--------|-------------|
| `--help`, `-h` | Show help information |
| `--version`, `-v` | Show version information |

### Examples

```bash
# Show general help
cadenzac --help

# Show version
cadenzac --version

# Show help for specific command
cadenzac help new
```

## Commands Overview

| Command | Status | Description | Purpose |
|---------|--------|-------------|---------|
| [`compile`](#compile-command) | ✅ **WORKING** | Compile Cadenza to target language | Multi-target compilation |
| [`run`](#run-command) | ✅ **WORKING** | Transpile and display a single file | Development and testing |
| [`new`](#new-command) | ❌ **Phase 5** | Create a new Cadenza project | Project initialization |
| [`targets`](#targets-command) | ❌ **Phase 5** | List available compilation targets | Target information |
| [`build`](#build-command) | ❌ **Phase 5** | Build the current project | Transpile all source files |
| [`dev`](#dev-command) | ❌ **Phase 5** | Start development server | UI development |
| [`test`](#test-command) | ❌ **Phase 5** | Run all tests in the project | Testing and validation |
| [`lsp`](#lsp-command) | ❌ **Phase 5** | Start the Language Server Protocol server | IDE integration |
| [`lint`](#lint-command) | ❌ **Phase 5** | Run static analysis and linting | Code quality analysis |
| [`add`](#add-command) | ❌ **Phase 5** | Add a package dependency | Package management |
| [`install`](#install-command) | ❌ **Phase 5** | Install project dependencies | Package management |
| [`audit`](#audit-command) | ❌ **Phase 5** | Security audit of dependencies | Security validation |
| [`workspace`](#workspace-command) | ❌ **Phase 5** | Manage multi-project workspaces | Workspace operations |
| [`help`](#help-command) | ✅ **WORKING** | Show help information | Documentation |

## Command Reference

### `compile` Command ✅ **WORKING** (July 2025)

Compiles Cadenza source code directly to executable files or transpiles to target languages.

#### Syntax

```bash
# Direct compilation to executable (WORKING)
./bin/release/cadenzac-core --compile <input-file> [--output <output-file>]
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --compile <input-file>

# Transpilation to target language (WORKING)
./bin/release/cadenzac-core <input-file> <output-file> [--target <target>]
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- <input-file> <output-file>

# Compile and run immediately (WORKING)
./bin/release/cadenzac-core --run <input-file>
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run <input-file>

# Generate library (WORKING)
./bin/release/cadenzac-core --library <input-file> [--output <output-file>]
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --library <input-file>
```

#### Options

- `--compile, -c`: Compile directly to assembly (default: transpile)
- `--run, -r`: Compile and run immediately  
- `--library, -l`: Generate library (.dll) instead of executable
- `--debug, -d`: Include debug symbols and disable optimizations
- `--output, -o`: Specify output file path
- `--target, -t`: Target language for transpilation (csharp, javascript)

#### Parameters

- `<input-file>` (required): Cadenza source file to compile

#### Description

The `compile` command transpiles Cadenza source to various target platforms:

- **C#**: Full .NET compatibility with async/await, LINQ support
- **JavaScript**: React components for frontend development



#### Examples

```bash
# Transpile to C# (default) - WORKING
./bin/release/cadenzac-core backend/UserService.cdz backend/UserService.cs
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- backend/UserService.cdz backend/UserService.cs

# Direct compilation to executable - WORKING
./bin/release/cadenzac-core --compile backend/UserService.cdz
./bin/release/cadenzac-core --compile backend/UserService.cdz --output UserService.exe

# Compile and run immediately - WORKING
./bin/release/cadenzac-core --run backend/UserService.cdz
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run backend/UserService.cdz

# Generate library - WORKING
./bin/release/cadenzac-core --library shared/Utils.cdz --output shared/Utils.dll

# Transpile to JavaScript - BASIC SUPPORT
./bin/release/cadenzac-core frontend/TodoApp.cdz frontend/TodoApp.js --target javascript

# Transpile to Blazor - NEW IN JULY 2025
./bin/release/cadenzac-core --target blazor ui-component.cdz
```

#### Target-Specific Features

**C# Target:**
- Full async/await support
- LINQ and Entity Framework integration
- NuGet package compatibility
- XML documentation generation

**Java Target:**
- Stream API and lambda expressions
- Maven project structure
- JUnit test generation
- Javadoc documentation

**JavaScript Target:**
- React component generation
- npm package.json creation
- TypeScript definitions
- ES6+ modern JavaScript

**WebAssembly Target:**
- Browser compatibility
- Minimal runtime overhead
- JavaScript interop
- Performance-optimized output

#### Auto-Target Detection

Cadenza automatically detects the appropriate target based on source code:

```cadenza
// Auto-detects JavaScript target (UI components)
component UserProfile() uses [DOM] -> UIComponent { ... }

// Auto-detects C# target (backend services)
service UserService uses [Database] { ... }
```

### `targets` Command

Lists all available compilation targets and their capabilities.

#### Syntax

```bash
cadenzac targets [options]
```

#### Options

- `--verbose`, `-v`: Show detailed target information
- `--capabilities`: Show feature support matrix

#### Description

Displays information about Cadenza compilation targets including supported features, limitations, and use cases.

#### Examples

```bash
# List all targets
cadenzac targets

# Show detailed information
cadenzac targets --verbose

# Show feature capabilities
cadenzac targets --capabilities
```

#### Output Example

```
🎯 Available Cadenza compilation targets:

Backend Targets:
  csharp, cs     - C# (.NET) - Full feature support
  java           - Java (JVM) - Full feature support  
  native         - Native code (C++) - High performance

Frontend Targets:
  javascript, js - JavaScript (Node.js/Browser) - UI components
  wasm           - WebAssembly - Browser/edge computing

Example usage:
  cadenzac compile --target javascript ui_app.cdz
  cadenzac compile --target csharp backend_service.cdz
```

### `dev` Command

Starts a development server for UI projects with hot reload and real-time compilation.

#### Syntax

```bash
cadenzac dev [options]
```

#### Options

- `--port <port>`: Specify development server port (default: 3000)
- `--watch`: Enable file watching and hot reload
- `--verbose`: Show detailed compilation output

#### Description

The `dev` command provides a development environment for Cadenza UI applications:

- Hot reload on file changes
- Real-time compilation feedback
- Browser auto-refresh
- Development server with static file serving

#### Examples

```bash
# Start development server
cadenzac dev

# Start on custom port with verbose output
cadenzac dev --port 8080 --verbose

# Development with file watching
cadenzac dev --watch
```

#### Development Workflow

1. Run `cadenzac dev` in your UI project directory
2. Open browser at `http://localhost:3000`
3. Edit Cadenza UI components
4. See changes reflected immediately

### `new` Command ❌ **Phase 5 - Self-hosting migration**

**STATUS**: Not yet implemented. The `new` command will be available after the .cdz tools are tested and fixed in Phase 5.

Creates a new Cadenza project with a complete directory structure.

#### Syntax

```bash
cadenzac new [--template <template>] <project-name>
```

#### Options

- `--template <template>`: Project template (basic, ui, fullstack)

#### Parameters

- `<project-name>` (required): Name of the new project

#### Description

The `new` command creates a complete Cadenza project structure with different templates:

**Basic Template (default):**
- Backend-focused project with C# output
- Simple functions and services
- Basic project structure

**UI Template:**
- Frontend-focused project with React components
- UI component examples
- JavaScript/React output target

**Fullstack Template:**
- Complete full-stack application
- Backend services and frontend components
- Shared type definitions
- Multi-target compilation setup

#### Examples

```bash
# Create a basic backend project
cadenzac new my-backend-app

# Create a UI component project
cadenzac new --template ui my-ui-app

# Create a full-stack application
cadenzac new --template fullstack my-fullstack-app

# Navigate to the project
cd my-ui-app
```

#### Template Structures

**Basic Template:**
```
my-backend-app/
├── cadenzac.json              # Project configuration
├── main.cdz              # Main source file
└── README.md              # Project documentation
```

**UI Template:**
```
my-ui-app/
├── cadenzac.json              # Project configuration
├── main.cdz              # Main UI component
├── components/            # UI components directory
├── state/                 # State management
└── README.md              # Project documentation
```

**Fullstack Template:**
```
my-fullstack-app/
├── cadenzac.json              # Project configuration
├── backend/
│   └── UserService.cdz   # Backend services
├── frontend/
│   ├── components/        # UI components
│   ├── state/            # State management
│   └── main.cdz         # Frontend entry point
├── shared/
│   └── types/            # Shared type definitions
│       └── User.cdz
└── README.md              # Project documentation
```

#### Generated Structure

```
my-awesome-app/
├── cadenzac.json              # Project configuration
├── src/
│   └── main.cdz          # Main source file
├── examples/
│   └── hello.cdz         # Example file
├── tests/
│   └── basic_test.cdz    # Test file
├── .gitignore             # Git ignore patterns
└── README.md              # Project documentation
```

#### Generated Files Content

**cadenzac.json:**
```json
{
  "Name": "my-awesome-app",
  "Version": "1.0.0",
  "Description": "A Cadenza project: my-awesome-app",
  "Build": {
    "Source": "src/",
    "Output": "build/",
    "Target": "csharp"
  },
  "Dependencies": {}
}
```

**src/main.cdz:**
```cadenza
function main() -> string {
    return "Hello, Cadenza!"
}
```

**examples/hello.cdz:**
```cadenza
pure function greet(name: string) -> string {
    return $"Hello, {name}!"
}

function main() -> string {
    return greet("World")
}
```

**tests/basic_test.cdz:**
```cadenza
pure function add(a: int, b: int) -> int {
    return a + b
}

function test_add() -> bool {
    return add(2, 3) == 5
}
```

#### Error Conditions

- **Missing project name**: Returns error code 1
- **Directory already exists**: Returns error code 1  
- **File system errors**: Returns error code 1

### `build` Command ❌ **Phase 5 - Self-hosting migration**

**STATUS**: Not yet implemented. The `build` command will be available after the .cdz tools are tested and fixed in Phase 5.

Builds the current Cadenza project by transpiling all source files.

#### Syntax

```bash
cadenzac build
```

#### Parameters

None.

#### Description

The `build` command:

1. Reads configuration from `cadenzac.json`
2. Finds all `.cdz` files in the source directory
3. Transpiles them to C# files in the output directory
4. Preserves directory structure
5. Reports build status

#### Examples

```bash
# Build the current project
cadenzac build
```

#### Sample Output

```
Building 3 file(s)...
Build completed. Output in 'build/'
```

#### Configuration

Build behavior is controlled by `cadenzac.json`:

```json
{
  "Build": {
    "Source": "src/",        // Source directory
    "Output": "build/",      // Output directory  
    "Target": "csharp"       // Target language
  }
}
```

#### Error Conditions

- **No cadenzac.json**: Uses default configuration with warning
- **No source directory**: Returns error code 1
- **No .cdz files**: Returns error code 1
- **Transpilation errors**: Returns error code 1

### `run` Command ✅ **WORKING** (July 2025)

Transpiles and displays the C# code for a single Cadenza file.

#### Syntax

```bash
# Using standalone executable
./bin/release/cadenzac-core --run <file.cdz>

# Using dotnet run
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run <file.cdz>

# Legacy transpilation mode (shows C# code)
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- <file.cdz>
```

#### Parameters

- `<file.cdz>` (required): Path to the Cadenza file

#### Description

The `run` command:

1. Transpiles the specified file to C#
2. Displays the generated C# code
3. Useful for development, testing, and debugging

#### Examples

```bash
# Run a single file - WORKING
./bin/release/cadenzac-core --run examples/hello.cdz
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run examples/hello.cdz

# Run with relative path - WORKING
./bin/release/cadenzac-core --run src/main.cdz

# Run with absolute path - WORKING
./bin/release/cadenzac-core --run /path/to/project/examples/demo.cdz

# Show transpiled C# code (legacy mode) - WORKING
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- examples/hello.cdz
```

#### Sample Output

```
Transpiled 'examples/hello.cdz' to '/tmp/hello.cs'
Generated C# code:
==================================================
```csharp
/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="name">Parameter of type string</param>
/// <returns>Returns string</returns>
public static string greet(string name)
{
    return $"Hello, {name}!";
}

/// <summary>
/// 
/// </summary>
/// <returns>Returns string</returns>
public static string main()
{
    return greet("World");
}
```
==================================================
Run complete.
```

#### Error Conditions

- **Missing file path**: Returns error code 1
- **File not found**: Returns error code 1
- **Transpilation errors**: Returns error code 1

### `test` Command ❌ **Phase 5 - Self-hosting migration**

**STATUS**: Not yet implemented. The `test` command will be available after the .cdz tools are tested and fixed in Phase 5.

Runs all tests in the current project.

#### Syntax

```bash
cadenzac test
```

#### Parameters

None.

#### Description

The `test` command:

1. Finds all `.cdz` files in the `tests/` directory
2. Validates that each file transpiles correctly
3. Reports success/failure for each test
4. Returns overall test status

#### Examples

```bash
# Run all tests
cadenzac test
```

#### Sample Output

```
Found 2 test file(s)
Processing test: tests/basic_test.cdz
  ✓ Transpiled successfully
Processing test: tests/advanced_test.cdz
  ✓ Transpiled successfully

All tests passed!
```

#### Test Organization

Tests should be organized in the `tests/` directory:

```
tests/
├── basic_test.cdz         # Basic functionality tests
├── result_test.cdz        # Result type tests
├── effect_test.cdz        # Effect system tests
└── integration_test.cdz   # Integration tests
```

#### Error Conditions

- **No tests directory**: Returns success (exit code 0)
- **No test files**: Returns success (exit code 0)
- **Transpilation failures**: Returns error code 1

### `help` Command ✅ **WORKING** (July 2025)

Shows help information for commands.

#### Syntax

```bash
cadenzac help [command]
```

#### Parameters

- `[command]` (optional): Specific command to show help for

#### Description

The `help` command provides:

- General help when no command specified
- Detailed help for specific commands
- Usage examples and syntax

#### Examples

```bash
# Show general help - WORKING
./bin/release/cadenzac-core --help
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --help

# Show version - WORKING
./bin/release/cadenzac-core --version
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --version

# Note: Command-specific help for new, build, run, test will be available in Phase 5
```

#### General Help Output

```
Cadenza Transpiler (cadenzac) v1.0.0

Usage: cadenzac <command> [options]

Commands:
  new          Create a new Cadenza project
  build        Build the current Cadenza project
  run          Transpile and run a single Cadenza file
  test         Run tests in the current project
  help         Show help information

Use 'cadenzac help <command>' for more information about a command.
```

#### Command-specific Help

Each command provides detailed help including:

- **Usage syntax**
- **Parameter descriptions**
- **Examples**
- **Generated file structures** (for `new`)
- **Configuration options** (for `build`)

### `lsp` Command ❌ **Phase 5 - Self-hosting migration**

**STATUS**: Not yet implemented. The `lsp` command will be available after the .cdz tools are tested and fixed in Phase 5.

Starts the Cadenza Language Server Protocol server for IDE integration.

#### Syntax

```bash
cadenzac lsp [options]
```

#### Parameters

- `--verbose` (optional): Enable verbose logging
- `--port <number>` (optional): TCP port for server (default: stdin/stdout)

#### Description

The `lsp` command starts a Language Server Protocol server that provides:

- Real-time syntax and semantic error detection
- Auto-completion for Cadenza keywords, types, and functions
- Hover information with type details and effect annotations
- Go-to-definition navigation
- Document symbols and workspace symbols

#### Examples

```bash
# Start LSP server (stdin/stdout mode for editors)
cadenzac lsp

# Start with verbose logging
cadenzac lsp --verbose

# Start on specific TCP port
cadenzac lsp --port 8080
```

#### IDE Integration

See [LSP Integration Guide](lsp-integration.md) for setup instructions with:
- VS Code
- JetBrains IDEs
- Neovim
- Emacs

### `lint` Command ❌ **Phase 5 - Self-hosting migration**

**STATUS**: Not yet implemented. The `lint` command will be available after the .cdz tools are tested and fixed in Phase 5.

Runs static analysis and linting on Cadenza source code.

#### Syntax

```bash
cadenzac lint [paths...] [options]
```

#### Parameters

- `[paths...]` (optional): Files or directories to analyze (default: current directory)
- `--config <file>` (optional): Custom configuration file (default: cadenzalint.json)
- `--format <format>` (optional): Output format (text|json|sarif, default: text)
- `--fix` (optional): Auto-fix issues where possible
- `--effects` (optional): Run only effect system rules
- `--results` (optional): Run only result type rules
- `--security` (optional): Run only security rules

#### Description

The `lint` command provides comprehensive static analysis including:

- Effect system validation (completeness, minimality, propagation)
- Result type usage analysis
- Security vulnerability detection
- Code quality checks
- Performance optimization suggestions

#### Examples

```bash
# Lint current directory
cadenzac lint

# Lint specific files/directories
cadenzac lint src/ examples/

# Use custom configuration
cadenzac lint --config my-rules.json

# JSON output for CI/CD
cadenzac lint --format json

# Auto-fix issues
cadenzac lint --fix

# Run only effect system rules
cadenzac lint --effects
```

#### Configuration

Create `cadenzalint.json` to customize rules:

```json
{
  "rules": {
    "effect-completeness": "error",
    "unused-results": "error",
    "dead-code": "warning"
  },
  "exclude": ["generated/", "*.test.cdz"]
}
```

See [Static Analysis Guide](static-analysis.md) for complete configuration reference.

### `add` Command

Adds a package dependency to the current project.

#### Syntax

```bash
cadenzac add <package> [version] [options]
```

#### Parameters

- `<package>` (required): Package name to add
- `[version]` (optional): Specific version constraint (default: latest)
- `--dev` (optional): Add as development dependency
- `--registry <url>` (optional): Use specific package registry

#### Description

The `add` command:

1. Resolves the package from Cadenza or NuGet registries
2. Updates `cadenzac.json` with the new dependency
3. Downloads and installs the package
4. Automatically infers effects for .NET libraries
5. Updates the lock file

#### Examples

```bash
# Add latest version
cadenzac add Cadenza.Database

# Add specific version
cadenzac add Newtonsoft.Json@13.0.3

# Add with version constraint
cadenzac add Cadenza.Testing@^1.0.0 --dev

# Add from NuGet
cadenzac add Microsoft.Extensions.Logging
```

### `install` Command

Installs all project dependencies.

#### Syntax

```bash
cadenzac install [options]
```

#### Parameters

- `--production` (optional): Install only production dependencies
- `--force` (optional): Force reinstall all packages
- `--verbose` (optional): Show detailed installation progress

#### Description

The `install` command:

1. Reads dependencies from `cadenzac.json`
2. Resolves version constraints and conflicts
3. Downloads packages from configured registries
4. Updates the lock file
5. Generates effect mappings for .NET libraries

#### Examples

```bash
# Install all dependencies
cadenzac install

# Production-only install
cadenzac install --production

# Force reinstall
cadenzac install --force
```

### `audit` Command

Performs security audit of project dependencies.

#### Syntax

```bash
cadenzac audit [options]
```

#### Parameters

- `--fix` (optional): Automatically fix vulnerabilities where possible
- `--format <format>` (optional): Output format (text|json|sarif, default: text)
- `--severity <level>` (optional): Minimum severity to report (low|medium|high|critical)

#### Description

The `audit` command:

1. Scans all dependencies for known vulnerabilities
2. Checks against GitHub Advisory Database and OSV
3. Reports security issues with severity levels
4. Suggests fixes and updates
5. Can automatically apply compatible fixes

#### Examples

```bash
# Run security audit
cadenzac audit

# Auto-fix vulnerabilities
cadenzac audit --fix

# JSON output for CI/CD
cadenzac audit --format json

# Only critical and high severity
cadenzac audit --severity high
```

### `workspace` Command

Manages multi-project workspaces.

#### Syntax

```bash
cadenzac workspace <subcommand> [options]
```

#### Subcommands

- `list`: List all projects in workspace
- `install`: Install dependencies for all projects
- `run <command>`: Run command across all projects

#### Examples

```bash
# List workspace projects
cadenzac workspace list

# Install dependencies for all projects
cadenzac workspace install

# Run build across workspace
cadenzac workspace run build

# Run tests across workspace
cadenzac workspace run test
```

#### Workspace Configuration

Configure in root `cadenzac.json`:

```json
{
  "name": "my-workspace",
  "workspace": {
    "projects": ["./apps/*", "./libs/*"],
    "exclude": ["./examples"]
  }
}
```

See [Package Manager Guide](package-manager.md) for complete workspace documentation.

## Configuration

### Project Configuration (cadenzac.json)

Cadenza projects are configured using `cadenzac.json`:

```json
{
  "Name": "project-name",
  "Version": "1.0.0",
  "Description": "Project description",
  "Build": {
    "Source": "src/",
    "Output": "build/",
    "Target": "csharp"
  },
  "Dependencies": {}
}
```

#### Configuration Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Name` | string | `"my-project"` | Project name |
| `Version` | string | `"1.0.0"` | Project version |
| `Description` | string | `""` | Project description |
| `Build.Source` | string | `"src/"` | Source directory |
| `Build.Output` | string | `"build/"` | Output directory |
| `Build.Target` | string | `"csharp"` | Target language |
| `Dependencies` | object | `{}` | Project dependencies |

#### Default Configuration

If no `cadenzac.json` exists, these defaults are used:

```json
{
  "Name": "my-project",
  "Version": "1.0.0",
  "Description": "",
  "Build": {
    "Source": "src/",
    "Output": "build/",
    "Target": "csharp"
  },
  "Dependencies": {}
}
```

## Workflow Examples

### Creating a New Project

```bash
# Create project
cadenzac new my-web-api
cd my-web-api

# Edit source files
# ... edit src/main.cdz

# Build project
cadenzac build

# Test project
cadenzac test

# Run examples
cadenzac run examples/hello.cdz
```

### Development Workflow

```bash
# Quick testing of individual files
cadenzac run src/user_service.cdz

# Full project build
cadenzac build

# Run all tests
cadenzac test

# Iterate on development
# ... edit files
cadenzac run src/modified_file.cdz
cadenzac test
```

### Project Structure Best Practices

```
my-project/
├── cadenzac.json                  # Project configuration
├── src/                        # Main source code
│   ├── main.cdz              # Application entry point
│   ├── models/                # Data models
│   │   ├── user.cdz
│   │   └── product.cdz
│   ├── services/              # Business logic
│   │   ├── user_service.cdz
│   │   └── product_service.cdz
│   └── utils/                 # Utility functions
│       └── validation.cdz
├── examples/                   # Usage examples
│   ├── basic_usage.cdz
│   ├── advanced_features.cdz
│   └── integration_example.cdz
├── tests/                      # Test files
│   ├── unit/
│   │   ├── user_tests.cdz
│   │   └── validation_tests.cdz
│   ├── integration/
│   │   └── api_tests.cdz
│   └── performance/
│       └── benchmark_tests.cdz
├── docs/                       # Project documentation
│   ├── API.md
│   └── DEPLOYMENT.md
├── .gitignore                  # Git ignore patterns
└── README.md                   # Project overview
```

### CI/CD Integration

```yaml
# Example GitHub Actions workflow
name: Cadenza CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 10.0.x
    
    - name: Build Cadenza CLI
      run: dotnet build src/
    
    - name: Build Project
      run: dotnet run --project src/cadenzac.csproj -- build
    
    - name: Run Tests
      run: dotnet run --project src/cadenzac.csproj -- test
    
    - name: Upload Build Artifacts
      uses: actions/upload-artifact@v2
      with:
        name: build-output
        path: build/
```

## Troubleshooting

### Common Issues

#### 1. Command Not Found

**Error:** `cadenzac: command not found`

**Solution:**
```bash
# Use full dotnet command (WORKING as of July 2025)
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --version

# Or use standalone executable
./bin/release/cadenzac-core --version

# Or create alias
alias cadenzac="dotnet run --project /path/to/cadenza/src/Cadenza.Core/cadenzac-core.csproj --"
```

#### 2. Project Already Exists

**Error:** `Error: Directory 'my-project' already exists`

**Solutions:**
```bash
# NOTE: cadenzac new is not yet implemented (Phase 5)
# For now, create projects manually:

# Create directory structure
mkdir my-project
cd my-project
mkdir src examples tests

# Create basic files
echo 'function main() -> string { return "Hello!" }' > src/main.cdz
echo 'function greet(name: string) -> string { return $"Hello, {name}!" }' > examples/hello.cdz
```

#### 3. No Source Files

**Error:** `No .cdz files found in 'src/'`

**Solutions:**
```bash
# Check source directory
ls src/

# Create source files
echo 'function main() -> string { return "Hello" }' > src/main.cdz

# Check configuration
cat cadenzac.json
```

#### 4. Transpilation Errors

**Error:** Various compilation errors

**Solutions:**
```bash
# Test individual files (WORKING)
./bin/release/cadenzac-core --run src/problematic_file.cdz
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run src/problematic_file.cdz

# Check syntax against language reference
# See docs/language-reference.md

# Validate Cadenza syntax
# Common issues:
# - Missing type annotations
# - Incorrect effect declarations
# - Invalid Result type usage
```

#### 5. Build Directory Issues

**Error:** Permission or access errors

**Solutions:**
```bash
# Check permissions
ls -la build/

# Create build directory
mkdir -p build

# Check disk space
df -h

# Check file permissions
chmod -R 755 build/
```

### Debug Mode

For additional debugging information:

```bash
# Use verbose dotnet output (WORKING)
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj --verbosity detailed -- --run myfile.cdz

# Check generated files for traditional transpilation
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- myfile.cdz myfile.cs
cat myfile.cs
```

### Getting Help

1. **Built-in Help:**
   ```bash
   # WORKING as of July 2025
   ./bin/release/cadenzac-core --help
   dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --help
   ```

2. **Documentation:**
   - [Getting Started Guide](getting-started.md)
   - [Language Reference](language-reference.md)
   - [Migration Guide](migration-guide.md)

3. **Examples:**
   - Check the `examples/` directory
   - Look at generated project templates

4. **Community:**
   - GitHub Issues
   - Documentation feedback

## Legacy Support

### Backward Compatibility

The CLI maintains backward compatibility with the legacy `--input` mode:

```bash
# Legacy mode (deprecated)
cadenzac --input examples/hello.cdz --output output.cs

# Modern equivalent
cadenzac run examples/hello.cdz
```

### Migration from Legacy CLI

1. **Replace `--input` calls:**
   ```bash
   # Old
   cadenzac --input file.cdz
   
   # New (WORKING as of July 2025)
   ./bin/release/cadenzac-core --run file.cdz
   dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run file.cdz
   ```

2. **Use project structure:**
   ```bash
   # Instead of individual file processing
   # NOTE: cadenzac new and cadenzac build are not yet implemented (Phase 5)
   # For now, work with individual files:
   ./bin/release/cadenzac-core --run file.cdz
   ```

## Summary

The Cadenza CLI provides a complete development experience:

- **Project Management**: Create and organize Cadenza projects
- **Build System**: Transpile source files to C#
- **Development Tools**: Test individual files quickly
- **Testing Framework**: Validate project correctness
- **Configuration**: Flexible project settings
- **Documentation**: Comprehensive help system

For more information, see:
- [Getting Started Guide](getting-started.md) - Basic usage tutorial
- [Language Reference](language-reference.md) - Complete language documentation
- [Migration Guide](migration-guide.md) - Moving from C# to Cadenza
- [Examples Directory](examples/) - Working code samples