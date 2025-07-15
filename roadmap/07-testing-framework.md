# Testing Framework Implementation

## Overview
Create a comprehensive testing framework for the Cadenza transpiler with unit tests, integration tests, and golden file tests.

## Goals
- Add unit tests for lexer, parser, and code generator
- Create integration tests for full transpilation pipeline
- Implement golden file tests for code generation
- Add performance benchmarks
- Set up automated testing

## Technical Requirements

### 1. Testing Infrastructure
- Create test project structure
- Add test discovery and execution
- Set up test assertions and reporting
- Add test data management

### 2. Test Categories
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end transpilation
- **Golden File Tests**: Code generation verification
- **Performance Tests**: Transpilation speed benchmarks
- **Regression Tests**: Prevent breaking changes

### 3. Test Organization
```
tests/
├── unit/
│   ├── lexer_tests.cs
│   ├── parser_tests.cs
│   └── codegen_tests.cs
├── integration/
│   ├── transpilation_tests.cs
│   └── examples_tests.cs
├── golden/
│   ├── inputs/
│   └── expected/
└── performance/
    └── benchmarks.cs
```

## Example Test Cases

### Unit Tests
```csharp
[Test]
public void Lexer_ShouldTokenizeFunction()
{
    var lexer = new CadenzaLexer("function test() -> int");
    var tokens = lexer.Tokenize();
    
    Assert.AreEqual(TokenType.Function, tokens[0].Type);
    Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
    Assert.AreEqual("test", tokens[1].Value);
}

[Test]
public void Parser_ShouldParseFunctionDeclaration()
{
    var tokens = new List<Token> { /* ... */ };
    var parser = new CadenzaParser(tokens);
    var ast = parser.Parse();
    
    Assert.IsInstanceOf<FunctionDeclaration>(ast.Statements[0]);
}
```

### Integration Tests
```csharp
[Test]
public void Transpiler_ShouldTranspileSimpleFunction()
{
    var input = "function add(a: int, b: int) -> int { return a + b }";
    var transpiler = new CadenzaTranspiler();
    var output = transpiler.TranspileToCS(input);
    
    Assert.Contains("public static int add(int a, int b)", output);
    Assert.Contains("return a + b;", output);
}
```

### Golden File Tests
```csharp
[Test]
public void GoldenFile_BasicFunctions()
{
    var inputFile = "tests/golden/inputs/basic_functions.cdz";
    var expectedFile = "tests/golden/expected/basic_functions.cs";
    
    var transpiler = new CadenzaTranspiler();
    var input = File.ReadAllText(inputFile);
    var actual = transpiler.TranspileToCS(input);
    var expected = File.ReadAllText(expectedFile);
    
    Assert.AreEqual(expected.Trim(), actual.Trim());
}
```

## Test Data Examples

### Golden File Input (basic_functions.cdz)
```cadenza
function add(a: int, b: int) -> int {
    return a + b
}

function multiply(x: int, y: int) -> int {
    return x * y
}

function is_positive(n: int) -> bool {
    return n > 0
}
```

### Golden File Expected (basic_functions.cs)
```csharp
public static int add(int a, int b)
{
    return a + b;
}

public static int multiply(int x, int y)
{
    return x * y;
}

public static bool is_positive(int n)
{
    return n > 0;
}
```

## Implementation Tasks
1. Create test project structure
2. Set up test framework (NUnit/xUnit)
3. Add lexer unit tests
4. Add parser unit tests
5. Add code generator unit tests
6. Create integration test framework
7. Add golden file test system
8. Create performance benchmarks
9. Add test data and examples
10. Set up automated test execution
11. Add test reporting and coverage
12. Create test documentation

## Success Criteria
- Comprehensive test coverage (>90%)
- All tests pass consistently
- Golden file tests verify output correctness
- Performance benchmarks track regressions
- Easy to add new tests for new features
- Automated test execution works

## Dependencies
- Current transpiler infrastructure
- All language features (for comprehensive testing)
- Test framework setup (NUnit/xUnit)
- File system operations for golden files