# Cadenza Testing Guide

This guide covers the comprehensive testing framework for Cadenza, including how to write tests, run the test suite, and contribute test improvements.

## Table of Contents

1. [Testing Overview](#testing-overview)
2. [Test Categories](#test-categories)
3. [Running Tests](#running-tests)
4. [Writing Unit Tests](#writing-unit-tests)
5. [Integration Testing](#integration-testing)
6. [Golden File Testing](#golden-file-testing)
7. [Performance Testing](#performance-testing)
8. [Regression Testing](#regression-testing)
9. [Contributing Tests](#contributing-tests)
10. [Test Data Management](#test-data-management)

## Testing Overview

Cadenza uses a comprehensive testing framework built on .NET's testing infrastructure with NUnit. The framework provides multiple types of tests to ensure the transpiler's correctness, performance, and reliability.

### Test Framework Architecture

```
Cadenza.Tests/
├── unit/              # Component-level tests
├── integration/       # End-to-end tests
├── golden/           # Expected output validation
├── performance/      # Speed and memory benchmarks
├── regression/       # Historical test cases
├── framework/        # Test utilities and discovery
└── reporting/        # Test results and coverage
```

### Key Testing Principles

1. **Comprehensive Coverage**: >90% unit test coverage, >80% overall coverage
2. **Multiple Test Types**: Unit, integration, golden file, performance, and regression
3. **Automated Validation**: All tests run in CI/CD pipeline
4. **Clear Organization**: Tests grouped by functionality and type
5. **Maintainable Tests**: Easy to understand, modify, and extend

## Test Categories

### 1. Unit Tests

Test individual components in isolation:

- **Lexer Tests**: Token generation and validation
- **Parser Tests**: AST construction and syntax validation
- **Code Generator Tests**: C# output generation
- **Effect System Tests**: Effect validation and tracking
- **Module System Tests**: Import/export resolution

### 2. Integration Tests

Test complete transpilation pipeline:

- **End-to-end transpilation**: Cadenza → C# → compilation
- **Cross-component interaction**: Multiple components working together
- **Real-world scenarios**: Practical use cases and workflows

### 3. Golden File Tests

Validate exact output matches expected results:

- **Input/Expected pairs**: Cadenza source and expected C# output
- **Feature coverage**: All language constructs tested
- **Regression prevention**: Changes don't break existing functionality

### 4. Performance Tests

Measure and track performance metrics:

- **Compilation speed**: Time to transpile various file sizes
- **Memory usage**: Memory consumption during transpilation
- **Scalability**: Performance with varying input complexity

### 5. Regression Tests

Prevent breaking changes:

- **Historical test cases**: Known good examples
- **Bug reproduction**: Tests for previously fixed issues
- **Backwards compatibility**: Ensure existing code still works

## Running Tests

### Prerequisites

- .NET 8.0 SDK or later
- Cadenza source code built successfully
- All NuGet packages restored

### Running All Tests

```bash
# Navigate to test directory
cd tests

# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run with coverage collection
dotnet test --collect:"XPlat Code Coverage"
```

### Running Specific Test Categories

```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Unit"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Integration"

# Golden file tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Golden"

# Performance tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Performance"

# Regression tests only
dotnet test --filter "FullyQualifiedName~Cadenza.Tests.Regression"
```

### Running Individual Test Classes

```bash
# Run specific test class
dotnet test --filter "ClassName=LexerTests"

# Run specific test method
dotnet test --filter "MethodName=Lexer_ShouldTokenizeNumbers"

# Run tests with specific attribute
dotnet test --filter "Category=FastTests"
```

### Performance Test Execution

Performance tests require special handling:

```bash
# Run in Release mode for accurate measurements
dotnet test --configuration Release --filter "Performance"

# Run with detailed timing
dotnet test --logger "console;verbosity=detailed" --filter "Performance"
```

## Writing Unit Tests

### Test Class Structure

```csharp
using NUnit.Framework;
using Cadenza;

namespace Cadenza.Tests.Unit
{
    [TestFixture]
    [Category("Unit")]
    public class LexerTests
    {
        private CadenzaLexer _lexer;

        [SetUp]
        public void SetUp()
        {
            // Setup run before each test
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup run after each test
        }

        [Test]
        public void Lexer_ShouldTokenizeNumbers()
        {
            // Arrange
            var source = "42";
            _lexer = new CadenzaLexer(source);

            // Act
            var tokens = _lexer.Tokenize();

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // Number + EOF
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[0].Value, Is.EqualTo("42"));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.EOF));
        }

        [TestCase("42", TokenType.Number, "42")]
        [TestCase("identifier", TokenType.Identifier, "identifier")]
        [TestCase("function", TokenType.Function, "function")]
        [TestCase("\"hello\"", TokenType.String, "hello")]
        public void Lexer_ShouldTokenizeCorrectly(string input, TokenType expectedType, string expectedValue)
        {
            // Arrange
            _lexer = new CadenzaLexer(input);

            // Act
            var tokens = _lexer.Tokenize();

            // Assert
            Assert.That(tokens[0].Type, Is.EqualTo(expectedType));
            Assert.That(tokens[0].Value, Is.EqualTo(expectedValue));
        }
    }
}
```

### Parser Test Examples

```csharp
[TestFixture]
[Category("Unit")]
public class ParserTests
{
    [Test]
    public void Parser_ShouldParseFunctionDeclaration()
    {
        // Arrange
        var source = "pure function add(a: int, b: int) -> int { return a + b }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.Tokenize();
        var parser = new CadenzaParser(tokens);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.That(program.Statements.Count, Is.EqualTo(1));
        
        var funcDecl = program.Statements[0] as FunctionDeclaration;
        Assert.That(funcDecl, Is.Not.Null);
        Assert.That(funcDecl.Name, Is.EqualTo("add"));
        Assert.That(funcDecl.IsPure, Is.True);
        Assert.That(funcDecl.Parameters.Count, Is.EqualTo(2));
        Assert.That(funcDecl.ReturnType, Is.EqualTo("int"));
    }

    [Test]
    public void Parser_ShouldParseResultType()
    {
        // Arrange
        var source = "function divide(a: int, b: int) -> Result<int, string> { return Ok(a / b) }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.Tokenize();
        var parser = new CadenzaParser(tokens);

        // Act
        var program = parser.Parse();

        // Assert
        var funcDecl = program.Statements[0] as FunctionDeclaration;
        Assert.That(funcDecl.ReturnType, Is.EqualTo("Result<int, string>"));
    }

    [Test]
    public void Parser_ShouldThrowErrorOnInvalidSyntax()
    {
        // Arrange
        var source = "function missing_params() -> int { return 42 }"; // Missing parameter list
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.Tokenize();
        var parser = new CadenzaParser(tokens);

        // Act & Assert
        Assert.Throws<Exception>(() => parser.Parse());
    }
}
```

### Code Generator Test Examples

```csharp
[TestFixture]
[Category("Unit")]
public class CodeGeneratorTests
{
    private CSharpGenerator _generator;

    [SetUp]
    public void SetUp()
    {
        _generator = new CSharpGenerator();
    }

    [Test]
    public void CodeGenerator_ShouldGenerateSimpleFunction()
    {
        // Arrange
        var func = new FunctionDeclaration(
            "add",
            new List<Parameter> 
            { 
                new("a", "int"), 
                new("b", "int") 
            },
            "int",
            new List<ASTNode> 
            { 
                new ReturnStatement(
                    new BinaryExpression(
                        new Identifier("a"), 
                        "+", 
                        new Identifier("b")
                    )
                ) 
            },
            null,
            true
        );
        var program = new Program(new List<ASTNode> { func });

        // Act
        var syntaxTree = _generator.GenerateFromAST(program);
        var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

        // Assert
        Assert.That(code, Contains.Substring("public static int add"));
        Assert.That(code, Contains.Substring("return a + b;"));
        Assert.That(code, Contains.Substring("Pure function"));
    }

    [Test]
    public void CodeGenerator_ShouldGenerateResultClass()
    {
        // Arrange
        var func = new FunctionDeclaration(
            "divide",
            new List<Parameter> { new("a", "int"), new("b", "int") },
            "Result<int, string>",
            new List<ASTNode> { new ReturnStatement(new OkExpression(new NumberLiteral(42))) }
        );
        var program = new Program(new List<ASTNode> { func });

        // Act
        var syntaxTree = _generator.GenerateFromAST(program);
        var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

        // Assert
        Assert.That(code, Contains.Substring("public class Result"));
        Assert.That(code, Contains.Substring("public static Result<T, E> Ok"));
        Assert.That(code, Contains.Substring("public static Result<T, E> Error"));
    }
}
```

## Integration Testing

### End-to-End Transpilation Tests

```csharp
[TestFixture]
[Category("Integration")]
public class TranspilationTests
{
    private CadenzaTranspiler _transpiler;

    [SetUp]
    public void SetUp()
    {
        _transpiler = new CadenzaTranspiler();
    }

    [Test]
    public async Task Transpiler_ShouldTranspileSimpleFunction()
    {
        // Arrange
        var flowLangCode = """
            pure function add(a: int, b: int) -> int {
                return a + b
            }
            """;

        // Act
        var csharpCode = _transpiler.TranspileToCS(flowLangCode);

        // Assert
        Assert.That(csharpCode, Contains.Substring("public static int add"));
        Assert.That(csharpCode, Contains.Substring("return a + b;"));
        
        // Verify generated C# compiles
        await VerifyGeneratedCodeCompiles(csharpCode);
    }

    [Test]
    public async Task Transpiler_ShouldTranspileResultTypes()
    {
        // Arrange
        var flowLangCode = """
            function safeDivide(a: int, b: int) -> Result<int, string> {
                if b == 0 {
                    return Error("Division by zero")
                }
                return Ok(a / b)
            }
            """;

        // Act
        var csharpCode = _transpiler.TranspileToCS(flowLangCode);

        // Assert
        Assert.That(csharpCode, Contains.Substring("Result<int, string>"));
        Assert.That(csharpCode, Contains.Substring("Result<int, string>.Error"));
        Assert.That(csharpCode, Contains.Substring("Result<int, string>.Ok"));
        
        await VerifyGeneratedCodeCompiles(csharpCode);
    }

    [Test]
    public async Task Transpiler_ShouldTranspileEffectSystem()
    {
        // Arrange
        var flowLangCode = """
            function saveUser(name: string) uses [Database, Logging] -> Result<int, string> {
                return Ok(42)
            }
            """;

        // Act
        var csharpCode = _transpiler.TranspileToCS(flowLangCode);

        // Assert
        Assert.That(csharpCode, Contains.Substring("Database, Logging"));
        Assert.That(csharpCode, Contains.Substring("Effects: Database, Logging"));
        
        await VerifyGeneratedCodeCompiles(csharpCode);
    }

    private async Task VerifyGeneratedCodeCompiles(string csharpCode)
    {
        var compilation = CSharpCompilation.Create("test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(csharpCode));
        
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated C# code has compilation errors:\n{errorMessages}\n\nGenerated code:\n{csharpCode}");
        }
    }
}
```

### CLI Integration Tests

```csharp
[TestFixture]
[Category("Integration")]
public class CliIntegrationTests
{
    private string _testProjectDir;

    [SetUp]
    public void SetUp()
    {
        _testProjectDir = Path.Combine(Path.GetTempPath(), "cadenza_test_" + Guid.NewGuid());
        Directory.CreateDirectory(_testProjectDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testProjectDir))
        {
            Directory.Delete(_testProjectDir, true);
        }
    }

    [Test]
    public async Task Cli_NewCommand_ShouldCreateProject()
    {
        // Arrange
        var projectName = "test-project";

        // Act
        var result = await RunCliCommand($"new {projectName}");

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(Directory.Exists(Path.Combine(_testProjectDir, projectName)), Is.True);
        Assert.That(File.Exists(Path.Combine(_testProjectDir, projectName, "cadenzac.json")), Is.True);
        Assert.That(File.Exists(Path.Combine(_testProjectDir, projectName, "src", "main.cdz")), Is.True);
    }

    [Test]
    public async Task Cli_BuildCommand_ShouldTranspileFiles()
    {
        // Arrange
        await RunCliCommand("new test-project");
        var projectDir = Path.Combine(_testProjectDir, "test-project");
        
        // Act
        var result = await RunCliCommand("build", projectDir);

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(Directory.Exists(Path.Combine(projectDir, "build")), Is.True);
        Assert.That(File.Exists(Path.Combine(projectDir, "build", "main.cs")), Is.True);
    }

    private async Task<(int ExitCode, string Output)> RunCliCommand(string command, string workingDirectory = null)
    {
        workingDirectory ??= _testProjectDir;
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project {GetFlowcProjectPath()} -- {command}",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        await process.WaitForExitAsync();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        return (process.ExitCode, output + error);
    }

    private string GetFlowcProjectPath()
    {
        // Return path to cadenzac.csproj relative to test directory
        return Path.Combine("..", "..", "src", "cadenzac.csproj");
    }
}
```

## Golden File Testing

### Golden File Structure

```
tests/golden/
├── inputs/                    # Cadenza source files
│   ├── basic_functions.cdz
│   ├── result_types.cdz
│   ├── effect_system.cdz
│   ├── modules.cdz
│   ├── string_interpolation.cdz
│   └── control_flow.cdz
└── expected/                  # Expected C# output
    ├── basic_functions.cs
    ├── result_types.cs
    ├── effect_system.cs
    ├── modules.cs
    ├── string_interpolation.cs
    └── control_flow.cs
```

### Golden File Test Implementation

```csharp
[TestFixture]
[Category("Golden")]
public class GoldenFileTests
{
    private CadenzaTranspiler _transpiler;
    private string _inputDir;
    private string _expectedDir;

    [SetUp]
    public void SetUp()
    {
        _transpiler = new CadenzaTranspiler();
        _inputDir = Path.Combine("golden", "inputs");
        _expectedDir = Path.Combine("golden", "expected");
    }

    [TestCase("basic_functions")]
    [TestCase("result_types")]
    [TestCase("effect_system")]
    [TestCase("modules")]
    [TestCase("string_interpolation")]
    [TestCase("control_flow")]
    public async Task GoldenFile_ShouldMatchExpectedOutput(string testName)
    {
        // Arrange
        var inputPath = Path.Combine(_inputDir, $"{testName}.cdz");
        var expectedPath = Path.Combine(_expectedDir, $"{testName}.cs");
        
        Assert.That(File.Exists(inputPath), Is.True, $"Input file not found: {inputPath}");
        Assert.That(File.Exists(expectedPath), Is.True, $"Expected file not found: {expectedPath}");
        
        var inputCode = await File.ReadAllTextAsync(inputPath);
        var expectedCode = await File.ReadAllTextAsync(expectedPath);

        // Act
        var actualCode = _transpiler.TranspileToCS(inputCode);

        // Assert
        var normalizedExpected = NormalizeCode(expectedCode);
        var normalizedActual = NormalizeCode(actualCode);
        
        Assert.That(normalizedActual, Is.EqualTo(normalizedExpected), 
            $"Generated code doesn't match expected output for {testName}");
    }

    [Test]
    public async Task GoldenFile_AllTestFilesExist()
    {
        // Verify that every input file has a corresponding expected file
        var inputFiles = Directory.GetFiles(_inputDir, "*.cdz");
        
        foreach (var inputFile in inputFiles)
        {
            var baseName = Path.GetFileNameWithoutExtension(inputFile);
            var expectedFile = Path.Combine(_expectedDir, $"{baseName}.cs");
            
            Assert.That(File.Exists(expectedFile), Is.True, 
                $"Missing expected file for {baseName}: {expectedFile}");
        }
    }

    private string NormalizeCode(string code)
    {
        // Parse and reformat to normalize whitespace and formatting
        return CSharpSyntaxTree.ParseText(code)
            .GetRoot()
            .NormalizeWhitespace()
            .ToFullString();
    }
}
```

### Creating Golden File Tests

1. **Create input file**:
   ```cadenza
   // tests/golden/inputs/new_feature.cdz
   pure function example(x: int) -> int {
       return x * 2
   }
   ```

2. **Generate expected output**:
   ```bash
   dotnet run --project src/cadenzac.csproj -- run tests/golden/inputs/new_feature.cdz
   # Copy generated output to tests/golden/expected/new_feature.cs
   ```

3. **Add test case**:
   ```csharp
   [TestCase("new_feature")]
   public async Task GoldenFile_ShouldMatchExpectedOutput(string testName)
   ```

## Performance Testing

### Benchmark Tests

```csharp
[TestFixture]
[Category("Performance")]
public class PerformanceTests
{
    private CadenzaTranspiler _transpiler;

    [SetUp]
    public void SetUp()
    {
        _transpiler = new CadenzaTranspiler();
    }

    [Test]
    public void Performance_SimpleFunction_ShouldBeUnder10ms()
    {
        // Arrange
        var source = "pure function add(a: int, b: int) -> int { return a + b }";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _transpiler.TranspileToCS(source);

        // Assert
        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10), 
            "Simple function transpilation should complete under 10ms");
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Performance_ComplexFunction_ShouldBeUnder25ms()
    {
        // Arrange
        var source = GenerateComplexFunction();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _transpiler.TranspileToCS(source);

        // Assert
        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(25), 
            "Complex function transpilation should complete under 25ms");
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Performance_LargeProgram_ShouldBeUnder500ms()
    {
        // Arrange
        var source = GenerateLargeProgram(100); // 100 functions
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _transpiler.TranspileToCS(source);

        // Assert
        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500), 
            "Large program transpilation should complete under 500ms");
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Performance_MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        var source = GenerateLargeProgram(50);
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var result = _transpiler.TranspileToCS(source);
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryUsed = finalMemory - initialMemory;
        Assert.That(memoryUsed, Is.LessThan(10 * 1024 * 1024), // 10MB
            "Memory usage should be under 10MB for moderate-sized programs");
    }

    private string GenerateComplexFunction()
    {
        return """
            function complexCalculation(a: int, b: int, c: int) -> Result<int, string> {
                guard a >= 0 else {
                    return Error("a must be non-negative")
                }
                
                guard b >= 0 else {
                    return Error("b must be non-negative")
                }
                
                let step1 = a + b
                let step2 = step1 * c
                let step3 = step2 / 2
                
                if step3 > 100 {
                    return Ok(step3)
                } else if step3 > 50 {
                    return Ok(step3 * 2)
                } else {
                    return Ok(step3 * 3)
                }
            }
            """;
    }

    private string GenerateLargeProgram(int functionCount)
    {
        var sb = new StringBuilder();
        
        for (int i = 0; i < functionCount; i++)
        {
            sb.AppendLine($"""
                pure function func{i}(x: int) -> int {{
                    return x + {i}
                }}
                """);
        }
        
        return sb.ToString();
    }
}
```

## Regression Testing

### Historical Test Cases

```csharp
[TestFixture]
[Category("Regression")]
public class RegressionTests
{
    private CadenzaTranspiler _transpiler;

    [SetUp]
    public void SetUp()
    {
        _transpiler = new CadenzaTranspiler();
    }

    [Test]
    public void Regression_Issue123_StringInterpolationWithNestedBraces()
    {
        // This test ensures that issue #123 (hypothetical) doesn't regress
        var source = """
            function formatUser(name: string, age: int) -> string {
                return $"User: {name} (age: {age})"
            }
            """;

        // Should not throw exception (was failing before fix)
        Assert.DoesNotThrow(() => _transpiler.TranspileToCS(source));
        
        var result = _transpiler.TranspileToCS(source);
        Assert.That(result, Contains.Substring("string.Format"));
    }

    [Test]
    public void Regression_Issue456_EffectPropagationInModules()
    {
        // Test for issue #456: Effect propagation wasn't working correctly in modules
        var source = """
            module TestModule {
                function dbOperation() uses [Database] -> Result<int, string> {
                    return Ok(42)
                }
                
                function combinedOperation() uses [Database, Logging] -> Result<int, string> {
                    let result = dbOperation()?
                    return Ok(result)
                }
                
                export {combinedOperation}
            }
            """;

        Assert.DoesNotThrow(() => _transpiler.TranspileToCS(source));
        
        var result = _transpiler.TranspileToCS(source);
        Assert.That(result, Contains.Substring("Database, Logging"));
    }

    [Test]
    public void Regression_ErrorPropagationInLetStatements()
    {
        // Ensure error propagation in let statements generates correct C#
        var source = """
            function complexFlow() -> Result<int, string> {
                let step1 = operation1()?
                let step2 = operation2(step1)?
                return Ok(step2)
            }
            
            function operation1() -> Result<int, string> {
                return Ok(10)
            }
            
            function operation2(x: int) -> Result<int, string> {
                return Ok(x * 2)
            }
            """;

        var result = _transpiler.TranspileToCS(source);
        
        // Should generate proper error checking code
        Assert.That(result, Contains.Substring("if ("));
        Assert.That(result, Contains.Substring("IsError"));
        Assert.That(result, Contains.Substring("return"));
    }
}
```

## Contributing Tests

### Test Contribution Guidelines

1. **Choose the right test type**:
   - **Unit tests**: For component-level functionality
   - **Integration tests**: For end-to-end scenarios
   - **Golden file tests**: For new language features
   - **Performance tests**: For optimization work
   - **Regression tests**: For bug fixes

2. **Follow naming conventions**:
   ```csharp
   // Pattern: Component_Should[Behavior]_When[Condition]
   [Test]
   public void Lexer_ShouldTokenizeNumbers_WhenGivenValidInput()
   
   [Test]
   public void Parser_ShouldThrowException_WhenSyntaxIsInvalid()
   ```

3. **Use descriptive test data**:
   ```csharp
   [TestCase("42", TokenType.Number, "Simple integer")]
   [TestCase("\"hello\"", TokenType.String, "Simple string")]
   [TestCase("function", TokenType.Function, "Function keyword")]
   public void Lexer_ShouldTokenize(string input, TokenType expected, string description)
   ```

4. **Include edge cases**:
   ```csharp
   [TestCase("")]           // Empty input
   [TestCase(" ")]          // Whitespace only
   [TestCase("\n")]         // Newline only
   [TestCase("/**/")]       // Empty comment
   ```

### Adding New Test Categories

1. **Create test directory**:
   ```bash
   mkdir tests/new_category
   ```

2. **Create test class**:
   ```csharp
   [TestFixture]
   [Category("NewCategory")]
   public class NewCategoryTests
   {
       // Test methods
   }
   ```

3. **Update test filters**:
   ```bash
   # Add to CI/CD pipeline
   dotnet test --filter "Category=NewCategory"
   ```

### Test Data Creation

1. **Input files**: Create in appropriate `inputs/` directory
2. **Expected outputs**: Generate and verify manually
3. **Test fixtures**: Use realistic, representative data
4. **Edge cases**: Include boundary conditions and error cases

## Test Data Management

### Organizing Test Data

```
tests/
├── data/
│   ├── valid_programs/        # Valid Cadenza programs
│   ├── invalid_programs/      # Programs that should fail
│   ├── edge_cases/           # Boundary conditions
│   └── large_programs/       # For performance testing
├── fixtures/
│   ├── expected_outputs/     # Expected transpilation results
│   ├── error_messages/       # Expected error messages
│   └── benchmarks/          # Performance baseline data
└── golden/
    ├── inputs/              # Golden file test inputs
    └── expected/            # Golden file expected outputs
```

### Test Data Guidelines

1. **Keep data small**: Use minimal examples that demonstrate the feature
2. **Use realistic examples**: Based on actual use cases
3. **Document complex cases**: Add comments explaining the test purpose
4. **Version control**: Include all test data in Git
5. **Automate generation**: Script creation of large test datasets

### Maintaining Test Data

1. **Regular updates**: Keep test data current with language changes
2. **Cleanup obsolete data**: Remove tests for deprecated features
3. **Validate consistency**: Ensure input/expected pairs match
4. **Performance monitoring**: Track test execution time

## Best Practices

### Writing Effective Tests

1. **Test one thing**: Each test should verify a single behavior
2. **Use AAA pattern**: Arrange, Act, Assert
3. **Clear naming**: Test names should describe what's being tested
4. **Independent tests**: Tests shouldn't depend on each other
5. **Fast execution**: Keep test runtime reasonable

### Test Maintenance

1. **Regular review**: Check tests still provide value
2. **Update with changes**: Modify tests when features change
3. **Remove duplicates**: Eliminate redundant test coverage
4. **Monitor coverage**: Ensure adequate test coverage

### CI/CD Integration

```yaml
# Example GitHub Actions test workflow
name: Cadenza Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore tests/
    
    - name: Run unit tests
      run: dotnet test tests/ --filter "Category=Unit" --logger trx
    
    - name: Run integration tests
      run: dotnet test tests/ --filter "Category=Integration" --logger trx
    
    - name: Run golden file tests
      run: dotnet test tests/ --filter "Category=Golden" --logger trx
    
    - name: Run performance tests
      run: dotnet test tests/ --filter "Category=Performance" --configuration Release
    
    - name: Generate coverage report
      run: dotnet test tests/ --collect:"XPlat Code Coverage"
    
    - name: Upload test results
      uses: actions/upload-artifact@v2
      with:
        name: test-results
        path: TestResults
```

## Summary

The Cadenza testing framework provides comprehensive coverage through:

- **Multiple test types** for different validation needs
- **Automated execution** in CI/CD pipelines
- **Clear organization** for easy maintenance
- **Performance monitoring** to prevent regressions
- **Contribution guidelines** for community involvement

Whether you're fixing bugs, adding features, or optimizing performance, the testing framework ensures Cadenza remains reliable and high-quality. Follow the guidelines in this document to contribute effective tests that help maintain Cadenza's quality standards.