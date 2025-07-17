# Cadenza Getting Started Guide

Welcome to Cadenza! This guide will help you get up and running with Cadenza, a backend programming language designed specifically for LLM-assisted development.

## What is Cadenza?

Cadenza is a modern backend programming language that prioritizes:

- **Explicitness over implicitness**: Every operation, side effect, and dependency is clearly declared
- **One way to do things**: Minimal choices reduce LLM confusion and increase code consistency
- **Safety by default**: Null safety, effect tracking, and comprehensive error handling built-in
- **Self-documenting**: Code structure serves as documentation

Cadenza transpiles to C#, giving you immediate access to the entire .NET ecosystem while providing a safer, more predictable programming experience.

## Installation

### ⚡ Quick Setup (Recommended)

1. **Install .NET 10.0+**: [Download here](https://dotnet.microsoft.com/download)
2. **Clone the repository**:
   ```bash
   git clone https://github.com/mikaelliljedahl/Cadenza.git
   cd Cadenza
   ```
3. **Build the Cadenza compiler**:
   ```bash
   dotnet build src/Cadenza.Core/cadenzac-core.csproj
   ```
4. **(Optional) Create a standalone executable**:
   ```bash
   dotnet publish src/Cadenza.Core/cadenzac-core.csproj -c Release -o bin/release --self-contained false
   ```
5. **(Optional) Add alias** (makes life easier):
   ```bash
   # For bash/zsh (using standalone executable)
   echo 'alias cadenzac="$(pwd)/bin/release/cadenzac-core"' >> ~/.bashrc && source ~/.bashrc
   
   # For bash/zsh (using dotnet run - for development)
   echo 'alias cadenzac="dotnet run --project $(pwd)/src/Cadenza.Core/cadenzac-core.csproj --"' >> ~/.bashrc && source ~/.bashrc
   ```

### Manual Setup

If you prefer manual setup:

1. **Prerequisites**: .NET 10.0+ SDK ([download](https://dotnet.microsoft.com/download))
2. **Clone and build**:
   ```bash
   git clone https://github.com/mikaelliljedahl/Cadenza.git
   cd Cadenza/src
   dotnet build
   ```
3. **Test installation**:
   ```bash
   dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --version
   ```

### ✅ Verify Installation
```bash
# Test with a simple file
cat > test.cdz << 'EOF'
function main() -> string { return "Hello!" }
EOF
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run test.cdz  # Should show "Hello!" output
```

## Your First Program

### 🚀 Copy-Paste Examples

**Example 1: Hello World**
```bash
# Create the file
cat > hello.cdz << 'EOF'
function main() -> string {
    return "Hello, Cadenza!"
}
EOF

# Run it (working as of July 2025)
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run hello.cdz
```

**Example 2: String Interpolation**
```bash
# Create the file
cat > greet.cdz << 'EOF'
pure function greet(name: string, age: int) -> string {
    return $"Hello {name}! You are {age} years old."
}

function main() -> string {
    return greet("Alice", 25)
}
EOF

# Run it (working as of July 2025)
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run greet.cdz
```

**Example 3: Result Types (Error Handling)**
```bash
# Create the file
cat > math.cdz << 'EOF'
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Cannot divide by zero")
    }
    return Ok(a / b)
}

function main() -> string {
    let result = safeDivide(10, 2)
    if result.IsOk {
        return $"Result: {result.Value}"
    } else {
        return $"Error: {result.Error}"
    }
}
EOF

# Run it (working as of July 2025)
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run math.cdz
```

### 🎯 Quick Commands to Remember

```bash
# **WORKING as of July 2025**
# Direct compilation and execution
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --compile myfile.cdz
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run myfile.cdz

# Traditional transpilation
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- myfile.cdz myfile.cs

# Get help
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --help
```

### Generated C# Output

Cadenza generates clean, readable C# code:

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

## Key Language Features

### Pure Functions

Pure functions have no side effects and always return the same output for the same input:

```cadenza
pure function add(a: int, b: int) -> int {
    return a + b
}

pure function multiply(x: int, y: int) -> int {
    return x * y
}
```

### Result Types for Error Handling

Cadenza uses Result types instead of exceptions for safer error handling:

```cadenza
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

Cadenza supports modern string interpolation:

```cadenza
function formatMessage(name: string, count: int) -> string {
    return $"User {name} has {count} messages"
}
```

### Effect System

Functions can declare their side effects, making them explicit:

```cadenza
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

Cadenza provides familiar control flow with guard clauses for early returns:

```cadenza
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

### Specification Blocks - Preserving Intent

Cadenza allows you to embed specifications directly with your code, creating an atomic link between intent and implementation:

```cadenza
/*spec
intent: "Process user registration with comprehensive validation"
rules:
  - "Name must not be empty"
  - "Age must be non-negative"
  - "Email must be valid format"
postconditions:
  - "User record is created on success"
  - "Validation errors are clearly reported"
spec*/
function registerUser(name: string, age: int, email: string) 
    uses [Database, Logging] -> Result<int, string> {
    
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    guard age >= 0 else {
        return Error("Age must be positive")
    }
    
    guard email != "" else {
        return Error("Email cannot be empty")
    }
    
    // Create user record
    let userId = createUserRecord(name, age, email)?
    let logged = logUserCreation(userId)?
    
    return Ok(userId)
}
```

**Benefits of Specification Blocks:**
- **Context Preservation**: LLMs can understand both what and why
- **Self-Documenting**: Specifications live with implementation
- **Consistency**: No drift between docs and code
- **Automatic Documentation**: Generates rich C# XML docs

## Creating a Project

### Using the CLI

Cadenza provides a comprehensive CLI for project management:

```bash
# **WORKING as of July 2025**
# Direct compilation and execution
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --compile my-awesome-app.cdz
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run my-awesome-app.cdz

# Traditional transpilation
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- my-awesome-app.cdz my-awesome-app.cs

# **NOT YET IMPLEMENTED** (Phase 5 - Self-hosting migration)
# These commands will be available after .cdz tools are tested and fixed:
# cadenzac new my-awesome-app
# cadenzac build  
# cadenzac test
```

### Project Structure

A Cadenza project has this structure:

```
my-awesome-app/
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

### Project Configuration (cadenzac.json)

```json
{
  "Name": "my-awesome-app",
  "Version": "1.0.0",
  "Description": "A Cadenza project",
  "Build": {
    "Source": "src/",
    "Output": "build/",
    "Target": "csharp"
  },
  "Dependencies": {}
}
```

## Module System

Cadenza supports a comprehensive module system for organizing code:

### Creating Modules

**math_utils.cdz**:
```cadenza
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

```cadenza
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

```cadenza
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

```cadenza
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

```cadenza
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

```cadenza
// ✅ Good - Effects declared
function saveUserData(user: string) uses [Database, Logging] -> Result<int, string> {
    // Clear what side effects this function has
    return Ok(42)
}
```

## Common Patterns

### Error Propagation

Use the `?` operator to propagate errors up the call stack:

```cadenza
function complexOperation() -> Result<int, string> {
    let step1 = validateInput("data")?
    let step2 = processData(step1)?
    let step3 = saveResult(step2)?
    return Ok(step3)
}
```

### Effect Composition

Functions can call other functions with effects, and their effects combine:

```cadenza
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

```cadenza
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

## Running Tests

Once you have Cadenza set up, you can run the comprehensive test suite to verify everything is working correctly:

### Running All Tests

```bash
# Navigate to the repository root
cd /path/to/Cadenza

# Run all tests (requires .NET 10.0)
dotnet test tests/Cadenza.Tests.csproj

# Run with verbose output to see test details
dotnet test tests/Cadenza.Tests.csproj --verbosity normal
```

### Test Categories

The test suite includes unit and integration tests for:

- **Lexer**: Token generation and validation
- **Parser**: AST construction and syntax validation
- **AST**: Core data structure validation
- **Transpiler**: Code generation correctness

### Expected Results

A successful test run should show:
```
Passed!  - Failed: 0, Passed: X, Skipped: 0, Total: X
```
(Where X is the total number of tests)

If tests fail, check that:
1. You have .NET 10.0 SDK installed
2. The project builds successfully: `dotnet build src/Cadenza.Core/cadenzac-core.csproj`
3. All dependencies are restored: `dotnet restore`

For more detailed testing information, see the [Testing Guide](testing-guide.md).

## Next Steps

Now that you have Cadenza up and running:

1. **Explore the Examples**: Check out the `examples/` directory, especially `specification_example.cdz` to see how specification blocks preserve intent with code
2. **Read the Language Reference**: See `docs/language-reference.md` for complete syntax documentation
3. **Try Direct Compilation**: Experiment with `--compile` and `--run` commands (working as of July 2025)
4. **Learn About Effects**: Understand Cadenza's effect system in `docs/examples/effect-system.md`
5. **Migration Guide**: If you're coming from C#, see `docs/migration-guide.md`
6. **Phase 5 Development**: Follow the self-hosting migration progress in `NEXT_SPRINT_PLAN.md`

## Getting Help

- **Documentation**: Check the `docs/` directory for comprehensive guides
- **Examples**: Look at working examples in the `examples/` directory  
- **CLI Help**: Run `dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --help` for command help
- **Troubleshooting**: See `docs/troubleshooting.md` for common issues

## Resources

- [Language Reference](language-reference.md) - Complete syntax and features
- [CLI Reference](cli-reference.md) - All available commands and options
- [Migration Guide](migration-guide.md) - Moving from C# to Cadenza
- [Examples](examples/) - Working code examples
- [Contributing](contributing.md) - How to contribute to Cadenza

Welcome to Cadenza! Happy coding! 🚀