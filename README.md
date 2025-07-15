# FlowLang 🚀

**A backend programming language designed specifically for LLM-assisted development**

FlowLang prioritizes explicitness, predictability, and safety while transpiling to clean C# code. It's designed to be the ideal partner for AI coding assistants.

## ⚡ Quick Start

### 1. Prerequisites
- [.NET 8.0+ SDK](https://dotnet.microsoft.com/download) 
- Git (optional)

### 2. Clone and Build
```bash
git clone https://github.com/mikaelliljedahl/FlowLang.git
cd FlowLang
cd src && dotnet build
```

### 3. Your First Program
Create `hello.flow`:
```flowlang
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
dotnet run --project src/FlowLang.Core/flowc-core.csproj -- --compile hello.flow
dotnet run --project src/FlowLang.Core/flowc-core.csproj -- --run hello.flow

# **WORKING: Traditional transpilation**
dotnet run --project src/FlowLang.Core/flowc-core.csproj -- hello.flow hello.cs
dotnet run hello.cs

# **WORKING: Standalone executable**
dotnet publish src/FlowLang.Core/flowc-core.csproj -c Release -o bin/release
./bin/release/flowc-core --compile hello.flow
./bin/release/flowc-core --run hello.flow
```

## 🔥 Core Features

### Multi-Module System - Working as of July 2025! 🎉
```flowlang
// math.flow
module Math {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    export { add, multiply }
}

// main.flow
import Math.{add, multiply}

function main() -> int {
    let result = add(5, 3)      // Resolves to FlowLang.Modules.Math.Math.add(5, 3)
    let product = multiply(result, 2)  // Qualified namespace calls work!
    return product
}
```

### Specification Blocks - Intent Preserved with Code
```flowlang
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
```flowlang
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
```flowlang
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
```flowlang
function welcome(name: string, age: int) -> string {
    return $"Welcome {name}! You are {age} years old."
}
```

### Guard Clauses
```flowlang
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
./bin/release/flowc-core --compile hello.flow                 # → hello.exe
./bin/release/flowc-core --run hello.flow                     # Compile + run
./bin/release/flowc-core --library utils.flow                 # → utils.dll

# **Traditional transpilation**
./bin/release/flowc-core hello.flow hello.cs                  # → C# source
./bin/release/flowc-core --target javascript hello.flow       # → JavaScript (basic)
./bin/release/flowc-core --target blazor ui-component.flow    # → Blazor component (NEW!)

# **Multi-module compilation**
./bin/release/flowc-core --compile main.flow                  # With imports working
```

### ❌ **NOT WORKING** (Phase 5 - Self-Hosting Migration)
```bash
# **These .flow tools exist but don't compile:**
# - src/FlowLang.Tools/linter.flow (298 lines)
# - src/FlowLang.Tools/dev-server.flow (66 lines)  
# - src/FlowLang.Tools/simple-dev-server.flow (165 lines)
# Issue: FlowLang.Runtime bridge incomplete, syntax incompatibilities

# **These C# tools exist but aren't integrated:**
# - src/FlowLang.Analysis/ (8 files) - no .csproj
# - src/FlowLang.LSP/ (6 files) - no .csproj
# - src/FlowLang.Package/ (6 files) - no .csproj
# Issue: Incomplete implementations, never tested

# **Missing entirely:**
# - Frontend/UI code generation (Blazor target planned)
# - Package management (flowc add, flowc install)
# - LSP server for IDE integration
# - Comprehensive linting and analysis
```

## 🎯 Why FlowLang?

**The Problem with LLMs and Traditional Languages:**
- Multiple ways to do the same thing confuse AI
- Hidden side effects make code unpredictable
- Implicit error handling leads to runtime crashes
- Complex syntax requires constant context switching

**FlowLang's Solution:**
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
- **[EXAMPLES_FOR_LLMS.md](EXAMPLES_FOR_LLMS.md)** - Shows why FlowLang is LLM-friendly
- **[SYNTAX_CHEAT_SHEET.md](SYNTAX_CHEAT_SHEET.md)** - Perfect LLM reference

### 📖 Comprehensive Guides
1. **[Getting Started Guide](docs/getting-started.md)** - Detailed installation and first steps
2. **[Language Reference](docs/language-reference.md)** - Complete language documentation
3. **[Transpiler Architecture](docs/transpiler-architecture.md)** - How FlowLang uses Roslyn to generate C# code
4. **[Examples](docs/examples/)** - [Basic Syntax](docs/examples/basic-syntax.md) | [Result Types](docs/examples/result-types.md) | [Effect System](docs/examples/effect-system.md) | [Specification Blocks](examples/specification_example.flow)
5. **[Tools](docs/)** - [CLI Reference](docs/cli-reference.md) | [LSP Integration](docs/lsp-integration.md) | [Package Manager](docs/package-manager.md)

## 🔧 IDE Integration

FlowLang includes a Language Server Protocol (LSP) implementation for rich IDE support:

```bash
# Start LSP server for VS Code, JetBrains, etc.
flowc lsp
```

Features:
- Real-time error detection
- Auto-completion
- Hover type information
- Go-to-definition
- Effect system validation

See [LSP Integration Guide](docs/lsp-integration.md) for setup instructions.

## 🏗️ What FlowLang Generates

FlowLang transpiles to clean, idiomatic C#:

**FlowLang Input:**
```flowlang
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

FlowLang integrates seamlessly with the .NET ecosystem:

```bash
# Add NuGet packages with automatic effect inference
flowc add Newtonsoft.Json
flowc add Microsoft.EntityFrameworkCore

# Install dependencies
flowc install

# Security audit
flowc audit
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
- 💬 **Issues:** [GitHub Issues](https://github.com/mikaelliljedahl/FlowLang/issues)
- 🐛 **Bug Reports:** Use the issue template
- 💡 **Feature Requests:** Discussions tab

## ⚖️ License

[MIT License](LICENSE) - feel free to use FlowLang in your projects!

---

**Happy coding with FlowLang!** 🎉

*Designed for humans, optimized for AI* ⚡