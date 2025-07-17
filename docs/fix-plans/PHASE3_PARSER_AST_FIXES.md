# Phase 3: Parser & AST Test Mismatches

## Context
The Cadenza project test suite has 85 failing tests out of 263 total tests. Phase 3 focuses on fixing parser test expectations that don't match the current parser implementation.

## Project Overview
- **Language**: Cadenza (compiles to C#)
- **Parser**: `src/Cadenza.Core/Parser.cs` - Recursive descent parser
- **AST**: `src/Cadenza.Core/Ast.cs` - Abstract syntax tree definitions
- **Key Issues**: Parser tests expect certain AST node types but get different ones

## Current Issues

### 1. Expression Type Mismatches
The parser has evolved but tests haven't been updated to match current behavior:

**Common Issue**: Tests expect `BinaryExpression` but parser returns `LogicalExpression`
- **Example**: `Parser_ShouldHandleComplexBooleanExpressions` expects `BinaryExpression` but gets `LogicalExpression`
- **Example**: `Parser_ShouldHandleOperatorPrecedence` expects `BinaryExpression` but gets different type

### 2. Null Reference Warnings
Several parser tests have null reference warnings:
- `tests/unit/ParserTests.cs:188,25` - Dereference of possibly null reference
- `tests/unit/ParserTests.cs:539,25` - Dereference of possibly null reference  
- `tests/unit/ParserTests.cs:593,25` - Dereference of possibly null reference
- `tests/unit/ParserTests.cs:727,25` - Dereference of possibly null reference

### 3. Parser Implementation Changes
The parser has been updated but tests reflect old behavior:
- Expression parsing now returns different AST node types
- Boolean expressions are parsed as `LogicalExpression` not `BinaryExpression`
- Operator precedence handling may have changed

## Current Parser Architecture

### AST Node Types (from `src/Cadenza.Core/Ast.cs`)
- `BinaryExpression` - Binary operations like `+`, `-`, `*`, `/`
- `LogicalExpression` - Logical operations like `&&`, `||` 
- `UnaryExpression` - Unary operations like `!`, `-`
- `MatchExpression` - Pattern matching (already implemented)
- `CallExpression` - Function calls
- `IfStatement` - Conditional statements

### Parser Methods (from `src/Cadenza.Core/Parser.cs`)
- `ParseExpression()` - Entry point for expression parsing
- `ParseLogicalOr()` - Handles `||` operations
- `ParseLogicalAnd()` - Handles `&&` operations
- `ParseEquality()` - Handles `==`, `!=` operations
- `ParseComparison()` - Handles `<`, `>`, `<=`, `>=` operations
- `ParseTerm()` - Handles `+`, `-` operations
- `ParseFactor()` - Handles `*`, `/` operations

## Tasks to Complete

### Task 1: Fix Expression Type Expectations
Update parser tests to expect the correct AST node types:

1. **Boolean Expression Tests**: Change expectations from `BinaryExpression` to `LogicalExpression`
2. **Operator Precedence Tests**: Update to match current parser behavior
3. **Complex Expression Tests**: Verify what the parser actually returns and update tests

### Task 2: Fix Null Reference Warnings
Add null checks and proper assertions:

1. **Line 188**: Add null check before accessing expression properties
2. **Line 539**: Add null check before accessing statement properties  
3. **Line 593**: Add null check before accessing node properties
4. **Line 727**: Add null check before accessing expression properties

### Task 3: Update Parser Test Assertions
Review all parser tests and update assertions to match current behavior:

1. **Expression Parsing Tests**: Verify return types match expectations
2. **Statement Parsing Tests**: Ensure proper AST node creation
3. **Error Handling Tests**: Update error message expectations if needed

### Task 4: Validate Parser Behavior
Run specific parser tests to understand current behavior:

1. Test boolean expressions and document actual return types
2. Test operator precedence and document actual AST structure
3. Test complex expressions and update test expectations

## Expected Files to Modify

1. `tests/unit/ParserTests.cs` - Primary file with parser test failures
2. Possibly `tests/unit/MatchExpressionTests.cs` - If match expression tests are affected
3. Any other parser-related test files

## Specific Failing Tests to Fix

Based on the earlier analysis:
- `Parser_ShouldHandleComplexBooleanExpressions` - BinaryExpression vs LogicalExpression
- `Parser_ShouldHandleOperatorPrecedence` - BinaryExpression vs LogicalExpression
- Other parser tests with type mismatches

## Implementation Strategy

### Step 1: Analyze Current Parser Behavior
```csharp
// Test what the parser actually returns
var source = "true && false || true";
var lexer = new CadenzaLexer(source);
var tokens = lexer.ScanTokens();
var parser = new CadenzaParser(tokens);
var ast = parser.Parse();
// Check the actual type of the expression
```

### Step 2: Update Test Expectations
```csharp
// Before:
Assert.That(returnStmt.Expression, Is.InstanceOf<BinaryExpression>());

// After:
Assert.That(returnStmt.Expression, Is.InstanceOf<LogicalExpression>());
```

### Step 3: Add Null Safety
```csharp
// Before:
var returnStmt = statements[0] as ReturnStatement;
Assert.That(returnStmt.Expression, Is.InstanceOf<BinaryExpression>());

// After:
var returnStmt = statements[0] as ReturnStatement;
Assert.That(returnStmt, Is.Not.Null);
Assert.That(returnStmt.Expression, Is.Not.Null);
Assert.That(returnStmt.Expression, Is.InstanceOf<LogicalExpression>());
```

## Success Criteria

1. ✅ All parser tests pass
2. ✅ No null reference warnings in parser tests
3. ✅ Test assertions match actual parser behavior
4. ✅ Expression type expectations are correct

## Reference Files

- **Parser Implementation**: `src/Cadenza.Core/Parser.cs`
- **AST Definitions**: `src/Cadenza.Core/Ast.cs`
- **Parser Tests**: `tests/unit/ParserTests.cs`
- **Language Reference**: `docs/language-fundamentals.md`

## Commands to Run

After implementation, verify with:
```bash
# Run parser tests specifically
dotnet test tests/Cadenza.Tests.csproj --filter "FullyQualifiedName~ParserTests"

# Run all unit tests
dotnet test tests/Cadenza.Tests.csproj --filter "FullyQualifiedName~unit"

# Check overall test status
dotnet test tests/Cadenza.Tests.csproj --logger "console;verbosity=normal"
```

## Implementation Notes

1. **Priority**: High - Parser is core to the language
2. **Dependencies**: None - can run independently
3. **Estimated Time**: 2-3 hours
4. **Risk Level**: Medium - requires understanding current parser behavior

The goal is to align all parser tests with the actual current implementation, fixing type mismatches and null reference issues.