The test suite for the Cadenza project is currently failing with a large number of compilation errors. Your task is to fix all the compilation errors in the `tests/Cadenza.Tests.csproj` project and ensure that all tests pass.

**Instructions:**

1.  Run the tests using the following command to see the full list of errors:
    ```bash
    dotnet test tests/Cadenza.Tests.csproj
    ```
2.  Systematically go through the errors and fix them. The errors are primarily due to the tests being out of sync with the production code. You will need to:
    *   Update method calls with the correct arguments.
    *   Remove or update references to obsolete methods and properties.
    *   Resolve ambiguous references by fully qualifying types or using aliases.
    *   Add any missing `using` directives.
3.  After fixing a set of errors, run the tests again to ensure that you are making progress and not introducing new issues.
4.  Continue this process until all compilation errors are resolved and all tests pass.

**Acceptance Criteria:**

*   The `dotnet test tests/Cadenza.Tests.csproj` command completes successfully.
*   All tests in the test suite pass.
*   No new errors are introduced into the build.

# Fixing Cadenza Tests

This document summarizes the current failing tests in the Cadenza project and provides ideas for how to approach fixing them. The goal is to get all tests passing, ensuring the stability and correctness of the Cadenza transpiler and its associated tools.

## Current Test Status

After running `dotnet test` in the `tests` directory, here's a breakdown of the failing tests:

**Total Tests: 237**
**Passed: 195**
**Failed: 42**

## Remaining Errors (Focus Areas)

The errors can be grouped into several categories:

### 1. Package Manager Integration Tests

These tests are related to the `Cadenza.Package` project and its interaction with NuGet packages and workspace management.

*   **`PackageIntegration_EndToEnd_PackageLifecycle_Should_Work`**:
    *   **Error:** `Assert.That(addResult.Success, Is.True)` Expected: True But was: False
    *   **Idea:** This indicates that `packageManager.AddPackageAsync("TestPackage")` is failing. This is likely because the `PackageManager` is trying to resolve the package from a non-existent source or the mock setup is incomplete.
    *   **Action:**
        1.  Verify the `PackageManager.AddPackageAsync` implementation. It might be trying to hit a real NuGet feed or a hardcoded non-existent one.
        2.  Ensure the `MockPackageRegistry` and `MockNuGetClient` are correctly integrated and providing the expected behavior for `AddPackageAsync`.
        3.  Confirm that the `EnhancedFlowcConfig` is correctly configured with the local NuGet source in the test setup.

*   **`PackageIntegration_PackageCreation_Should_GenerateValidPackage`**:
    *   **Error:** `Assert.That(packagePath.EndsWith("sample-package-1.0.0.nupkg"), Is.True)` Expected: True But was: False
    *   **Idea:** The test expects a `.nupkg` extension, but the output is a `.zip` file. This suggests the `PackageCreator` is generating a `.zip` instead of a `.nupkg`.
    *   **Action:**
        1.  Inspect the `Cadenza.Package.PackageCreator` class and its `CreatePackageAsync` method.
        2.  Ensure it's using the correct NuGet packaging APIs to generate `.nupkg` files.
        3.  The test output shows `Created package: /tmp/.../sample-package-1.0.0.zip`. This confirms the `.zip` extension.

*   **`PackageManager_EffectInference_ShouldMapNuGetPackagesCorrectly`**:
    *   **Error:** `System.IO.InvalidDataException : Central Directory corrupt.`
    *   **Idea:** This error occurs when trying to read a corrupted or invalid ZIP archive. The `BindingGenerator.GenerateBindingsAsync` method is likely trying to open a `MemoryStream` that doesn't contain a valid NuGet package structure.
    *   **Action:**
        1.  Review the `MockNuGetClient.DownloadPackageAsync` method. It currently returns an `emptyStream`. It needs to return a valid, albeit minimal, stream representing a `.nupkg` file for the `BindingGenerator` to process.
        2.  Consider creating a small, valid mock `.nupkg` file in memory or on disk for this test.

### 2. Transpiler/Code Generator Tests

These tests are located in `tests/integration/TranspilationTests.cs` and `tests/unit/CodeGeneratorTests.cs`. They indicate issues with how Cadenza code is being translated into C#.

*   **`Transpiler_ShouldPreserveComments`**:
    *   **Error:** Assertion fails because the expected XML documentation comment (`/// Pure function - no side effects`) is not found in the generated output.
    *   **Idea:** The C# generator is not correctly producing XML documentation comments from Cadenza's `pure` function declarations.
    *   **Action:**
        1.  Inspect `CSharpGenerator.GenerateFunction` and related methods to ensure XML documentation comments are being generated.
        2.  Verify that the `/// Pure function - no side effects` comment is correctly extracted from the Cadenza AST and formatted as XML documentation.

*   **`Transpiler_ShouldTranspileComplexExpressions`**:
    *   **Error:** Assertion fails because the generated C# expression `a + b * c > 10 && a - b < c || c == 0;` does not match the expected `(a + b * c) > 10 && (a - b) < c || c == 0;`. This indicates an operator precedence issue in the generated code.
    *   **Idea:** The C# generator is not correctly preserving operator precedence from the Cadenza AST.
    *   **Action:**
        1.  Review the `CSharpGenerator`'s implementation for handling binary expressions and operator precedence.
        2.  Ensure that parentheses are correctly added in the generated C# to enforce the intended order of operations.

*   **`Transpiler_ShouldTranspileErrorPropagation`**:
    *   **Error:** Assertion fails because the generated C# code for error propagation (`!parseNumber(input).IsSuccess ? throw new InvalidOperationException(parseNumber(input).Error) : parseNumber(input).Value;`) does not match the expected `var result_result = parseNumber(input);`.
    *   **Idea:** The C# generator is producing a more complex and potentially less readable form of error propagation than expected. The test expects a simpler `var result_result = ...` pattern.
    *   **Action:**
        1.  Examine the `CSharpGenerator`'s logic for handling the `?` operator and `Result` types.
        2.  Adjust the generator to produce the simpler `var result_result = parseNumber(input); if (result_result.IsError) return result_result; var result = result_result.Value;` pattern, or update the test to match the current generated output if the current output is functionally correct and idiomatic C#.

*   **`Transpiler_ShouldTranspileImportsAndQualifiedCalls`**:
    *   **Error:** Assertion fails because `using Math;` is expected, but `using Cadenza.Modules.Math;` is generated.
    *   **Idea:** The generated C# namespace for modules is `Cadenza.Modules.Math` instead of just `Math`. The test expects a simpler `using` statement.
    *   **Action:**
        1.  Decide whether the generated namespace `Cadenza.Modules.Math` is the desired behavior. If so, update the test assertion.
        2.  If the desired behavior is `using Math;`, then adjust the `CSharpGenerator` to produce simpler namespaces or aliases.

*   **`Transpiler_ShouldTranspileLetStatements`**:
    *   **Error:** This test is currently commented out. When uncommented, it fails with compilation errors related to the `input` string.
    *   **Idea:** The `input` string in the test itself is malformed, causing the C# compiler to fail.
    *   **Action:**
        1.  Uncomment the test.
        2.  Correct the `input` string to be a valid Cadenza code snippet. It currently has nested string literals that are incorrectly escaped. It should be:
            ```csharp
            var input = @"function calculateTotal(baseValue: int, tax: int) -> int {
                let subtotal = baseValue + tax
                let discount = subtotal / 10
                return subtotal - discount
            }";
            ```
        3.  Verify the assertions match the expected C# output for `let` statements.

*   **`Transpiler_ShouldTranspileModules`**:
    *   **Error:** Assertion fails because `namespace Math` is expected, but `namespace Cadenza.Modules.Math` is generated.
    *   **Idea:** Similar to the import issue, the generated namespace for modules is `Cadenza.Modules.Math` instead of just `Math`.
    *   **Action:**
        1.  Decide whether the generated namespace `Cadenza.Modules.Math` is the desired behavior. If so, update the test assertion.
        2.  If the desired behavior is `namespace Math`, then adjust the `CSharpGenerator` to produce simpler namespaces.

### 3. Regression Tests

These tests are located in `tests/regression/RegressionTests.cs` and are designed to prevent regressions of previously fixed bugs or ensure core functionality remains stable.

*   **`Regression_BasicLanguageFeatures`**:
    *   **Error:** `Regression test 'BasicFunction' failed: Expected pattern 'public static int test()' not found in output`
    *   **Idea:** The generated C# code for basic functions is not matching the expected pattern. This could be due to changes in the C# generator's output format or a fundamental issue in basic function transpilation.
    *   **Action:**
        1.  Inspect the generated C# for `BasicFunction`.
        2.  Update the expected pattern in the test if the generated code is correct but the pattern is outdated.
        3.  If the generated code is incorrect, debug the `CSharpGenerator` for basic function declarations.

*   **`Regression_ErrorHandling`**:
    *   **Error:** `Expected error for input: pure function test() uses [Database] -> int { return 42 }` Expected: `<System.Exception>` But was: `null`
    *   **Idea:** This test expects a compilation error (an exception) when a `pure` function declares effects, but no exception is being thrown. This indicates that the semantic analysis for the effect system is not fully implemented or is not correctly throwing errors.
    *   **Action:**
        1.  Review the `SemanticAnalyzer` (or equivalent component) responsible for validating effect system rules.
        2.  Ensure that `pure` functions attempting to declare effects or call effectful functions correctly trigger a compilation error (exception).

*   **`Regression_GeneratedCodeCompilation`**:
    *   **Error:** `Generated code has compilation errors: ; expected, Identifier expected, Syntax error, ',' expected, ... Program using top-level statements must be an executable., The name 'usingSystem' does not exist in the current context, ...`
    *   **Idea:** This is a broad error indicating that the generated C# code for various regression test cases is not compiling. The specific errors (`usingSystem`, missing semicolons, etc.) point to fundamental issues in the C# generation process.
    *   **Action:**
        1.  This is a high-priority, broad issue. It suggests that the `CSharpGenerator` is producing invalid C# syntax.
        2.  Focus on the specific syntax errors reported (e.g., `usingSystem` indicates missing space or incorrect `using` directive generation).
        3.  Address these systematically in the `CSharpGenerator`.

*   **`Regression_PreventBreakingChanges`**:
    *   **Error:** `Regression test failures detected: BasicFunction: Output integrity check failed ...`
    *   **Idea:** This test compares the generated output against a baseline to ensure no unintended changes. The failures indicate that the generated C# output has changed from the expected golden files.
    *   **Action:**
        1.  First, fix all other transpilation and code generation errors.
        2.  Once the transpiler is producing correct C# output, if the output is intentionally different (e.g., due to improvements or refactoring), update the golden files by running the `GoldenFile_ExpectedOutputs_ShouldRegenerate` test (but only after confirming the new output is correct).
        3.  If the output is unintentionally different, debug the `CSharpGenerator` to understand why the output has changed.

### 4. Analysis Tool Tests

These tests are located in `tests/unit/analysis/` and relate to the static analysis and linting features.

*   **`Analysis_PureFunctionValidationRule_ShouldDetectPureFunctionWithEffects`**:
    *   **Error:** `Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Error))` Expected: Error But was: Warning
    *   **Idea:** The test expects an `Error` severity for a pure function declaring effects, but it's currently a `Warning`.
    *   **Action:**
        1.  Adjust the severity level of this rule in the static analysis configuration or the rule definition itself to `Error`.

*   **`Analysis_AnalysisReport_ShouldDeterminePassingResult`**:
    *   **Error:** `Assert.That(report.HasPassingResult(DiagnosticSeverity.Error), Is.False)` Expected: False But was: True
    *   **Idea:** This test checks if the analysis report correctly identifies a "passing" result (no errors). The failure indicates that even with errors, the report is showing a passing result.
    *   **Action:**
        1.  Review the logic in `AnalysisReport.HasPassingResult` to ensure it correctly aggregates diagnostic severities and returns `false` if any errors are present.

## Next Steps for the New Team Member

1.  **Start with the `Transpiler_ShouldTranspileLetStatements` test.**
    *   Uncomment the test.
    *   Inspect the `TestContext.WriteLine` output to see the generated C# code.
    *   Compare it carefully with the expected C# output for `let` statements.
    *   Identify why the generated code is not matching the expected pattern (e.g., incorrect variable declaration, missing semicolons, incorrect scope).
    *   Modify the `CSharpGenerator` (likely in `src/Cadenza.Core/cadenzac-core.cs`) to produce the correct C# for `let` statements.
    *   Once fixed, uncomment the `Assert.That` lines and ensure the test passes.

2.  **Address the `Regression_GeneratedCodeCompilation` errors.**
    *   This is a critical category. Focus on the specific C# compilation errors reported (e.g., `usingSystem`, missing semicolons, `Program using top-level statements must be an executable`).
    *   These errors indicate fundamental issues in the `CSharpGenerator`'s ability to produce valid C# syntax.
    *   Fix these errors in `src/Cadenza.Core/cadenzac-core.cs` (or other relevant generator files).

3.  **Work on the `PackageIntegration_PackageCreation_Should_GenerateValidPackage` test.**
    *   The test expects a `.nupkg` but gets a `.zip`. This needs to be fixed in the `PackageCreator` to generate the correct file type.

4.  **Address the `PackageManager_EffectInference_ShouldMapNuGetPackagesCorrectly` test.**
    *   The `Central Directory corrupt` error indicates an issue with the mock NuGet package stream. The `MockNuGetClient.DownloadPackageAsync` needs to provide a valid, even if empty, `.nupkg` stream.

5.  **Systematically work through the remaining `Transpiler_ShouldTranspile...` and `CodeGenerator_ShouldGenerate...` tests.**
    *   For each failing test, inspect the generated C# output (using `TestContext.WriteLine` if needed).
    *   Compare it against the expected C# output.
    *   Identify the discrepancy and fix the `CSharpGenerator` accordingly.

6.  **Finally, address the `Regression_PreventBreakingChanges` test.**
    *   Once all other transpilation issues are resolved, this test will likely still fail because the generated output has changed.
    *   If the new generated output is correct and desired, update the golden files by running the `GoldenFile_ExpectedOutputs_ShouldRegenerate` test (but only after confirming the new output is correct).

Good luck! Let me know if you have any questions as you go through these.
