# FlowLang Getting Started Guide

Welcome to FlowLang! This guide will help you get up and running with FlowLang, a backend programming language designed specifically for LLM-assisted development.

## What is FlowLang?

FlowLang is a modern backend programming language that prioritizes:

- **Explicitness over implicitness**: Every operation, side effect, and dependency is clearly declared
- **One way to do things**: Minimal choices reduce LLM confusion and increase code consistency
- **Safety by default**: Null safety, effect tracking, and comprehensive error handling built-in
- **Self-documenting**: Code structure serves as documentation

FlowLang transpiles to C#, giving you immediate access to the entire .NET ecosystem while providing a safer, more predictable programming experience.

## Installation

### Prerequisites

1. **.NET 8.0 SDK or later**
   - Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
   - Verify installation: `dotnet --version`

2. **Git** (optional, for cloning the repository)
   - Download from [https://git-scm.com/](https://git-scm.com/)

### Installing FlowLang

1. **Clone the FlowLang repository**:
   ```bash
   git clone https://github.com/your-org/flowlang.git
   cd flowlang
   ```

2. **Build the transpiler**:
   ```bash
   cd src
   dotnet build
   ```

3. **Verify installation**:
   ```bash
   dotnet run --project src/flowc.csproj -- --version
   ```

You should see: `FlowLang Transpiler (flowc) v1.0.0`

## Your First Program

Let's create your first FlowLang program!

### 1. Create a Hello World Program

Create a file called `hello.flow`:

```flowlang
pure function greet(name: string) -> string {
    return "Hello, " + name + "!"
}

function main() -> string {
    return greet("World")
}
```

### 2. Transpile and Run

```bash
# Transpile to C#
dotnet run --project src/flowc.csproj -- run hello.flow

# This will show you the generated C# code
```

### Generated C# Output

FlowLang generates clean, readable C# code:

```csharp
/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="name">Parameter of type string</param>
/// <returns>Returns string</returns>
public static string greet(string name)
{
    return "Hello, " + name + "!";
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

## Key Language Features

### Pure Functions

Pure functions have no side effects and always return the same output for the same input:

```flowlang
pure function add(a: int, b: int) -> int {
    return a + b
}

pure function multiply(x: int, y: int) -> int {
    return x * y
}
```

### Result Types for Error Handling

FlowLang uses Result types instead of exceptions for safer error handling:

```flowlang
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Cannot divide by zero")
    }
    return Ok(a / b)
}

function calculate() -> Result<int, string> {
    let result = safeDivide(10, 2)?  // Error propagation
    return Ok(result * 3)
}
```

### String Interpolation

FlowLang supports modern string interpolation:

```flowlang
function formatMessage(name: string, count: int) -> string {
    return $"User {name} has {count} messages"
}
```

### Effect System

Functions can declare their side effects, making them explicit:

```flowlang
function saveUser(name: string) uses [Database, Logging] -> Result<int, string> {
    // Function that uses database and logging effects
    return Ok(42)
}

function fetchData() uses [Network] -> Result<string, string> {
    // Function that makes network calls
    return Ok("data")
}
```

### Control Flow

FlowLang provides familiar control flow with guard clauses for early returns:

```flowlang
function processUser(name: string, age: int) -> Result<string, string> {
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    guard age >= 0 else {
        return Error("Age must be positive")
    }
    
    if age < 18 {
        return Ok("Minor user")
    } else {
        return Ok("Adult user")
    }
}
```

## Creating a Project

### Using the CLI

FlowLang provides a comprehensive CLI for project management:

```bash
# Create a new project
dotnet run --project src/flowc.csproj -- new my-awesome-app
cd my-awesome-app

# Build the project
dotnet run --project ../src/flowc.csproj -- build

# Run tests
dotnet run --project ../src/flowc.csproj -- test

# Run a specific file
dotnet run --project ../src/flowc.csproj -- run examples/hello.flow
```

### Project Structure

A FlowLang project has this structure:

```
my-awesome-app/
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

### Project Configuration (flowc.json)

```json
{
  "Name": "my-awesome-app",
  "Version": "1.0.0",
  "Description": "A FlowLang project",
  "Build": {
    "Source": "src/",
    "Output": "build/",
    "Target": "csharp"
  },
  "Dependencies": {}
}
```

## Module System

FlowLang supports a comprehensive module system for organizing code:

### Creating Modules

**math_utils.flow**:
```flowlang
module MathUtils {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    export {add, multiply}
}
```

### Using Modules

```flowlang
import MathUtils.{add, multiply}  // Selective import
import MathUtils.*                // Wildcard import

function calculate() -> int {
    let sum = add(5, 3)              // From selective import
    let product = multiply(sum, 2)   // From selective import
    return MathUtils.add(product, 1) // Qualified call
}
```

## Best Practices

### 1. Use Pure Functions When Possible

```flowlang
// ✅ Good - Pure function
pure function calculateTax(amount: int, rate: int) -> int {
    return amount * rate / 100
}

// ❌ Avoid - Unnecessary effects
function calculateTax(amount: int, rate: int) uses [Memory] -> int {
    return amount * rate / 100
}
```

### 2. Handle Errors with Result Types

```flowlang
// ✅ Good - Explicit error handling
function parseNumber(input: string) -> Result<int, string> {
    if input == "" {
        return Error("Input cannot be empty")
    }
    // Parsing logic here
    return Ok(42)
}

// ❌ Avoid - No error handling
function parseNumber(input: string) -> int {
    // What if input is invalid?
    return 42
}
```

### 3. Use Guard Clauses for Validation

```flowlang
// ✅ Good - Clear validation with guards
function processUser(name: string, age: int) -> Result<string, string> {
    guard name != "" else {
        return Error("Name required")
    }
    
    guard age >= 0 else {
        return Error("Age must be positive")
    }
    
    return Ok("User processed")
}
```

### 4. Be Explicit About Effects

```flowlang
// ✅ Good - Effects declared
function saveUserData(user: string) uses [Database, Logging] -> Result<int, string> {
    // Clear what side effects this function has
    return Ok(42)
}
```

## Common Patterns

### Error Propagation

Use the `?` operator to propagate errors up the call stack:

```flowlang
function complexOperation() -> Result<int, string> {
    let step1 = validateInput("data")?
    let step2 = processData(step1)?
    let step3 = saveResult(step2)?
    return Ok(step3)
}
```

### Effect Composition

Functions can call other functions with effects, and their effects combine:

```flowlang
function logMessage(msg: string) uses [Logging] -> Result<string, string> {
    return Ok(msg)
}

function saveData(data: string) uses [Database] -> Result<int, string> {
    return Ok(42)
}

function processRequest(data: string) 
    uses [Database, Logging] 
    -> Result<int, string> {
    
    let logged = logMessage("Processing: " + data)?
    let saved = saveData(data)?
    return Ok(saved)
}
```

### Module Organization

Organize related functions into modules:

```flowlang
module UserService {
    function createUser(name: string) uses [Database] -> Result<int, string> {
        return Ok(42)
    }
    
    function getUser(id: int) uses [Database] -> Result<string, string> {
        return Ok("User")
    }
    
    export {createUser, getUser}
}
```

## Next Steps

Now that you have FlowLang up and running:

1. **Explore the Examples**: Check out the `examples/` directory for more complex examples
2. **Read the Language Reference**: See `docs/language-reference.md` for complete syntax documentation
3. **Try the CLI**: Experiment with `flowc new`, `build`, `run`, and `test` commands
4. **Learn About Effects**: Understand FlowLang's effect system in `docs/examples/effect-system.md`
5. **Migration Guide**: If you're coming from C#, see `docs/migration-guide.md`

## Getting Help

- **Documentation**: Check the `docs/` directory for comprehensive guides
- **Examples**: Look at working examples in the `examples/` directory  
- **CLI Help**: Run `dotnet run --project src/flowc.csproj -- help` for command help
- **Troubleshooting**: See `docs/troubleshooting.md` for common issues

## Resources

- [Language Reference](language-reference.md) - Complete syntax and features
- [CLI Reference](cli-reference.md) - All available commands and options
- [Migration Guide](migration-guide.md) - Moving from C# to FlowLang
- [Examples](examples/) - Working code examples
- [Contributing](contributing.md) - How to contribute to FlowLang

Welcome to FlowLang! Happy coding! 🚀