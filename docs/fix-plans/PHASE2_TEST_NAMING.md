# Phase 2: Test Naming & Discovery Issues

## Context
The Cadenza project test suite has 85 failing tests out of 263 total tests. Phase 2 focuses on fixing test naming convention violations and test discovery framework issues.

## Project Overview
- **Language**: Cadenza (compiles to C#)
- **Test Framework**: NUnit with custom test discovery
- **Test Structure**: Unit tests, Integration tests, Golden file tests, Performance tests
- **Test Discovery**: Custom framework in `tests/framework/TestDiscovery.cs`

## Current Issues

### 1. Test Naming Convention Violations
The test discovery framework enforces specific naming patterns, but many tests don't follow them:

**Expected Pattern**: `Component_ShouldBehavior` or `Component_ShouldBehavior_WhenCondition`
**Valid Prefixes**: `Lexer_`, `Parser_`, `CodeGenerator_`, `Transpiler_`, `GoldenFile_`, `Regression_`, `Discovery_`

### 2. Specific Failing Tests
Based on the test output, these tests have naming violations:
- `AstTests.Program_Creation_ShouldStoreStatements`
- `AstTests.FunctionDeclaration_Creation_ShouldStoreAllProperties`
- `AstTests.Parameter_Creation_ShouldStoreNameAndType`
- `AstTests.LetStatement_Creation_ShouldStoreAllProperties`
- `AstTests.IfStatement_Creation_ShouldStoreAllProperties`
- `AstTests.BinaryExpression_Creation_ShouldStoreAllProperties`
- `AstTests.CallExpression_Creation_ShouldStoreAllProperties`
- `AstTests.NumberLiteral_Creation_ShouldStoreValue`
- `AstTests.StringLiteral_Creation_ShouldStoreValue`
- `AstTests.BooleanLiteral_Creation_ShouldStoreValue`
- `AstTests.ResultExpression_Creation_ShouldStoreAllProperties`
- `AstTests.ErrorPropagation_Creation_ShouldStoreExpression`
- `AstTests.MatchExpression_Creation_ShouldStoreAllProperties`
- `AstTests.MatchCase_Creation_ShouldStoreAllProperties`
- `AstTests.ModuleDeclaration_Creation_ShouldStoreAllProperties`
- `AstTests.ImportStatement_Creation_ShouldStoreAllProperties`
- `AstTests.ExportStatement_Creation_ShouldStoreExportedNames`
- `AstTests.SpecificationBlock_Creation_ShouldStoreAllProperties`
- `AstTests.GuardStatement_Creation_ShouldStoreAllProperties`
- `AstTests.TernaryExpression_Creation_ShouldStoreAllProperties`
- `AstTests.AllASTNodes_ShouldInheritFromASTNode`

And many more in other test classes.

### 3. Test Discovery Framework Issue
The current test discovery is too strict and doesn't handle valid test patterns properly.

## Tasks to Complete

### Task 1: Fix AST Test Naming
Rename AST tests to follow proper naming convention:
- `Program_Creation_ShouldStoreStatements` → `Ast_Program_ShouldStoreStatements`
- `FunctionDeclaration_Creation_ShouldStoreAllProperties` → `Ast_FunctionDeclaration_ShouldStoreAllProperties`
- Continue pattern for all AST-related tests

### Task 2: Fix Token Test Naming
- `Token_Creation_ShouldSetAllProperties` → `Tokens_Token_ShouldSetAllProperties`
- `Token_WithNullLiteral_ShouldAcceptNull` → `Tokens_Token_ShouldAcceptNull_WhenNullLiteral`
- `TokenType_Enum_ShouldContainExpectedValues` → `Tokens_TokenType_ShouldContainExpectedValues`

### Task 3: Fix Package Manager Test Naming
- `AddPackage_Should_AddToConfig` → `PackageManager_AddPackage_ShouldAddToConfig`
- `AddPackage_WithDevFlag_Should_AddToDevDependencies` → `PackageManager_AddPackage_ShouldAddToDevDependencies_WhenDevFlag`

### Task 4: Fix Analysis Test Naming
- `PureFunctionValidationRule_ShouldDetectPureFunctionWithEffects` → `Analysis_PureFunctionValidationRule_ShouldDetectPureFunctionWithEffects`
- Continue pattern for all analysis tests

### Task 5: Update Test Discovery Framework
Improve the `IsWellNamedTestMethod` function in `tests/framework/TestDiscovery.cs` to:
1. Add missing valid prefixes: `Ast_`, `Tokens_`, `PackageManager_`, `Analysis_`
2. Handle edge cases better
3. Make the validation more flexible while maintaining standards

## Expected Files to Modify

1. `tests/unit/AstTests.cs` - Rename all test methods
2. `tests/unit/TokensTests.cs` - Rename test methods
3. `tests/unit/package/PackageManagerTests.cs` - Rename test methods
4. `tests/unit/analysis/EffectAnalyzerTests.cs` - Rename test methods
5. `tests/unit/analysis/LintRuleEngineTests.cs` - Rename test methods
6. `tests/framework/TestDiscovery.cs` - Update validation logic
7. Other test files with naming violations

## Naming Convention Examples

**Before:**
```csharp
[Test]
public void Program_Creation_ShouldStoreStatements()
{
    // Test implementation
}
```

**After:**
```csharp
[Test]
public void Ast_Program_ShouldStoreStatements()
{
    // Test implementation
}
```

## Success Criteria

1. ✅ All test methods follow `Component_ShouldBehavior` pattern
2. ✅ Test discovery framework accepts all renamed tests
3. ✅ `Discovery_AllTestMethodsAreProperlyNamed` test passes
4. ✅ No test naming violations in output

## Reference Files

- **Test Discovery**: `tests/framework/TestDiscovery.cs`
- **Project Structure**: `docs/PROJECT_STRUCTURE_CLEAN.md`
- **Current failing test**: `Discovery_AllTestMethodsAreProperlyNamed`

## Implementation Strategy

1. **Start with Test Discovery**: Update the framework to be more flexible
2. **Systematic Renaming**: Go through each test file and rename methods
3. **Validation**: Run the discovery test to ensure all names are valid
4. **Verification**: Ensure renamed tests still run correctly

## Commands to Run

After implementation, verify with:
```bash
# Run the test discovery validation
dotnet test tests/Cadenza.Tests.csproj --filter "FullyQualifiedName~Discovery_AllTestMethodsAreProperlyNamed"

# Check overall test status
dotnet test tests/Cadenza.Tests.csproj --logger "console;verbosity=normal"
```

## Implementation Notes

1. **Priority**: Medium - Improves test organization and discovery
2. **Dependencies**: None - can run independently
3. **Estimated Time**: 1-2 hours
4. **Risk Level**: Low - mainly renaming operations

The goal is to reduce the failing test count by fixing all test naming convention violations, improving the overall test suite organization.