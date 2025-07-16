# Cadenza Quick Start Examples 🚀

**Copy, paste, and run these examples to learn Cadenza in 5 minutes!**

## ⚡ Super Quick Setup

1. **Install .NET 10.0+**: https://dotnet.microsoft.com/download
2. **Clone and build**:
   ```bash
   git clone https://github.com/mikaelliljedahl/Cadenza.git
   cd Cadenza
   bash setup.sh  # Easy setup script
   ```
3. **Ready!** Use `cadenzac` commands below.

## 📝 Copy-Paste Examples

### 1. Hello World
**File: `hello.cdz`**
```cadenza
function main() -> string {
    return "Hello, Cadenza!"
}
```
**Run:**
```bash
cadenzac run hello.cdz
```

### 2. String Interpolation
**File: `greet.cdz`**
```cadenza
pure function greet(name: string, age: int) -> string {
    return $"Hello {name}! You are {age} years old."
}

function main() -> string {
    return greet("Alice", 25)
}
```
**Run:**
```bash
cadenzac run greet.cdz
```

### 3. Safe Math with Result Types
**File: `math.cdz`**
```cadenza
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
cadenzac run math.cdz
```

### 4. Effect System Example
**File: `effects.cdz`**
```cadenza
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
cadenzac run effects.cdz
```

### 5. Guard Clauses for Validation
**File: `validate.cdz`**
```cadenza
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
cadenzac run validate.cdz
```

### 6. Module System (Multi-Module Projects)
**File: `math.cdz`**
```cadenza
module Math {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    export { add, multiply }
}
```

**File: `main.cdz`**
```cadenza
// Import specific functions (recommended)
import Math.{add, multiply}

function main() -> int {
    let result = add(5, 3)           // Generates: Math.add(5, 3)
    let product = multiply(result, 2) // Generates: Math.multiply(result, 2)
    return product
}
```

**Alternative: Qualified calls (explicit namespacing)**
```cadenza
// File: main_qualified.cdz
import Math.*

function main() -> int {
    let result = Math.add(5, 3)      // Explicit namespace
    let product = Math.multiply(result, 2)  // No ambiguity
    return product
}
```

**Run:**
```bash
# Compile both files together
cadenzac run math.cdz main.cdz
```

### 7. Complex Control Flow
**File: `control.cdz`**
```cadenza
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
cadenzac run control.cdz
```

## 🎯 Quick Commands Cheat Sheet

```bash
# Run a single file (shows generated C#)
cadenzac run myfile.cdz

# Create a new project
cadenzac new my-project
cd my-project

# Build entire project
cadenzac build

# Run tests
cadenzac test

# Static analysis
cadenzac lint

# Security audit
cadenzac audit

# Start Language Server (for IDE integration)
cadenzac lsp

# Get help
cadenzac --help
cadenzac help <command>
```

## 🔥 Pro Tips

1. **File Extension**: Always use `.cdz` for Cadenza files
2. **Main Function**: Use `function main() -> string` as your entry point
3. **Pure Functions**: Mark pure functions with `pure` keyword
4. **Effect Tracking**: Declare side effects with `uses [Effect1, Effect2]`
5. **Error Handling**: Use Result types and `?` operator for safe error propagation
6. **String Interpolation**: Use `$"Hello {variable}"` syntax

## 🐛 Common Issues

**Build errors?**
```bash
# Make sure you're in the Cadenza root directory
cd /path/to/cadenza

# Check .NET version
dotnet --version  # Should be 10.0+

# Rebuild
cd src && dotnet build
```

**Command not found?**
```bash
# Use the core transpiler directly
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- myfile.cdz myfile.cs
dotnet run myfile.cs

# Or use the simple cadenzac script:
./cadenzac myfile.cdz
```

## 📚 Next Steps

1. **Read the docs**: [docs/getting-started.md](docs/getting-started.md)
2. **Language reference**: [docs/language-reference.md](docs/language-reference.md)
3. **More examples**: [docs/examples/](docs/examples/)
4. **IDE setup**: [docs/lsp-integration.md](docs/lsp-integration.md)

**Happy coding with Cadenza!** 🎉