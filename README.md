# Cadenza 🚀

**A backend programming language designed specifically for LLM-assisted development**

Cadenza prioritizes explicitness, predictability, and safety while transpiling to clean C# code. It's designed to be the ideal partner for AI coding assistants.

## ⚡ Quick Start

### 1. Prerequisites
- [.NET 8.0+ SDK](https://dotnet.microsoft.com/download) 
- Git (optional)

### 2. Clone and Build
```bash
git clone https://github.com/mikaelliljedahl/Cadenza.git
cd Cadenza
cd src && dotnet build
```

### 3. Your First Program
Create `hello.cdz`:
```cadenza
pure function greet(name: string) -> string {
    return $"Hello, {name}!"
}

function main() -> string {
    return greet("World")
}
```

### 4. Run It
```bash
# **WORKING: Direct compilation (Phase 4 - July 2025)**
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --compile hello.cdz
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --run hello.cdz

# **WORKING: Traditional transpilation**
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- hello.cdz hello.cs
dotnet run hello.cs

# **WORKING: Standalone executable**
dotnet publish src/Cadenza.Core/cadenzac-core.csproj -c Release -o bin/release
./bin/release/cadenzac-core --compile hello.cdz
./bin/release/cadenzac-core --run hello.cdz
```

## 🔥 Core Features

### Multi-Module System - Working as of July 2025! 🎉
```cadenza
// math.cdz
module Math {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    export { add, multiply }
}

// main.cdz
import Math.{add, multiply}

function main() -> int {
    let result = add(5, 3)      // Resolves to Cadenza.Modules.Math.Math.add(5, 3)
    let product = multiply(result, 2)  // Qualified namespace calls work!
    return product
}
```

### Specification Blocks - Intent Preserved with Code
```cadenza
/*spec
intent: "Validate user data and calculate applicable tax rate"
rules:
  - "Age must be between 0 and 150"
  - "Tax rate varies by age group and location"
postconditions:
  - "Returns appropriate tax rate for demographics"
  - "Fails gracefully with descriptive error messages"
spec*/
function calculateTax(age: int, location: string) -> Result<float, string> {
    guard age >= 0 && age <= 150 else {
        return Error("Invalid age range")
    }
    // Implementation follows specification above
}
```

### Result Types - No More Exceptions
```cadenza
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

function calculate() -> Result<int, string> {
    let result = safeDivide(10, 2)?  // ? automatically propagates errors
    return Ok(result * 2)
}
```

### Effect System - Explicit Side Effects
```cadenza
// Pure function - no side effects
pure function add(a: int, b: int) -> int {
    return a + b
}

// Function with explicit effects
function saveUser(name: string) uses [Database, Logging] -> Result<string, string> {
    log_info($"Saving user: {name}")     // Logging effect
    let id = database_save(name)?        // Database effect
    return Ok(id)
}
```

### String Interpolation
```cadenza
function welcome(name: string, age: int) -> string {
    return $"Welcome {name}! You are {age} years old."
}
```

### Guard Clauses
```cadenza
function validateAge(age: int) -> Result<string, string> {
    guard age >= 0 else {
        return Error("Age cannot be negative")
    }
    guard age <= 150 else {
        return Error("Age seems unrealistic")
    }
    return Ok("Valid age")
}
```

## 📚 Current Implementation Status

### ✅ **WORKING** (Phase 4 - July 2025)
```bash
# **Direct compilation and execution**
./bin/release/cadenzac-core --compile hello.cdz                 # → hello.exe
./bin/release/cadenzac-core --run hello.cdz                     # Compile + run
./bin/release/cadenzac-core --library utils.cdz                 # → utils.dll

# **Traditional transpilation**
./bin/release/cadenzac-core hello.cdz hello.cs                  # → C# source
./bin/release/cadenzac-core --target javascript hello.cdz       # → JavaScript (basic)
./bin/release/cadenzac-core --target blazor ui-component.cdz    # → Blazor component (NEW!)

# **Multi-module compilation**
./bin/release/cadenzac-core --compile main.cdz                  # With imports working
```

### ❌ **NOT WORKING** (Phase 5 - Self-Hosting Migration)
```bash
# **These .cdz tools exist but don't compile:**
# - src/Cadenza.Tools/linter.cdz (298 lines)
# - src/Cadenza.Tools/dev-server.cdz (66 lines)  
# - src/Cadenza.Tools/simple-dev-server.cdz (165 lines)
# Issue: Cadenza.Runtime bridge incomplete, syntax incompatibilities

# **These C# tools exist but aren't integrated:**
# - src/Cadenza.Analysis/ (8 files) - no .csproj
# - src/Cadenza.LSP/ (6 files) - no .csproj
# - src/Cadenza.Package/ (6 files) - no .csproj
# Issue: Incomplete implementations, never tested

# **Missing entirely:**
# - Frontend/UI code generation (Blazor target planned)
# - Package management (cadenzac add, cadenzac install)
# - LSP server for IDE integration
# - Comprehensive linting and analysis
```

## 🎯 Why Cadenza?

**The Problem with LLMs and Traditional Languages:**
- Multiple ways to do the same thing confuse AI
- Hidden side effects make code unpredictable
- Implicit error handling leads to runtime crashes
- Complex syntax requires constant context switching

**Cadenza's Solution:**
- ✅ **One way to do things** - reduces AI confusion
- ✅ **Explicit effects** - `uses [Database, Network]` declares side effects
- ✅ **Safe error handling** - Result types prevent crashes
- ✅ **Self-documenting** - code structure serves as documentation
- ✅ **Specification preservation** - intent and code are atomically linked

## 📚 Learning Resources

### 🚀 Quick Start (5 minutes)
- **[QUICKSTART.md](QUICKSTART.md)** - Copy-paste examples to start immediately
- **[SYNTAX_CHEAT_SHEET.md](SYNTAX_CHEAT_SHEET.md)** - Complete syntax reference
- **[setup.sh](setup.sh)** - One-command installation

### 🤖 For LLMs and AI
- **[EXAMPLES_FOR_LLMS.md](EXAMPLES_FOR_LLMS.md)** - Shows why Cadenza is LLM-friendly
- **[SYNTAX_CHEAT_SHEET.md](SYNTAX_CHEAT_SHEET.md)** - Perfect LLM reference

### 📖 Comprehensive Guides
1. **[Getting Started Guide](docs/getting-started.md)** - Detailed installation and first steps
2. **[Language Reference](docs/language-reference.md)** - Complete language documentation
3. **[Philosophy and FAQ](Philosophy_and_FAQ.md)** - The "why" behind Cadenza
4. **[Transpiler Architecture](docs/transpiler-architecture.md)** - How Cadenza uses Roslyn to generate C# code
5. **[Examples](docs/examples/)** - [Basic Syntax](docs/examples/basic-syntax.md) | [Result Types](docs/examples/result-types.md) | [Effect System](docs/examples/effect-system.md) | [Specification Blocks](examples/specification_example.cdz)
6. **[Tools](docs/)** - [CLI Reference](docs/cli-reference.md) | [LSP Integration](docs/lsp-integration.md) | [Package Manager](docs/package-manager.md)

## 🔧 IDE Integration

Cadenza includes a Language Server Protocol (LSP) implementation for rich IDE support:

```bash
# Start LSP server for VS Code, JetBrains, etc.
cadenzac lsp
```

Features:
- Real-time error detection
- Auto-completion
- Hover type information
- Go-to-definition
- Effect system validation

See [LSP Integration Guide](docs/lsp-integration.md) for setup instructions.

## 🏗️ What Cadenza Generates

Cadenza transpiles to clean, idiomatic C#:

**Cadenza Input:**
```cadenza
function processUser(id: string) uses [Database] -> Result<User, string> {
    let user = database_get_user(id)?
    return Ok(user)
}
```

**Generated C# Output:**
```csharp
/// <summary>
/// Function with effects: Database
/// </summary>
/// <param name="id">Parameter of type string</param>
/// <returns>Returns Result&lt;User, string&gt;</returns>
public static Result<User, string> processUser(string id)
{
    var user = database_get_user(id);
    if (user.IsError) return user;
    return Result<User, string>.Ok(user.Value);
}
```

## 📦 Package Management

Cadenza integrates seamlessly with the .NET ecosystem:

```bash
# Add NuGet packages with automatic effect inference
cadenzac add Newtonsoft.Json
cadenzac add Microsoft.EntityFrameworkCore

# Install dependencies
cadenzac install

# Security audit
cadenzac audit
```

## 🤝 Contributing

We welcome contributions! See [Contributing Guide](docs/contributing.md) for:
- Development setup
- Code style guidelines
- Testing procedures
- How to add new language features

## 📋 Project Status

- ✅ **Phase 1 (MVP):** Core language, transpilation, CLI tools
- ✅ **Phase 2 (Ecosystem):** LSP, static analysis, package management
- 🔄 **Phase 3 (Advanced):** Specification blocks, saga patterns, observability
- 🔄 **Phase 4 (Frontend):** WebAssembly, JavaScript targets

## 🆘 Need Help?

- 📖 **Documentation:** [docs/](docs/)
- 💬 **Issues:** [GitHub Issues](https://github.com/mikaelliljedahl/Cadenza/issues)
- 🐛 **Bug Reports:** Use the issue template
- 💡 **Feature Requests:** Discussions tab

## ⚖️ License

[MIT License](LICENSE) - feel free to use Cadenza in your projects!

---

**Happy coding with Cadenza!** 🎉

*Designed for humans, optimized for AI* ⚡