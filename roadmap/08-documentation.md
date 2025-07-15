# Documentation Implementation

## Overview
Create comprehensive documentation for Cadenza including getting started guides, language reference, and examples.

## Goals
- Write getting started guide for new users
- Create complete language reference documentation
- Document all language features with examples
- Create migration guide from C#
- Document CLI tools and project setup

## Technical Requirements

### 1. Documentation Structure
```
docs/
├── getting-started.md
├── language-reference.md
├── examples/
│   ├── basic-syntax.md
│   ├── result-types.md
│   ├── effect-system.md
│   └── modules.md
├── cli-reference.md
├── migration-guide.md
├── api-reference.md
└── contributing.md
```

### 2. Content Requirements
- Clear, beginner-friendly explanations
- Comprehensive code examples
- Side-by-side Cadenza vs C# comparisons
- Practical use cases and patterns
- Troubleshooting and FAQ sections

## Documentation Content

### Getting Started Guide
```markdown
# Cadenza Getting Started

## Installation
1. Download .NET 10 Preview
2. Clone Cadenza repository
3. Build transpiler: `dotnet build`

## Your First Program
Create `hello.cdz`:
```cadenza
function greet(name: string) -> string {
    return "Hello, " + name + "!"
}
```

Transpile to C#:
```bash
cadenzac run hello.cdz
```

## Next Steps
- Learn about Result types
- Explore the effect system
- Try the examples
```

### Language Reference
```markdown
# Cadenza Language Reference

## Functions
Functions are the primary building blocks in Cadenza:

```cadenza
function add(a: int, b: int) -> int {
    return a + b
}
```

## Result Types
Cadenza uses Result types for error handling:

```cadenza
function divide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}
```

## Effect System
Functions can declare their side effects:

```cadenza
function save_user(user: User) uses [Database] -> Result<UserId, Error> {
    return database.save(user)
}
```
```

### CLI Reference
```markdown
# Cadenza CLI Reference

## Commands

### cadenzac new <name>
Create a new Cadenza project:
```bash
cadenzac new my-project
```

### cadenzac build
Build the current project:
```bash
cadenzac build
```

### cadenzac run <file>
Transpile and run a single file:
```bash
cadenzac run examples/hello.cdz
```

### cadenzac test
Run tests in the current project:
```bash
cadenzac test
```

## Configuration
Project settings are stored in `cadenzac.json`:
```json
{
  "name": "my-project",
  "version": "1.0.0",
  "build": {
    "source": "src/",
    "output": "build/"
  }
}
```
```

## Example Documentation

### Result Types Example
```markdown
# Result Types in Cadenza

Result types provide a safe way to handle errors without exceptions.

## Basic Usage

```cadenza
function parse_number(input: string) -> Result<int, string> {
    if input == "" {
        return Error("Empty input")
    }
    
    // In a real implementation, this would parse the string
    return Ok(42)
}
```

## Error Propagation

Use the `?` operator to propagate errors:

```cadenza
function calculate(a: string, b: string) -> Result<int, string> {
    let x = parse_number(a)?
    let y = parse_number(b)?
    return Ok(x + y)
}
```

## Generated C# Code

Cadenza generates clean C# code with proper error handling:

```csharp
public static Result<int, string> parse_number(string input)
{
    if (input == "")
    {
        return Result<int, string>.Error("Empty input");
    }
    
    return Result<int, string>.Ok(42);
}
```
```

## Implementation Tasks
1. Create documentation structure
2. Write getting started guide
3. Create language reference with examples
4. Document all current features
5. Create CLI reference documentation
6. Write migration guide from C#
7. Create practical examples
8. Add troubleshooting section
9. Create API reference
10. Add contributing guidelines
11. Set up documentation website/hosting
12. Add code examples testing

## Success Criteria
- Complete documentation for all features
- Clear, beginner-friendly explanations
- Comprehensive examples that work
- Easy navigation and search
- Up-to-date with latest features
- Positive user feedback

## Dependencies
- All language features implemented
- CLI tools completed
- Working examples and test cases
- Documentation hosting setup