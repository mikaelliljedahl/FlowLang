# FlowLang CLI Reference

The FlowLang CLI (`flowc`) provides a comprehensive set of commands for creating, building, and managing FlowLang projects. This reference documents all available commands, options, and usage patterns.

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

- .NET 8.0 SDK or later
- FlowLang source code (cloned repository)

### Building the CLI

```bash
cd src
dotnet build
```

### Running Commands

All commands are run through the .NET CLI:

```bash
dotnet run --project src/flowc.csproj -- <command> [options]
```

For convenience, you can create an alias:

```bash
# Linux/macOS
alias flowc="dotnet run --project /path/to/flowlang/src/flowc.csproj --"

# Windows (PowerShell)
function flowc { dotnet run --project C:\path\to\flowlang\src\flowc.csproj -- $args }
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
flowc --help

# Show version
flowc --version

# Show help for specific command
flowc help new
```

## Commands Overview

| Command | Description | Purpose |
|---------|-------------|---------|
| [`new`](#new-command) | Create a new FlowLang project | Project initialization |
| [`build`](#build-command) | Build the current project | Transpile all source files |
| [`run`](#run-command) | Transpile and display a single file | Development and testing |
| [`test`](#test-command) | Run all tests in the project | Testing and validation |
| [`lsp`](#lsp-command) | Start the Language Server Protocol server | IDE integration |
| [`lint`](#lint-command) | Run static analysis and linting | Code quality analysis |
| [`add`](#add-command) | Add a package dependency | Package management |
| [`install`](#install-command) | Install project dependencies | Package management |
| [`audit`](#audit-command) | Security audit of dependencies | Security validation |
| [`workspace`](#workspace-command) | Manage multi-project workspaces | Workspace operations |
| [`help`](#help-command) | Show help information | Documentation |

## Command Reference

### `new` Command

Creates a new FlowLang project with a complete directory structure.

#### Syntax

```bash
flowc new <project-name>
```

#### Parameters

- `<project-name>` (required): Name of the new project

#### Description

The `new` command creates a complete FlowLang project structure including:

- Project configuration file (`flowc.json`)
- Source directory with main file
- Examples directory with sample code
- Tests directory with basic tests
- Git ignore file
- README file

#### Examples

```bash
# Create a new project
flowc new my-awesome-app

# Navigate to the project
cd my-awesome-app
```

#### Generated Structure

```
my-awesome-app/
├── flowc.json              # Project configuration
├── src/
│   └── main.flow          # Main source file
├── examples/
│   └── hello.flow         # Example file
├── tests/
│   └── basic_test.flow    # Test file
├── .gitignore             # Git ignore patterns
└── README.md              # Project documentation
```

#### Generated Files Content

**flowc.json:**
```json
{
  "Name": "my-awesome-app",
  "Version": "1.0.0",
  "Description": "A FlowLang project: my-awesome-app",
  "Build": {
    "Source": "src/",
    "Output": "build/",
    "Target": "csharp"
  },
  "Dependencies": {}
}
```

**src/main.flow:**
```flowlang
function main() -> string {
    return "Hello, FlowLang!"
}
```

**examples/hello.flow:**
```flowlang
pure function greet(name: string) -> string {
    return $"Hello, {name}!"
}

function main() -> string {
    return greet("World")
}
```

**tests/basic_test.flow:**
```flowlang
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

### `build` Command

Builds the current FlowLang project by transpiling all source files.

#### Syntax

```bash
flowc build
```

#### Parameters

None.

#### Description

The `build` command:

1. Reads configuration from `flowc.json`
2. Finds all `.flow` files in the source directory
3. Transpiles them to C# files in the output directory
4. Preserves directory structure
5. Reports build status

#### Examples

```bash
# Build the current project
flowc build
```

#### Sample Output

```
Building 3 file(s)...
Build completed. Output in 'build/'
```

#### Configuration

Build behavior is controlled by `flowc.json`:

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

- **No flowc.json**: Uses default configuration with warning
- **No source directory**: Returns error code 1
- **No .flow files**: Returns error code 1
- **Transpilation errors**: Returns error code 1

### `run` Command

Transpiles and displays the C# code for a single FlowLang file.

#### Syntax

```bash
flowc run <file.flow>
```

#### Parameters

- `<file.flow>` (required): Path to the FlowLang file

#### Description

The `run` command:

1. Transpiles the specified file to C#
2. Displays the generated C# code
3. Useful for development, testing, and debugging

#### Examples

```bash
# Run a single file
flowc run examples/hello.flow

# Run with relative path
flowc run src/main.flow

# Run with absolute path
flowc run /path/to/project/examples/demo.flow
```

#### Sample Output

```
Transpiled 'examples/hello.flow' to '/tmp/hello.cs'
Generated C# code:
==================================================
/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="name">Parameter of type string</param>
/// <returns>Returns string</returns>
public static string greet(string name)
{
    return string.Format("Hello, {0}!", name);
}

/// <summary>
/// 
/// </summary>
/// <returns>Returns string</returns>
public static string main()
{
    return greet("World");
}
==================================================
Run complete.
```

#### Error Conditions

- **Missing file path**: Returns error code 1
- **File not found**: Returns error code 1
- **Transpilation errors**: Returns error code 1

### `test` Command

Runs all tests in the current project.

#### Syntax

```bash
flowc test
```

#### Parameters

None.

#### Description

The `test` command:

1. Finds all `.flow` files in the `tests/` directory
2. Validates that each file transpiles correctly
3. Reports success/failure for each test
4. Returns overall test status

#### Examples

```bash
# Run all tests
flowc test
```

#### Sample Output

```
Found 2 test file(s)
Processing test: tests/basic_test.flow
  ✓ Transpiled successfully
Processing test: tests/advanced_test.flow
  ✓ Transpiled successfully

All tests passed!
```

#### Test Organization

Tests should be organized in the `tests/` directory:

```
tests/
├── basic_test.flow         # Basic functionality tests
├── result_test.flow        # Result type tests
├── effect_test.flow        # Effect system tests
└── integration_test.flow   # Integration tests
```

#### Error Conditions

- **No tests directory**: Returns success (exit code 0)
- **No test files**: Returns success (exit code 0)
- **Transpilation failures**: Returns error code 1

### `help` Command

Shows help information for commands.

#### Syntax

```bash
flowc help [command]
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
# Show general help
flowc help

# Show help for specific commands
flowc help new
flowc help build
flowc help run
flowc help test
```

#### General Help Output

```
FlowLang Transpiler (flowc) v1.0.0

Usage: flowc <command> [options]

Commands:
  new          Create a new FlowLang project
  build        Build the current FlowLang project
  run          Transpile and run a single FlowLang file
  test         Run tests in the current project
  help         Show help information

Use 'flowc help <command>' for more information about a command.
```

#### Command-specific Help

Each command provides detailed help including:

- **Usage syntax**
- **Parameter descriptions**
- **Examples**
- **Generated file structures** (for `new`)
- **Configuration options** (for `build`)

### `lsp` Command

Starts the FlowLang Language Server Protocol server for IDE integration.

#### Syntax

```bash
flowc lsp [options]
```

#### Parameters

- `--verbose` (optional): Enable verbose logging
- `--port <number>` (optional): TCP port for server (default: stdin/stdout)

#### Description

The `lsp` command starts a Language Server Protocol server that provides:

- Real-time syntax and semantic error detection
- Auto-completion for FlowLang keywords, types, and functions
- Hover information with type details and effect annotations
- Go-to-definition navigation
- Document symbols and workspace symbols

#### Examples

```bash
# Start LSP server (stdin/stdout mode for editors)
flowc lsp

# Start with verbose logging
flowc lsp --verbose

# Start on specific TCP port
flowc lsp --port 8080
```

#### IDE Integration

See [LSP Integration Guide](lsp-integration.md) for setup instructions with:
- VS Code
- JetBrains IDEs
- Neovim
- Emacs

### `lint` Command

Runs static analysis and linting on FlowLang source code.

#### Syntax

```bash
flowc lint [paths...] [options]
```

#### Parameters

- `[paths...]` (optional): Files or directories to analyze (default: current directory)
- `--config <file>` (optional): Custom configuration file (default: flowlint.json)
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
flowc lint

# Lint specific files/directories
flowc lint src/ examples/

# Use custom configuration
flowc lint --config my-rules.json

# JSON output for CI/CD
flowc lint --format json

# Auto-fix issues
flowc lint --fix

# Run only effect system rules
flowc lint --effects
```

#### Configuration

Create `flowlint.json` to customize rules:

```json
{
  "rules": {
    "effect-completeness": "error",
    "unused-results": "error",
    "dead-code": "warning"
  },
  "exclude": ["generated/", "*.test.flow"]
}
```

See [Static Analysis Guide](static-analysis.md) for complete configuration reference.

### `add` Command

Adds a package dependency to the current project.

#### Syntax

```bash
flowc add <package> [version] [options]
```

#### Parameters

- `<package>` (required): Package name to add
- `[version]` (optional): Specific version constraint (default: latest)
- `--dev` (optional): Add as development dependency
- `--registry <url>` (optional): Use specific package registry

#### Description

The `add` command:

1. Resolves the package from FlowLang or NuGet registries
2. Updates `flowc.json` with the new dependency
3. Downloads and installs the package
4. Automatically infers effects for .NET libraries
5. Updates the lock file

#### Examples

```bash
# Add latest version
flowc add FlowLang.Database

# Add specific version
flowc add Newtonsoft.Json@13.0.3

# Add with version constraint
flowc add FlowLang.Testing@^1.0.0 --dev

# Add from NuGet
flowc add Microsoft.Extensions.Logging
```

### `install` Command

Installs all project dependencies.

#### Syntax

```bash
flowc install [options]
```

#### Parameters

- `--production` (optional): Install only production dependencies
- `--force` (optional): Force reinstall all packages
- `--verbose` (optional): Show detailed installation progress

#### Description

The `install` command:

1. Reads dependencies from `flowc.json`
2. Resolves version constraints and conflicts
3. Downloads packages from configured registries
4. Updates the lock file
5. Generates effect mappings for .NET libraries

#### Examples

```bash
# Install all dependencies
flowc install

# Production-only install
flowc install --production

# Force reinstall
flowc install --force
```

### `audit` Command

Performs security audit of project dependencies.

#### Syntax

```bash
flowc audit [options]
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
flowc audit

# Auto-fix vulnerabilities
flowc audit --fix

# JSON output for CI/CD
flowc audit --format json

# Only critical and high severity
flowc audit --severity high
```

### `workspace` Command

Manages multi-project workspaces.

#### Syntax

```bash
flowc workspace <subcommand> [options]
```

#### Subcommands

- `list`: List all projects in workspace
- `install`: Install dependencies for all projects
- `run <command>`: Run command across all projects

#### Examples

```bash
# List workspace projects
flowc workspace list

# Install dependencies for all projects
flowc workspace install

# Run build across workspace
flowc workspace run build

# Run tests across workspace
flowc workspace run test
```

#### Workspace Configuration

Configure in root `flowc.json`:

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

### Project Configuration (flowc.json)

FlowLang projects are configured using `flowc.json`:

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

If no `flowc.json` exists, these defaults are used:

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
flowc new my-web-api
cd my-web-api

# Edit source files
# ... edit src/main.flow

# Build project
flowc build

# Test project
flowc test

# Run examples
flowc run examples/hello.flow
```

### Development Workflow

```bash
# Quick testing of individual files
flowc run src/user_service.flow

# Full project build
flowc build

# Run all tests
flowc test

# Iterate on development
# ... edit files
flowc run src/modified_file.flow
flowc test
```

### Project Structure Best Practices

```
my-project/
├── flowc.json                  # Project configuration
├── src/                        # Main source code
│   ├── main.flow              # Application entry point
│   ├── models/                # Data models
│   │   ├── user.flow
│   │   └── product.flow
│   ├── services/              # Business logic
│   │   ├── user_service.flow
│   │   └── product_service.flow
│   └── utils/                 # Utility functions
│       └── validation.flow
├── examples/                   # Usage examples
│   ├── basic_usage.flow
│   ├── advanced_features.flow
│   └── integration_example.flow
├── tests/                      # Test files
│   ├── unit/
│   │   ├── user_tests.flow
│   │   └── validation_tests.flow
│   ├── integration/
│   │   └── api_tests.flow
│   └── performance/
│       └── benchmark_tests.flow
├── docs/                       # Project documentation
│   ├── API.md
│   └── DEPLOYMENT.md
├── .gitignore                  # Git ignore patterns
└── README.md                   # Project overview
```

### CI/CD Integration

```yaml
# Example GitHub Actions workflow
name: FlowLang CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 8.0.x
    
    - name: Build FlowLang CLI
      run: dotnet build src/
    
    - name: Build Project
      run: dotnet run --project src/flowc.csproj -- build
    
    - name: Run Tests
      run: dotnet run --project src/flowc.csproj -- test
    
    - name: Upload Build Artifacts
      uses: actions/upload-artifact@v2
      with:
        name: build-output
        path: build/
```

## Troubleshooting

### Common Issues

#### 1. Command Not Found

**Error:** `flowc: command not found`

**Solution:**
```bash
# Use full dotnet command
dotnet run --project src/flowc.csproj -- --version

# Or create alias
alias flowc="dotnet run --project /path/to/flowlang/src/flowc.csproj --"
```

#### 2. Project Already Exists

**Error:** `Error: Directory 'my-project' already exists`

**Solutions:**
```bash
# Use different name
flowc new my-project-v2

# Remove existing directory
rm -rf my-project
flowc new my-project

# Or work in existing directory
cd my-project
# Add flowc.json manually if needed
```

#### 3. No Source Files

**Error:** `No .flow files found in 'src/'`

**Solutions:**
```bash
# Check source directory
ls src/

# Create source files
echo 'function main() -> string { return "Hello" }' > src/main.flow

# Check configuration
cat flowc.json
```

#### 4. Transpilation Errors

**Error:** Various compilation errors

**Solutions:**
```bash
# Test individual files
flowc run src/problematic_file.flow

# Check syntax against language reference
# See docs/language-reference.md

# Validate FlowLang syntax
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
# Use verbose dotnet output
dotnet run --project src/flowc.csproj --verbosity detailed -- build

# Check generated files
ls -la build/
cat build/main.cs
```

### Getting Help

1. **Built-in Help:**
   ```bash
   flowc help
   flowc help <command>
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
flowc --input examples/hello.flow --output output.cs

# Modern equivalent
flowc run examples/hello.flow
```

### Migration from Legacy CLI

1. **Replace `--input` calls:**
   ```bash
   # Old
   flowc --input file.flow
   
   # New
   flowc run file.flow
   ```

2. **Use project structure:**
   ```bash
   # Instead of individual file processing
   flowc new my-project
   # Move files to src/
   flowc build
   ```

## Summary

The FlowLang CLI provides a complete development experience:

- **Project Management**: Create and organize FlowLang projects
- **Build System**: Transpile source files to C#
- **Development Tools**: Test individual files quickly
- **Testing Framework**: Validate project correctness
- **Configuration**: Flexible project settings
- **Documentation**: Comprehensive help system

For more information, see:
- [Getting Started Guide](getting-started.md) - Basic usage tutorial
- [Language Reference](language-reference.md) - Complete language documentation
- [Migration Guide](migration-guide.md) - Moving from C# to FlowLang
- [Examples Directory](examples/) - Working code samples