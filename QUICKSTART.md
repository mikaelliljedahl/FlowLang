# FlowLang Quick Start Examples 🚀

**Copy, paste, and run these examples to learn FlowLang in 5 minutes!**

## ⚡ Super Quick Setup

1. **Install .NET 8.0+**: https://dotnet.microsoft.com/download
2. **Clone and build**:
   ```bash
   git clone https://github.com/mikaelliljedahl/FlowLang.git
   cd FlowLang
   bash setup.sh  # Easy setup script
   ```
3. **Ready!** Use `flowc` commands below.

## 📝 Copy-Paste Examples

### 1. Hello World
**File: `hello.flow`**
```flowlang
function main() -> string {
    return "Hello, FlowLang!"
}
```
**Run:**
```bash
flowc run hello.flow
```

### 2. String Interpolation
**File: `greet.flow`**
```flowlang
pure function greet(name: string, age: int) -> string {
    return $"Hello {name}! You are {age} years old."
}

function main() -> string {
    return greet("Alice", 25)
}
```
**Run:**
```bash
flowc run greet.flow
```

### 3. Safe Math with Result Types
**File: `math.flow`**
```flowlang
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Cannot divide by zero")
    }
    return Ok(a / b)
}

function calculate() -> Result<string, string> {
    let result = safeDivide(10, 2)?
    return Ok($"Result: {result}")
}

function main() -> string {
    let outcome = calculate()
    if outcome.IsOk {
        return outcome.Value
    } else {
        return $"Error: {outcome.Error}"
    }
}
```
**Run:**
```bash
flowc run math.flow
```

### 4. Effect System Example
**File: `effects.flow`**
```flowlang
// Pure function - no side effects allowed
pure function add(a: int, b: int) -> int {
    return a + b
}

// Function with database effect
function saveData(value: int) uses [Database] -> Result<string, string> {
    // Simulated database operation
    if value > 0 {
        return Ok($"Saved value: {value}")
    }
    return Error("Invalid value")
}

// Function combining pure and effectful operations
function processAndSave(a: int, b: int) uses [Database] -> Result<string, string> {
    let sum = add(a, b)        // Pure function call
    return saveData(sum)       // Effectful function call
}

function main() -> string {
    let result = processAndSave(5, 3)
    if result.IsOk {
        return result.Value
    } else {
        return $"Error: {result.Error}"
    }
}
```
**Run:**
```bash
flowc run effects.flow
```

### 5. Guard Clauses for Validation
**File: `validate.flow`**
```flowlang
function validateEmail(email: string) -> Result<string, string> {
    guard email != "" else {
        return Error("Email cannot be empty")
    }
    
    guard email.Contains("@") else {
        return Error("Email must contain @")
    }
    
    return Ok("Valid email")
}

function validateAge(age: int) -> Result<string, string> {
    guard age >= 0 else {
        return Error("Age cannot be negative")
    }
    
    guard age <= 150 else {
        return Error("Age seems unrealistic")
    }
    
    return Ok("Valid age")
}

function main() -> string {
    let emailCheck = validateEmail("user@example.com")
    let ageCheck = validateAge(25)
    
    if emailCheck.IsOk && ageCheck.IsOk {
        return "All validations passed!"
    } else {
        return "Validation failed"
    }
}
```
**Run:**
```bash
flowc run validate.flow
```

### 6. Module System
**File: `utils.flow`**
```flowlang
module MathUtils {
    pure function square(x: int) -> int {
        return x * x
    }
    
    pure function double(x: int) -> int {
        return x * 2
    }
    
    export { square, double }
}

module StringUtils {
    pure function shout(text: string) -> string {
        return text + "!"
    }
    
    export { shout }
}

import MathUtils.{square}
import StringUtils.*

function main() -> string {
    let num = square(5)
    return shout($"The square is {num}")
}
```
**Run:**
```bash
flowc run utils.flow
```

### 7. Complex Control Flow
**File: `control.flow`**
```flowlang
function grade(score: int) -> string {
    if score >= 90 {
        return "A"
    } else if score >= 80 {
        return "B"
    } else if score >= 70 {
        return "C"
    } else if score >= 60 {
        return "D"
    } else {
        return "F"
    }
}

function processScores(scores: int[]) -> string {
    let total = 0
    let count = 0
    
    for score in scores {
        total = total + score
        count = count + 1
    }
    
    if count == 0 {
        return "No scores provided"
    }
    
    let average = total / count
    let gradeResult = grade(average)
    
    return $"Average: {average}, Grade: {gradeResult}"
}

function main() -> string {
    let scores = [85, 92, 78, 88, 95]
    return processScores(scores)
}
```
**Run:**
```bash
flowc run control.flow
```

## 🎯 Quick Commands Cheat Sheet

```bash
# Run a single file (shows generated C#)
flowc run myfile.flow

# Create a new project
flowc new my-project
cd my-project

# Build entire project
flowc build

# Run tests
flowc test

# Static analysis
flowc lint

# Security audit
flowc audit

# Start Language Server (for IDE integration)
flowc lsp

# Get help
flowc --help
flowc help <command>
```

## 🔥 Pro Tips

1. **File Extension**: Always use `.flow` for FlowLang files
2. **Main Function**: Use `function main() -> string` as your entry point
3. **Pure Functions**: Mark pure functions with `pure` keyword
4. **Effect Tracking**: Declare side effects with `uses [Effect1, Effect2]`
5. **Error Handling**: Use Result types and `?` operator for safe error propagation
6. **String Interpolation**: Use `$"Hello {variable}"` syntax

## 🐛 Common Issues

**Build errors?**
```bash
# Make sure you're in the FlowLang root directory
cd /path/to/flowlang

# Check .NET version
dotnet --version  # Should be 8.0+

# Rebuild
cd src && dotnet build
```

**Command not found?**
```bash
# Use full command
dotnet run --project src/flowc.csproj -- run myfile.flow

# Or set up alias (recommended)
echo 'alias flowc="dotnet run --project $(pwd)/src/flowc.csproj --"' >> ~/.bashrc
source ~/.bashrc
```

## 📚 Next Steps

1. **Read the docs**: [docs/getting-started.md](docs/getting-started.md)
2. **Language reference**: [docs/language-reference.md](docs/language-reference.md)
3. **More examples**: [docs/examples/](docs/examples/)
4. **IDE setup**: [docs/lsp-integration.md](docs/lsp-integration.md)

**Happy coding with FlowLang!** 🎉