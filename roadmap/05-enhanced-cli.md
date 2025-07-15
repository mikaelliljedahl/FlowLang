# Enhanced CLI Tool Implementation

## Overview
Create a professional command-line interface for Cadenza with multiple commands and configuration support.

## Goals
- Add `cadenzac new <project>` command for project creation
- Add `cadenzac build` command for building projects
- Add `cadenzac run` command for running transpiled code
- Add `cadenzac test` command for running tests
- Support configuration files (cadenzac.json)

## Technical Requirements

### 1. CLI Structure
- Refactor main application to support subcommands
- Add command-line argument parsing
- Add help system with usage information
- Add version information

### 2. Commands to Implement
- `cadenzac new <name>` - Create new Cadenza project
- `cadenzac build [options]` - Build project to C#
- `cadenzac run [file]` - Transpile and run single file
- `cadenzac test` - Run tests in project
- `cadenzac --help` - Show help information
- `cadenzac --version` - Show version

### 3. Configuration Support
- Read cadenzac.json configuration file
- Support project-level settings
- Support build options and output paths

### 4. Project Structure
- Create standard project template
- Support src/ and examples/ folders
- Generate .gitignore and README.md

## Example Usage
```bash
# Create new project
cadenzac new my-project
cd my-project

# Build project
cadenzac build

# Run single file
cadenzac run examples/hello.cdz

# Run tests
cadenzac test

# Show help
cadenzac --help
```

## Expected Project Structure
```
my-project/
├── cadenzac.json          # Project configuration
├── src/
│   └── main.cdz       # Main source files
├── examples/
│   └── hello.cdz      # Example files
├── tests/
│   └── basic_test.cdz # Test files
├── .gitignore
└── README.md
```

## Configuration File (cadenzac.json)
```json
{
  "name": "my-project",
  "version": "1.0.0",
  "description": "My Cadenza project",
  "build": {
    "source": "src/",
    "output": "build/",
    "target": "csharp"
  },
  "dependencies": {
    "System.Text.Json": "6.0.0"
  }
}
```

## Implementation Tasks
1. Refactor main application for subcommands
2. Add command-line argument parsing
3. Implement `new` command with project template
4. Implement `build` command with project support
5. Implement `run` command for single files
6. Implement `test` command framework
7. Add configuration file support
8. Add help and version commands
9. Create project templates
10. Add error handling and validation
11. Test all commands with examples

## Success Criteria
- All commands work correctly
- Project creation generates proper structure
- Build command processes multiple files
- Configuration file is read and used
- Help system provides clear information
- Error messages are helpful and clear

## Dependencies
- Current transpiler infrastructure
- File system operations
- JSON configuration parsing
- Command-line argument parsing