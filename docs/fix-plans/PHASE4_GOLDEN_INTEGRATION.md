# Phase 4: Golden Files & Integration Tests

## Context
The Cadenza project test suite has 85 failing tests out of 263 total tests. Phase 4 focuses on fixing golden file tests and integration tests that verify the transpiler's end-to-end functionality.

## Project Overview
- **Language**: Cadenza (compiles to C#)
- **Golden Files**: Input `.cdz` files and expected `.cs` output files
- **Transpiler**: `src/Cadenza.Core/Transpiler.cs` - Converts AST to C# code
- **Integration Tests**: End-to-end tests that compile and run generated C# code

## Current Issues

### 1. Golden File Test Failures
Golden file tests compare transpiler output against expected C# code:
- **Location**: `tests/golden/` folder
- **Input Files**: `tests/golden/inputs/*.cdz` - Cadenza source files
- **Expected Output**: `tests/golden/expected/*.cs` - Expected C# output
- **Test Class**: `tests/golden/GoldenFileTests.cs`

### 2. Integration Test Failures
Integration tests that verify compiled C# code actually works:
- **Location**: `tests/integration/` folder
- **Test Class**: `tests/integration/TranspilationTests.cs`
- **Dependencies**: Require golden files to be correct first

### 3. Common Golden File Issues
Based on typical transpiler evolution:
- **Outdated Expected Files**: C# output format may have changed
- **Missing Language Features**: New Cadenza features not in golden files
- **Compilation Errors**: Generated C# code doesn't compile
- **Runtime Errors**: Generated C# code compiles but fails at runtime

## Current Golden File Structure

### Input Files (`tests/golden/inputs/`)
- `basic_functions.cdz` - Simple function declarations
- `control_flow.cdz` - If/else, loops, guards
- `effect_system.cdz` - Effect declarations and usage
- `modules.cdz` - Module declarations and imports
- `pure_functions.cdz` - Pure function modifiers
- `result_types.cdz` - Result type handling
- `string_interpolation.cdz` - String interpolation features

### Expected Output Files (`tests/golden/expected/`)
- `basic_functions.cs` - Expected C# for basic functions
- `control_flow.cs` - Expected C# for control flow
- `effect_system.cs` - Expected C# for effects
- `modules.cs` - Expected C# for modules
- `pure_functions.cs` - Expected C# for pure functions
- `result_types.cs` - Expected C# for Result types
- `string_interpolation.cs` - Expected C# for string interpolation

## Tasks to Complete

### Task 1: Regenerate Golden Files
Update all expected output files to match current transpiler behavior:

1. **Run Transpiler on Input Files**: Generate current C# output
2. **Compare with Expected**: Identify differences
3. **Update Expected Files**: Replace with current (correct) output
4. **Verify Compilation**: Ensure generated C# compiles

### Task 2: Fix Compilation Issues
Address any C# compilation errors in generated code:

1. **Missing Using Statements**: Add required C# using directives
2. **Type Mismatches**: Fix type conversion issues
3. **Syntax Errors**: Fix malformed C# syntax
4. **Namespace Issues**: Ensure proper namespace declarations

### Task 3: Fix Integration Tests
Ensure integration tests pass with updated golden files:

1. **Transpilation Tests**: Verify end-to-end compilation
2. **Runtime Tests**: Test that generated C# actually runs
3. **Error Handling**: Verify error propagation works correctly

### Task 4: Add Missing Test Coverage
Add golden files for any missing language features:

1. **Match Expressions**: If not already covered
2. **Guard Statements**: Complex guard scenarios
3. **Error Propagation**: Result type error handling
4. **Module System**: Advanced import/export scenarios

## Expected Files to Modify

### Golden Files
1. `tests/golden/expected/basic_functions.cs` - Update expected C# output
2. `tests/golden/expected/control_flow.cs` - Update expected C# output
3. `tests/golden/expected/effect_system.cs` - Update expected C# output
4. `tests/golden/expected/modules.cs` - Update expected C# output
5. `tests/golden/expected/pure_functions.cs` - Update expected C# output
6. `tests/golden/expected/result_types.cs` - Update expected C# output
7. `tests/golden/expected/string_interpolation.cs` - Update expected C# output

### Test Files
1. `tests/golden/GoldenFileTests.cs` - Fix test implementation issues
2. `tests/integration/TranspilationTests.cs` - Update integration tests
3. `tests/integration/analysis/StaticAnalysisIntegrationTests.cs` - Analysis integration
4. `tests/integration/package/PackageIntegrationTests.cs` - Package integration

## Implementation Strategy

### Step 1: Analyze Current Transpiler Output
```bash
# For each golden file input, generate current output
cd tests/golden/inputs
dotnet run --project ../../../src/Cadenza.Core/cadenzac-core.csproj basic_functions.cdz > ../expected/basic_functions.cs.new
# Compare with existing expected file
diff ../expected/basic_functions.cs ../expected/basic_functions.cs.new
```

### Step 2: Update Expected Files
```csharp
// Process each input file and update expected output
foreach (var inputFile in Directory.GetFiles("tests/golden/inputs", "*.cdz"))
{
    var transpiler = new CadenzaTranspiler();
    var generatedCode = transpiler.TranspileFile(inputFile);
    var expectedFile = Path.ChangeExtension(inputFile.Replace("inputs", "expected"), ".cs");
    File.WriteAllText(expectedFile, generatedCode);
}
```

### Step 3: Verify Compilation
```bash
# Test that generated C# compiles
csc /target:library /out:temp.dll tests/golden/expected/basic_functions.cs
```

### Step 4: Run Integration Tests
```bash
# Test end-to-end compilation and execution
dotnet test tests/Cadenza.Tests.csproj --filter "FullyQualifiedName~Integration"
```

## Success Criteria

1. ✅ All golden file tests pass
2. ✅ Generated C# code compiles without errors
3. ✅ Integration tests pass
4. ✅ No missing language features in golden files
5. ✅ Runtime behavior matches expectations

## Reference Files

- **Transpiler**: `src/Cadenza.Core/Transpiler.cs`
- **Golden Tests**: `tests/golden/GoldenFileTests.cs`
- **Integration Tests**: `tests/integration/TranspilationTests.cs`
- **Language Examples**: `examples/` folder for reference

## Commands to Run

After implementation, verify with:
```bash
# Run golden file tests specifically
dotnet test tests/Cadenza.Tests.csproj --filter "FullyQualifiedName~GoldenFile"

# Run integration tests
dotnet test tests/Cadenza.Tests.csproj --filter "FullyQualifiedName~Integration"

# Check overall test status
dotnet test tests/Cadenza.Tests.csproj --logger "console;verbosity=normal"
```

## Implementation Notes

1. **Priority**: High - Golden files validate transpiler correctness
2. **Dependencies**: Should wait for Phase 3 (Parser fixes) to complete
3. **Estimated Time**: 2-3 hours
4. **Risk Level**: Medium - Requires understanding transpiler output format

## Specific Failing Tests to Fix

Based on the test analysis, expect these patterns of failures:
- `GoldenFile_*` tests failing due to output mismatches
- `Integration_*` tests failing due to compilation errors
- `TranspilationTests.*` failing due to runtime issues

## Validation Process

1. **Manual Review**: Check that generated C# looks correct
2. **Compilation Test**: Verify C# compiles without warnings
3. **Runtime Test**: Execute generated code and verify behavior
4. **Regression Test**: Ensure existing functionality still works

The goal is to ensure the transpiler generates correct, compilable C# code that matches the expected behavior for all language features.