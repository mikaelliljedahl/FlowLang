# Enhanced CLI Tool Demo

This document demonstrates all the enhanced CLI features that have been implemented for the FlowLang transpiler.

## 1. Help and Version Commands

### Show general help
```bash
dotnet run --project transpiler -- --help
```

### Show version
```bash
dotnet run --project transpiler -- --version
```

### Show help for specific commands
```bash
dotnet run --project transpiler -- help new
dotnet run --project transpiler -- help build
dotnet run --project transpiler -- help run
dotnet run --project transpiler -- help test
```

## 2. Creating New Projects

### Create a new FlowLang project
```bash
dotnet run --project transpiler -- new my-awesome-project
cd my-awesome-project
```

This creates a complete project structure:
```
my-awesome-project/
├── flowc.json          # Project configuration
├── src/
│   └── main.flow       # Main source files
├── examples/
│   └── hello.flow      # Example files
├── tests/
│   └── basic_test.flow # Test files
├── .gitignore
└── README.md
```

### Generated flowc.json configuration
```json
{
  "Name": "my-awesome-project",
  "Version": "1.0.0",
  "Description": "A FlowLang project: my-awesome-project",
  "Build": {
    "Source": "src/",
    "Output": "build/",
    "Target": "csharp"
  },
  "Dependencies": {}
}
```

## 3. Building Projects

### Build the current project
```bash
dotnet run --project ../transpiler -- build
```

This:
- Reads configuration from flowc.json
- Finds all .flow files in the source directory
- Transpiles them to C# in the output directory
- Preserves directory structure

## 4. Running Single Files

### Run a single FlowLang file
```bash
dotnet run --project ../transpiler -- run examples/hello.flow
```

This:
- Transpiles the file to a temporary location
- Displays the generated C# code
- Useful for testing and debugging

## 5. Running Tests

### Run all tests in the project
```bash
dotnet run --project ../transpiler -- test
```

This:
- Finds all .flow files in the tests/ directory
- Validates that they transpile correctly
- Reports success/failure for each test

## 6. Error Handling

The CLI provides helpful error messages for common issues:

### Missing project name
```bash
dotnet run --project transpiler -- new
# Output: Error: Project name is required
#         Usage: flowc new <project-name>
```

### File not found
```bash
dotnet run --project transpiler -- run nonexistent.flow
# Output: Error: File 'nonexistent.flow' not found
```

### Unknown command
```bash
dotnet run --project transpiler -- unknown
# Output: Unknown command: unknown
#         Use 'flowc help' for available commands.
```

## 7. Backward Compatibility

The old `--input` mode is still supported but shows a deprecation warning:
```bash
dotnet run --project transpiler -- --input examples/simple.flow
# Output: Warning: --input mode is deprecated. Use 'flowc run <file>' instead.
```

## 8. Complete Example Workflow

```bash
# Create a new project
dotnet run --project transpiler -- new my-flowlang-app
cd my-flowlang-app

# Edit src/main.flow as needed
# ...

# Build the project
dotnet run --project ../transpiler -- build

# Run examples
dotnet run --project ../transpiler -- run examples/hello.flow

# Run tests
dotnet run --project ../transpiler -- test

# Get help when needed
dotnet run --project ../transpiler -- help build
```

## Features Implemented

✅ **Subcommand Support**: Professional CLI with multiple commands  
✅ **Project Creation**: `flowc new` creates complete project structure  
✅ **Build System**: `flowc build` processes projects with configuration  
✅ **Single File Execution**: `flowc run` for testing individual files  
✅ **Test Framework**: `flowc test` runs all tests in project  
✅ **Help System**: Comprehensive help for all commands  
✅ **Configuration**: flowc.json support for project settings  
✅ **Templates**: Standard project templates with examples and tests  
✅ **Error Handling**: Clear error messages and validation  
✅ **Backward Compatibility**: Legacy --input mode still works  

All requirements from the roadmap specification have been successfully implemented!