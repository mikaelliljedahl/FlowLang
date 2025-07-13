# FlowLang Syntax Cheat Sheet 📝

**Quick reference for FlowLang syntax - perfect for LLMs and humans!**

## 🏗️ Basic Structure

### Function Declaration
```flowlang
// Basic function
function functionName(param: type) -> returnType {
    return value
}

// Pure function (no side effects)
pure function add(a: int, b: int) -> int {
    return a + b
}

// Function with effects
function saveData(data: string) uses [Database, Logging] -> Result<string, string> {
    return Ok("saved")
}
```

### Main Function (Entry Point)
```flowlang
function main() -> string {
    return "Hello, FlowLang!"
}
```

## 📊 Types

### Basic Types
```flowlang
// Primitive types
let number: int = 42
let text: string = "hello"
let flag: bool = true

// Arrays
let numbers: int[] = [1, 2, 3, 4, 5]
let names: string[] = ["Alice", "Bob"]
```

### Result Types (Error Handling)
```flowlang
// Function that can fail
function divide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

// Using Result types
function useResult() -> string {
    let result = divide(10, 2)
    if result.IsOk {
        return $"Success: {result.Value}"
    } else {
        return $"Error: {result.Error}"
    }
}

// Error propagation with ?
function chainResults() -> Result<int, string> {
    let result1 = divide(10, 2)?  // ? automatically propagates errors
    let result2 = divide(result1, 2)?
    return Ok(result2)
}
```

## 🔤 String Operations

### String Interpolation
```flowlang
function greet(name: string, age: int) -> string {
    return $"Hello {name}! You are {age} years old."
}

// Nested expressions
function calculate(a: int, b: int) -> string {
    return $"The sum of {a} and {b} is {a + b}"
}
```

### String Concatenation
```flowlang
function combine(first: string, second: string) -> string {
    return first + " " + second
}
```

## 🔀 Control Flow

### If/Else Statements
```flowlang
function checkScore(score: int) -> string {
    if score >= 90 {
        return "Excellent"
    } else if score >= 70 {
        return "Good"
    } else {
        return "Needs improvement"
    }
}
```

### Guard Clauses
```flowlang
function validateInput(value: int) -> Result<string, string> {
    guard value >= 0 else {
        return Error("Value must be positive")
    }
    
    guard value <= 100 else {
        return Error("Value must be <= 100")
    }
    
    return Ok("Valid input")
}
```

### Boolean Logic
```flowlang
function complexCheck(a: int, b: int, flag: bool) -> bool {
    return (a > 0 && b > 0) || (flag && a != b)
}

function negate(value: bool) -> bool {
    return !value
}
```

## ⚡ Effect System

### Pure Functions
```flowlang
// No side effects allowed
pure function multiply(x: int, y: int) -> int {
    return x * y
}
```

### Functions with Effects
```flowlang
// Common effects: Database, Network, Logging, FileSystem, Memory, IO
function fetchUserData(id: string) uses [Database, Network] -> Result<string, string> {
    // Implementation would use database and network
    return Ok("user data")
}

function processWithLogging(data: string) uses [Logging] -> Result<string, string> {
    // Log the operation
    return Ok("processed: " + data)
}
```

### Effect Propagation
```flowlang
// Callers must declare effects of functions they call
function orchestrate(id: string) uses [Database, Network, Logging] -> Result<string, string> {
    let data = fetchUserData(id)?        // Uses Database, Network
    let result = processWithLogging(data)? // Uses Logging
    return result
}
```

## 📦 Module System

### Defining Modules
```flowlang
module MathUtils {
    pure function square(x: int) -> int {
        return x * x
    }
    
    pure function cube(x: int) -> int {
        return x * x * x
    }
    
    // Only export what should be public
    export { square, cube }
}

module StringUtils {
    pure function upper(text: string) -> string {
        return text.ToUpper()
    }
    
    export { upper }
}
```

### Using Modules
```flowlang
// Selective imports
import MathUtils.{square}
import StringUtils.{upper}

// Wildcard imports
import MathUtils.*

// Qualified calls (without import)
function useModule() -> int {
    return MathUtils.square(5)
}

function main() -> string {
    let num = square(4)           // From selective import
    let text = upper("hello")     // From selective import
    return $"{text}: {num}"
}
```

## 🎯 Common Patterns

### Validation Pattern
```flowlang
function validateUser(name: string, age: int) -> Result<string, string> {
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    guard age >= 0 && age <= 150 else {
        return Error("Invalid age")
    }
    
    return Ok("Valid user")
}
```

### Processing Pattern
```flowlang
function processData(input: string) uses [Database] -> Result<string, string> {
    // Validate input
    guard input != "" else {
        return Error("Input cannot be empty")
    }
    
    // Process
    let processed = input.ToUpper()
    
    // Save and return
    let saveResult = saveToDatabase(processed)?
    return Ok(saveResult)
}
```

### Chaining Pattern
```flowlang
function complexOperation(input: int) -> Result<int, string> {
    let step1 = validateInput(input)?
    let step2 = processStep(step1)?
    let step3 = finalizeStep(step2)?
    return Ok(step3)
}
```

## 🔧 CLI Commands

### File Operations
```bash
# Run single file
flowc run myfile.flow

# Create new project
flowc new my-project

# Build project
flowc build

# Run tests
flowc test
```

### Analysis and Quality
```bash
# Static analysis
flowc lint

# Security audit
flowc audit

# Start Language Server (IDE integration)
flowc lsp
```

### Help
```bash
# General help
flowc --help

# Command-specific help
flowc help run
flowc help new
```

## 💡 Best Practices

1. **Always use Result types** for operations that can fail
2. **Mark pure functions** with the `pure` keyword
3. **Declare all effects** with `uses [Effect1, Effect2]`
4. **Use guard clauses** for input validation
5. **Use string interpolation** instead of concatenation
6. **Keep functions small** and focused
7. **Use meaningful names** for variables and functions

## 🚀 Quick Examples

**Hello World:**
```flowlang
function main() -> string {
    return "Hello, FlowLang!"
}
```

**Safe Division:**
```flowlang
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}
```

**String Interpolation:**
```flowlang
function greet(name: string) -> string {
    return $"Hello, {name}!"
}
```

**Guard Clauses:**
```flowlang
function validate(age: int) -> Result<string, string> {
    guard age >= 0 else {
        return Error("Age must be positive")
    }
    return Ok("Valid")
}
```

---

**That's it!** You now have everything you need to write FlowLang code. 🎉

For more details, see the [full documentation](docs/) or try the [quick start examples](QUICKSTART.md).