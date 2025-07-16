# Contributing to Cadenza

Thank you for your interest in contributing to Cadenza! This document provides guidelines and information for developers who want to help improve the Cadenza programming language and transpiler.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Development Environment](#development-environment)
3. [Project Structure](#project-structure)
4. [Code Style Guidelines](#code-style-guidelines)
5. [Contributing Process](#contributing-process)
6. [Types of Contributions](#types-of-contributions)
7. [Testing Guidelines](#testing-guidelines)
8. [Documentation Guidelines](#documentation-guidelines)
9. [Release Process](#release-process)
10. [Community Guidelines](#community-guidelines)

## Getting Started

### Prerequisites

- **.NET 10.0 SDK or later**
- **Git** for version control
- **Code editor** (Visual Studio, VS Code, JetBrains Rider)
- **Basic understanding** of compiler construction and C#

### First Steps

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/your-username/cadenza.git
   cd cadenza
   ```
3. **Set up the upstream remote**:
   ```bash
   git remote add upstream https://github.com/original-org/cadenza.git
   ```
4. **Build the project**:
   ```bash
   cd src
   dotnet build
   ```
5. **Run tests** to ensure everything works:
   ```bash
   cd ../tests
   dotnet test
   ```

## Development Environment

### Recommended Setup

**Visual Studio Code:**
```json
// .vscode/settings.json
{
    "dotnet.defaultSolution": "Cadenza.sln",
    "omnisharp.enableRoslynAnalyzers": true,
    "csharp.format.enable": true,
    "editor.formatOnSave": true
}
```

**EditorConfig** (already included in project):
```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true
```

### Development Tools

- **Language Server**: Cadenza syntax highlighting (future)
- **Debugger**: Use C# debugging for transpiler
- **Performance Profiler**: For optimization work
- **Git Hooks**: Pre-commit formatting and tests

## Project Structure

### Directory Overview

```
cadenza/
├── src/                    # Main transpiler source code
│   ├── cadenzac.cs           # Single-file transpiler implementation
│   └── cadenzac.csproj       # Project configuration
├── tests/                  # Comprehensive test suite
│   ├── unit/              # Unit tests for components
│   ├── integration/       # End-to-end tests
│   ├── golden/            # Golden file tests
│   ├── performance/       # Performance benchmarks
│   └── regression/        # Regression tests
├── docs/                   # Documentation
│   ├── getting-started.md
│   ├── language-reference.md
│   ├── cli-reference.md
│   ├── migration-guide.md
│   ├── api-reference.md
│   └── contributing.md    # This file
├── examples/              # Example Cadenza programs
├── roadmap/               # Development roadmap documents
└── README.md              # Project overview
```

### Key Components

| Component | Location | Description |
|-----------|----------|-------------|
| **Lexer** | `src/cadenzac.cs` | Tokenization (lines 164-473) |
| **Parser** | `src/cadenzac.cs` | Syntax analysis (lines 479-1217) |
| **AST** | `src/cadenzac.cs` | Node definitions (lines 106-158) |
| **Code Generator** | `src/cadenzac.cs` | C# generation (lines 1224-1818) |
| **CLI** | `src/cadenzac.cs` | Command interface (lines 1846-2386) |

## Code Style Guidelines

### C# Coding Standards

1. **Use C# 11+ features** when appropriate
2. **Follow .NET naming conventions**:
   - PascalCase for classes, methods, properties
   - camelCase for local variables, parameters
   - UPPER_CASE for constants
3. **Use record types** for immutable data structures
4. **Prefer expression-bodied members** when concise
5. **Use nullable reference types** consistently

### Example Code Style

```csharp
// ✅ Good
public record TokenLocation(int Line, int Column);

public class CadenzaLexer
{
    private readonly string _source;
    private int _position = 0;

    public CadenzaLexer(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public List<Token> Tokenize() => 
        TokenizeInternal().ToList();

    private IEnumerable<Token> TokenizeInternal()
    {
        while (!IsAtEnd())
        {
            var token = NextToken();
            if (token != null)
                yield return token;
        }
    }
}

// ❌ Avoid
public class bad_lexer  // Wrong naming
{
    public string source;  // Should be private/readonly
    
    public List<Token> tokenize()  // Wrong naming
    {
        List<Token> tokens = new List<Token>();  // Prefer var
        // ... rest of implementation
        return tokens;
    }
}
```

### Cadenza Language Style

When writing Cadenza examples or tests:

```cadenza
// ✅ Good - Clear, explicit
pure function calculateTax(amount: int, rate: int) -> int {
    return amount * rate / 100
}

function saveUser(name: string) uses [Database, Logging] -> Result<int, string> {
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    return Ok(42)
}

// ❌ Avoid - Missing types, unclear effects
function calculate(amount, rate) {
    return amount * rate / 100
}

function save(name) {
    return 42
}
```

## Contributing Process

### 1. Planning Your Contribution

Before starting work:

1. **Check existing issues** for similar work
2. **Create an issue** to discuss large changes
3. **Review the roadmap** to understand priorities
4. **Join the discussion** in existing issues

### 2. Making Changes

1. **Create a feature branch**:
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Make small, focused commits**:
   ```bash
   git add .
   git commit -m "Add: Support for new string escape sequences
   
   - Add support for \u unicode escapes
   - Add tests for new escape sequences
   - Update documentation with examples"
   ```

3. **Keep commits atomic** - one logical change per commit
4. **Write clear commit messages** following conventional commits

### 3. Testing Your Changes

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "FullyQualifiedName~Unit"
dotnet test --filter "FullyQualifiedName~Integration"

# Run performance tests
dotnet test --filter "FullyQualifiedName~Performance"

# Test CLI commands
cd examples
dotnet run --project ../src/cadenzac.csproj -- run simple.cdz
```

### 4. Submitting Your Changes

1. **Push your branch**:
   ```bash
   git push origin feature/my-new-feature
   ```

2. **Create a Pull Request** with:
   - Clear title and description
   - Reference to related issues
   - Screenshots or examples if applicable
   - Test results and performance impact

3. **Respond to feedback** promptly and professionally

### 5. After Submission

- **Keep your branch updated** with main
- **Address review comments** 
- **Rebase if requested** to maintain clean history
- **Celebrate** when your contribution is merged! 🎉

## Types of Contributions

### 1. Bug Fixes

**Process:**
1. Create issue with reproduction steps
2. Write failing test that demonstrates bug
3. Fix the bug
4. Ensure test passes
5. Update documentation if needed

**Example Bug Fix:**
```csharp
// Fix: String interpolation with nested braces
// Before: Throws exception on $"Value: {obj.Property}"
// After: Correctly parses nested expressions

private ASTNode ParseStringInterpolationExpression(string template)
{
    // ... fixed implementation
}
```

### 2. New Language Features

**Process:**
1. Discuss in GitHub issue first
2. Update language specification
3. Add lexer tokens if needed
4. Add AST nodes
5. Implement parser support
6. Add code generation
7. Write comprehensive tests
8. Update documentation and examples

**Example Feature Addition:**
```csharp
// Adding support for boolean literals
public enum TokenType
{
    // ... existing tokens
    True,
    False,
}

public record BooleanLiteral(bool Value) : ASTNode;
```

### 3. Performance Improvements

**Process:**
1. Profile current performance
2. Identify bottlenecks
3. Implement optimization
4. Measure improvement
5. Ensure no regressions
6. Update performance tests

**Example Performance Fix:**
```csharp
// Before: O(n²) string concatenation
var result = "";
foreach (var part in parts)
    result += part;

// After: O(n) with StringBuilder
var result = new StringBuilder();
foreach (var part in parts)
    result.Append(part);
return result.ToString();
```

### 4. Documentation Improvements

**Types of documentation contributions:**
- **API documentation**: Inline code comments
- **User guides**: Getting started, tutorials
- **Reference docs**: Language specification
- **Examples**: Working code samples
- **Migration guides**: From other languages

### 5. Testing Enhancements

**Types of test contributions:**
- **Unit tests**: Individual component testing
- **Integration tests**: End-to-end scenarios
- **Golden file tests**: Expected output validation
- **Performance tests**: Speed and memory benchmarks
- **Regression tests**: Prevent breaking changes

### 6. Tooling and Infrastructure

**Areas for contribution:**
- **VS Code extension**: Syntax highlighting, debugging
- **Build improvements**: Faster compilation, caching
- **CI/CD enhancements**: Better testing, deployment
- **Developer tools**: Profilers, analyzers

## Testing Guidelines

### Test Categories

1. **Unit Tests** - Test individual components in isolation
2. **Integration Tests** - Test complete transpilation pipeline
3. **Golden File Tests** - Verify exact output matches expected
4. **Performance Tests** - Measure speed and memory usage
5. **Regression Tests** - Prevent breaking changes

### Writing Tests

#### Unit Test Example

```csharp
[TestFixture]
public class LexerTests
{
    [Test]
    public void Lexer_ShouldTokenizeStringLiterals()
    {
        // Arrange
        var source = "\"Hello, World!\"";
        var lexer = new CadenzaLexer(source);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.That(tokens.Count, Is.EqualTo(2)); // String + EOF
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Value, Is.EqualTo("Hello, World!"));
    }

    [TestCase("42", TokenType.Number, "42")]
    [TestCase("identifier", TokenType.Identifier, "identifier")]
    [TestCase("function", TokenType.Function, "function")]
    public void Lexer_ShouldTokenizeCorrectly(string input, TokenType expectedType, string expectedValue)
    {
        var lexer = new CadenzaLexer(input);
        var tokens = lexer.Tokenize();
        
        Assert.That(tokens[0].Type, Is.EqualTo(expectedType));
        Assert.That(tokens[0].Value, Is.EqualTo(expectedValue));
    }
}
```

#### Integration Test Example

```csharp
[TestFixture]
public class TranspilationTests
{
    [Test]
    public async Task Transpiler_ShouldTranspileSimpleFunction()
    {
        // Arrange
        var flowLangCode = """
            pure function add(a: int, b: int) -> int {
                return a + b
            }
            """;

        var transpiler = new CadenzaTranspiler();

        // Act
        var csharpCode = transpiler.TranspileToCS(flowLangCode);

        // Assert
        Assert.That(csharpCode, Contains.Substring("public static int add"));
        Assert.That(csharpCode, Contains.Substring("return a + b;"));
        
        // Verify generated C# compiles
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(csharpCode));
        var diagnostics = compilation.GetDiagnostics();
        Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
    }
}
```

#### Golden File Test Example

```csharp
[TestFixture]
public class GoldenFileTests
{
    [Test]
    public async Task GoldenFile_BasicFunctions()
    {
        // Arrange
        var inputPath = "tests/golden/inputs/basic_functions.cdz";
        var expectedPath = "tests/golden/expected/basic_functions.cs";
        
        var inputCode = await File.ReadAllTextAsync(inputPath);
        var expectedCode = await File.ReadAllTextAsync(expectedPath);
        
        var transpiler = new CadenzaTranspiler();

        // Act
        var actualCode = transpiler.TranspileToCS(inputCode);

        // Assert
        Assert.That(NormalizeWhitespace(actualCode), Is.EqualTo(NormalizeWhitespace(expectedCode)));
    }

    private static string NormalizeWhitespace(string code)
    {
        return CSharpSyntaxTree.ParseText(code)
            .GetRoot()
            .NormalizeWhitespace()
            .ToFullString();
    }
}
```

### Test Data Organization

```
tests/
├── golden/
│   ├── inputs/
│   │   ├── basic_functions.cdz
│   │   ├── result_types.cdz
│   │   └── effect_system.cdz
│   └── expected/
│       ├── basic_functions.cs
│       ├── result_types.cs
│       └── effect_system.cs
├── data/
│   ├── valid_programs/      # Valid Cadenza programs
│   ├── invalid_programs/    # Programs that should fail
│   └── edge_cases/         # Boundary conditions
└── fixtures/
    ├── large_programs/     # For performance testing
    └── real_world/         # Real-world examples
```

## Documentation Guidelines

### Writing Style

1. **Be clear and concise** - Avoid jargon
2. **Use examples** - Show, don't just tell
3. **Be consistent** - Follow established patterns
4. **Consider the audience** - Beginners vs experts
5. **Keep it up-to-date** - Update docs with code changes

### Documentation Types

#### API Documentation

```csharp
/// <summary>
/// Transpiles Cadenza source code to C# code.
/// </summary>
/// <param name="flowLangSource">The Cadenza source code to transpile</param>
/// <returns>Generated C# code as a string</returns>
/// <exception cref="LexicalException">Thrown when source contains invalid tokens</exception>
/// <exception cref="SyntaxException">Thrown when source contains syntax errors</exception>
/// <example>
/// <code>
/// var transpiler = new CadenzaTranspiler();
/// var csharpCode = transpiler.TranspileToCS("pure function add(a: int, b: int) -> int { return a + b }");
/// </code>
/// </example>
public string TranspileToCS(string flowLangSource)
```

#### User Documentation

```markdown
# Function Declarations

Functions are the primary building blocks in Cadenza. They can be pure (no side effects) or declare explicit effects.

## Pure Functions

Pure functions have no side effects and always return the same output for the same input:

```cadenza
pure function add(a: int, b: int) -> int {
    return a + b
}
```

## Functions with Effects

Functions can declare side effects using the `uses` keyword:

```cadenza
function saveUser(name: string) uses [Database] -> Result<int, string> {
    // Implementation would save to database
    return Ok(42)
}
```
```

### Documentation Standards

1. **Use markdown** for all documentation files
2. **Include code examples** for all features
3. **Provide both Cadenza and C#** comparisons when helpful
4. **Link between related sections**
5. **Use tables** for reference information
6. **Include troubleshooting** for common issues

## Release Process

### Version Numbering

Cadenza follows [Semantic Versioning](https://semver.org/):

- **MAJOR.MINOR.PATCH** (e.g., 1.2.3)
- **MAJOR**: Breaking language changes
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

### Release Workflow

1. **Feature Development**
   - Features developed in feature branches
   - Merged to main via pull requests
   - All tests must pass

2. **Pre-release Testing**
   - Integration testing on main
   - Performance regression testing
   - Documentation review

3. **Release Preparation**
   - Update version numbers
   - Update CHANGELOG.md
   - Create release notes
   - Tag release in Git

4. **Release Deployment**
   - Build release artifacts
   - Publish to package manager
   - Update documentation site
   - Announce release

### Changelog Format

```markdown
# Changelog

## [1.2.0] - 2024-03-15

### Added
- New string interpolation features
- Support for nested module imports
- Enhanced error messages

### Changed
- Improved performance of large file transpilation
- Updated CLI command structure

### Fixed
- Fixed bug in error propagation with nested calls
- Corrected module export resolution

### Deprecated
- Legacy --input CLI mode (use 'cadenzac run' instead)

## [1.1.0] - 2024-02-01
...
```

## Community Guidelines

### Code of Conduct

1. **Be respectful** - Treat everyone with respect
2. **Be inclusive** - Welcome newcomers and diverse perspectives
3. **Be constructive** - Provide helpful feedback
4. **Be patient** - Remember that everyone is learning
5. **Be collaborative** - Work together toward common goals

### Communication Channels

- **GitHub Issues**: Bug reports, feature requests
- **GitHub Discussions**: General questions, ideas
- **Pull Requests**: Code review, collaboration
- **Documentation**: Written guides and references

### Getting Help

1. **Check documentation** first
2. **Search existing issues** for similar problems
3. **Create detailed issue** if problem not found
4. **Be patient** for responses
5. **Help others** when you can

### Recognition

Contributors are recognized through:

- **Git commit history** - Permanent record
- **Contributors file** - Listed in repository
- **Release notes** - Major contributions highlighted
- **Community mentions** - Social media, blog posts

## Development Workflow

### Daily Development

1. **Sync with upstream**:
   ```bash
   git fetch upstream
   git checkout main
   git merge upstream/main
   ```

2. **Create feature branch**:
   ```bash
   git checkout -b feature/my-feature
   ```

3. **Make changes incrementally**:
   ```bash
   # Make small changes
   git add .
   git commit -m "Add: Initial implementation of feature"
   
   # Continue development
   git add .
   git commit -m "Test: Add unit tests for new feature"
   
   # Finalize
   git add .
   git commit -m "Docs: Add documentation for new feature"
   ```

4. **Test thoroughly**:
   ```bash
   dotnet test
   dotnet run --project src/cadenzac.csproj -- test
   ```

5. **Create pull request** when ready

### Code Review Process

**As an Author:**
1. Self-review your changes before submitting
2. Write clear PR description
3. Respond to feedback promptly
4. Update code based on suggestions
5. Keep PR scope focused

**As a Reviewer:**
1. Review code for correctness and style
2. Test changes locally if significant
3. Provide constructive feedback
4. Approve when satisfied
5. Be respectful and helpful

### Debugging Tips

1. **Use the debugger** for complex issues
2. **Add logging** to understand flow
3. **Write failing tests** to reproduce bugs
4. **Check generated C#** for code generation issues
5. **Use profiler** for performance problems

## Advanced Topics

### Compiler Architecture

Understanding the transpiler architecture helps with complex contributions:

1. **Lexical Analysis**: Text → Tokens
2. **Syntax Analysis**: Tokens → AST
3. **Semantic Analysis**: AST validation
4. **Code Generation**: AST → C# Syntax Tree
5. **Output**: C# Syntax Tree → String

### Performance Optimization

Areas for performance improvement:

1. **Lexer optimization**: Faster tokenization
2. **Parser optimization**: Efficient AST construction
3. **Memory usage**: Reduce allocations
4. **Parallel processing**: Multi-threaded compilation
5. **Caching**: Avoid re-parsing unchanged files

### Language Design Decisions

When proposing language changes, consider:

1. **Consistency** with existing features
2. **Learning curve** for new users
3. **Tooling support** implications
4. **Performance impact**
5. **Migration path** for existing code

## Conclusion

Contributing to Cadenza is a great way to:

- **Learn compiler construction**
- **Improve a useful tool**
- **Help the developer community**
- **Build your programming skills**
- **Collaborate with other developers**

Whether you're fixing a small bug, adding a major feature, or improving documentation, your contributions are valuable and appreciated.

### Quick Start Checklist

- [ ] Fork and clone the repository
- [ ] Set up development environment
- [ ] Build and run tests successfully
- [ ] Read the codebase and documentation
- [ ] Find an issue or area to work on
- [ ] Create a feature branch
- [ ] Make your changes with tests
- [ ] Submit a pull request
- [ ] Respond to feedback
- [ ] Celebrate your contribution! 🎉

**Happy contributing!** 🚀

For questions about contributing, please:
- Check the documentation first
- Search existing GitHub issues
- Create a new issue for discussion
- Join the community discussion

Thank you for helping make Cadenza better!