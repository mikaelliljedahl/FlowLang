# Cadenza Documentation

Welcome to the comprehensive documentation for Cadenza - a backend programming language designed specifically for LLM-assisted development. This documentation covers everything from getting started to advanced topics and contribution guidelines.

> 🚀 **Want to get started quickly?** Check out the main [README.md](../README.md) or [QUICKSTART.md](../QUICKSTART.md) for immediate copy-paste examples!

## Why Cadenza? The Problem with LLMs and Modern Code

If you've used an LLM to code, you've seen the symptoms: it generates code that uses the wrong error handling pattern, introduces subtle side effects, or misses dependencies. This isn't the LLM's fault; it's a language problem.

Current languages suffer from:

1.  **Ambiguity:** There are often half a dozen ways to perform the same task, from error handling (exceptions, nulls, result objects) to dependency management. An LLM has no "right" way to choose.
2.  **Hidden Side Effects:** A function that looks pure might be secretly writing to a database, calling a network endpoint, or logging to a file. This makes it impossible for an AI to reason about the code's safety and correctness.
3.  **Implicit Error Handling:** Function signatures rarely tell you what can go wrong. LLMs are left to guess which exceptions to catch, leading to fragile code that crashes at runtime.
4.  **Unclear Dependencies:** Code often depends on hidden services or configurations, making it difficult for an LLM to generate correct unit tests or understand the full context of a function.

Cadenza solves these problems by prioritizing explicitness, safety, and predictability. It's designed to be the ideal partner for an LLM, enabling AI to generate correct, robust, and maintainable code on the first try.

## Documentation Overview

Cadenza prioritizes explicitness, predictability, and safety while maintaining compatibility with the .NET ecosystem through C# transpilation. The documentation is organized to support both new users and experienced developers.

## Quick Start

- **New to Cadenza?** Start with [Getting Started](getting-started.md)
- **Coming from C#?** Check out the [Migration Guide](migration-guide.md)
- **Need a quick reference?** See the [Language Reference](language-reference.md)
- **Having issues?** Visit [Troubleshooting](troubleshooting.md)

## Documentation Structure

### Core Documentation

| Document | Description | Audience |
|----------|-------------|----------|
| [Getting Started](getting-started.md) | Installation, first program, basic concepts | New users |
| [Language Reference](language-reference.md) | Complete syntax and features documentation | All users |
| [CLI Reference](cli-reference.md) | All commands, options, and usage patterns | All users |
| [Migration Guide](migration-guide.md) | Moving from C# to Cadenza | C# developers |

### Advanced Topics

| Document | Description | Audience |
|----------|-------------|----------|
| [API Reference](api-reference.md) | Transpiler internals and extension points | Contributors |
| [Contributing](contributing.md) | Development guidelines and processes | Contributors |
| [Testing Guide](testing-guide.md) | Test framework and contribution guidelines | Contributors |
| [Troubleshooting](troubleshooting.md) | Common issues and solutions | All users |

### Phase 2 Tooling Documentation

| Document | Description | Audience |
|----------|-------------|----------|
| [LSP Integration](lsp-integration.md) | Language Server Protocol setup and features | All users |
| [Static Analysis](static-analysis.md) | Linting rules and configuration | All users |
| [Package Manager](package-manager.md) | Dependency management and .NET integration | All users |

### Core Concepts

| Document | Description | Audience |
|----------|-------------|----------|
| [Language Fundamentals](language-fundamentals.md) | The foundational principles of the Cadenza language. | All users |
| [Philosophy and FAQ](Philosophy_and_FAQ.md) | The reasoning behind Cadenza's design and frequently asked questions. | All users |
| [Problem Statement](problem-statement.md) | The problems Cadenza aims to solve. | All users |
| [Specifications](specifications.md) | The language specification. | Advanced users |
| [Transpiler Architecture](transpiler-architecture.md) | A deep-dive into the transpiler's architecture. | Contributors |

### User Guides

| Document | Description | Audience |
|----------|-------------|----------|
| [Syntax Cheat Sheet](SYNTAX_CHEAT_SHEET.md) | A quick reference for Cadenza syntax. | All users |
| [UI Components](ui-components.md) | A guide to using UI components. | All users |

### Development

| Document | Description | Audience |
|----------|-------------|----------|
| [Roadmap Next Phase](roadmap-next-phase.md) | The roadmap for the next phase of development. | All users |
| [Self-Hosting Progress](self-hosting-progress.md) | The progress on self-hosting the Cadenza compiler. | All users |

### Examples and Tutorials

| Document | Description | Features Covered |
|----------|-------------|------------------|
| [Basic Syntax](examples/basic-syntax.md) | Core language constructs | Functions, types, operators |
| [Result Types](examples/result-types.md) | Safe error handling | Error propagation, validation |
| [Effect System](examples/effect-system.md) | Side effect management | Pure functions, effect declarations |
| [Modules](examples/modules.md) | Code organization | Import/export, namespaces |

## Cadenza Features Overview

### Core Philosophy
- **Explicit over implicit**: Every operation and side effect is clearly declared
- **One way to do things**: Minimal choices reduce confusion and increase consistency
- **Safety by default**: Null safety, effect tracking, and comprehensive error handling
- **Self-documenting**: Code structure serves as documentation

### Key Language Features

#### 🔄 Result Types
```cadenza
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}
```

#### ⚡ Effect System
```cadenza
// Pure function - no side effects
pure function calculate(x: int, y: int) -> int {
    return x + y
}

// Effectful function - explicit side effects
function saveUser(name: string) uses [Database, Logging] -> Result<int, string> {
    return Ok(42)
}
```

#### 📦 Module System
```cadenza
module MathUtils {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    export {add}
}

import MathUtils.{add}
```

#### 🔗 String Interpolation
```cadenza
function formatMessage(name: string, count: int) -> string {
    return $"User {name} has {count} items"
}
```

#### 🛡️ Guard Clauses
```cadenza
function validateUser(name: string, age: int) -> Result<string, string> {
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    guard age >= 0 else {
        return Error("Age must be positive")
    }
    
    return Ok("Valid user")
}
```

## Learning Path

### 1. Beginner Path
1. **Installation**: [Getting Started - Installation](getting-started.md#installation)
2. **First Program**: [Getting Started - Your First Program](getting-started.md#your-first-program)
3. **Basic Syntax**: [Basic Syntax Examples](examples/basic-syntax.md)
4. **CLI Usage**: [CLI Reference - Commands](cli-reference.md#commands-overview)

### 2. Intermediate Path
1. **Result Types**: [Result Types Examples](examples/result-types.md)
2. **Error Handling**: [Result Types - Error Propagation](examples/result-types.md#error-propagation)
3. **Effect System**: [Effect System Examples](examples/effect-system.md)
4. **Module Organization**: [Modules Examples](examples/modules.md)

### 3. Advanced Path
1. **Contributing**: [Contributing Guidelines](contributing.md)
2. **Testing**: [Testing Guide](testing-guide.md)
3. **Transpiler Internals**: [API Reference](api-reference.md)
4. **Performance**: [Troubleshooting - Performance](troubleshooting.md#performance-issues)

### 4. Migration Path (for C# Developers)
1. **Concepts Mapping**: [Migration Guide - Key Differences](migration-guide.md#key-differences-overview)
2. **Syntax Translation**: [Migration Guide - Basic Syntax](migration-guide.md#basic-syntax-translation)
3. **Error Handling**: [Migration Guide - Error Handling](migration-guide.md#error-handling-migration)
4. **Best Practices**: [Migration Guide - Best Practices](migration-guide.md#best-practices-migration)

## Documentation Standards

### Code Examples
All code examples in this documentation are:
- ✅ **Tested**: Examples are validated against the transpiler
- ✅ **Complete**: Include all necessary context
- ✅ **Realistic**: Based on practical use cases
- ✅ **Commented**: Explain complex concepts

### Example Format
```cadenza
// Clear comment explaining the purpose
function exampleFunction(param: type) -> returnType {
    // Implementation with explanatory comments
    return value
}
```

### Cross-References
Documents link to related sections:
- **See also**: Links to related topics
- **Prerequisites**: Required knowledge
- **Next steps**: Suggested reading order

## Getting Help

### Documentation Navigation
- Use the table of contents in each document
- Follow cross-references between documents
- Check the troubleshooting guide for common issues

### Community Resources
- **GitHub Issues**: Report bugs and request features
- **Discussions**: Ask questions and share ideas
- **Contributing**: Help improve Cadenza and its documentation

### Support Channels
1. **Self-Service**: Search documentation and examples
2. **Community**: GitHub discussions and issues
3. **Contributing**: Help others and improve the project

## Contributing to Documentation

### How to Contribute
1. **Found an error?** Create an issue or submit a PR
2. **Missing information?** Suggest additions via GitHub issues
3. **Want to help?** See [Contributing Guidelines](contributing.md)

### Documentation Guidelines
- **Clarity**: Write for your intended audience
- **Completeness**: Cover all necessary information
- **Examples**: Include working code samples
- **Testing**: Verify all examples work correctly

### Areas Needing Help
- More real-world examples
- Performance optimization guides
- IDE integration documentation
- Video tutorials and walkthroughs

## Roadmap and Future

### Current Status
Cadenza is in active development with these features implemented:
- ✅ Core language syntax and semantics
- ✅ Result types and error propagation
- ✅ Effect system with pure functions
- ✅ Module system with imports/exports
- ✅ String interpolation and handling
- ✅ Control flow with guard clauses
- ✅ Comprehensive CLI tools
- ✅ Complete testing framework

### Phase 2 Completed (Ecosystem Integration) ✅
- ✅ **Language Server Protocol (LSP)** - Real-time IDE support for VS Code, JetBrains, Visual Studio
- ✅ **Static Analysis & Linting** - 22 specialized rules with Cadenza-specific validation
- ✅ **Enhanced Package Manager** - Seamless .NET ecosystem integration with auto-bindings
- ✅ **Professional CLI Tools** - Complete development workflow commands
- ✅ **Security & Compliance** - Vulnerability scanning and automated fixes

### Phase 3 Planned (Advanced Features)
- 🔄 Saga/Compensation Runtime
- 🔄 Built-in Observability
- 🔄 Performance Optimizations
- 🔄 Multiple Target Support

### Documentation Roadmap

- ✅ **Phase 2 Documentation Complete** - LSP, linting, package management guides
- 📝 Video tutorials
- 📝 Interactive examples  
- 📝 Real-world case studies
- 📝 Performance benchmarking guides

### Roadmap

| Document | Description |
|----------|-------------|
| [Result Types](../roadmap/01-result-types.md) | The plan for implementing result types. |
| [String Literals](../roadmap/02-string-literals.md) | The plan for implementing string literals. |
| [Control Flow](../roadmap/03-control-flow.md) | The plan for implementing control flow. |
| [Effect System](../roadmap/04-effect-system.md) | The plan for implementing the effect system. |
| [Enhanced CLI](../roadmap/05-enhanced-cli.md) | The plan for enhancing the CLI. |
| [Module System](../roadmap/06-module-system.md) | The plan for implementing the module system. |
| [Testing Framework](../roadmap/07-testing-framework.md) | The plan for implementing the testing framework. |
| [Documentation](../roadmap/08-documentation.md) | The plan for improving the documentation. |


## Document Maintenance

### Keeping Documentation Current
- Documentation is updated with each feature release
- Examples are tested with the CI/CD pipeline
- Community feedback drives documentation improvements
- Regular reviews ensure accuracy and completeness

### Version Compatibility
- Documentation matches the current Cadenza version
- Major version changes include migration guides
- Deprecated features are clearly marked
- Legacy documentation is preserved for reference

## Feedback and Improvement

### How to Provide Feedback
- **GitHub Issues**: Report documentation bugs or gaps
- **Pull Requests**: Contribute improvements directly
- **Discussions**: Suggest new documentation topics
- **Community**: Share your experience and suggestions

### Documentation Quality Standards
- **Accuracy**: All information is correct and up-to-date
- **Completeness**: Covers all necessary topics thoroughly
- **Clarity**: Written in clear, accessible language
- **Usefulness**: Provides practical value to readers

## Conclusion

This documentation represents a comprehensive resource for Cadenza users and contributors. Whether you're just getting started or diving deep into the transpiler internals, you'll find the information you need to be successful with Cadenza.

The Cadenza project is committed to maintaining high-quality documentation that serves the community effectively. Your feedback and contributions help make this documentation better for everyone.

**Happy coding with Cadenza!** 🚀

