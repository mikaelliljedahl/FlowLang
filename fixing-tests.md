# Fixing Cadenza Tests - Part 2

This document summarizes the current failing tests in the Cadenza project and provides ideas for how to approach fixing them. The goal is to get all tests passing, ensuring the stability and correctness of the Cadenza transpiler and its associated tools.

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

## Current Test Status

After running `dotnet test` in the `tests` directory, here's a breakdown of the failing tests:

**Total Tests: 237**
**Passed: 204**
**Failed: 33**

## Remaining Errors (Focus Areas)

The errors can be grouped into several categories:

### 1. Package Manager Integration Tests

These tests are related to the `Cadenza.Package` project and its interaction with NuGet packages and workspace management.

*   **`PackageIntegration_EndToEnd_PackageLifecycle_Should_Work`**:
    *   **Error:** `Assert.That(addResult.Success, Is.True)` Expected: True But was: False
    *   **Idea:** This indicates that `packageManager.AddPackageAsync("TestPackage")` is failing. This is likely because the `PackageManager` is trying to resolve the package from a non-existent source or the mock setup is incomplete. The warning "Failed to query NuGet source ... An invalid request URI was provided" suggests an issue with the NuGet source configuration in the test environment.
    *   **Action:**
        1.  Verify the `PackageManager.AddPackageAsync` implementation and its interaction with NuGet sources.
        2.  Ensure the `MockPackageRegistry` and `MockNuGetClient` are correctly integrated and providing the expected behavior for `AddPackageAsync`.
        3.  Confirm that the `EnhancedFlowcConfig` is correctly configured with a valid local NuGet source in the test setup, or that the test is not trying to hit a real, invalid NuGet feed.

*   **`PackageIntegration_PackageCreation_Should_GenerateValidPackage`**:
    *   **Error:** `Assert.That(packagePath.EndsWith("sample-package-1.0.0.nupkg"), Is.True)` Expected: True But was: False
    *   **Idea:** The test expects a `.nupkg` extension, but the output shows a `.zip` file (`Created package: /tmp/.../sample-package-1.0.0.zip`). This suggests the `PackageCreator` is generating a `.zip` instead of a `.nupkg`.
    *   **Action:**
        1.  Inspect the `Cadenza.Package.PackageCreator` class and its `CreatePackageAsync` method.
        2.  Ensure it's using the correct NuGet packaging APIs to generate `.nupkg` files, not generic `.zip` files.

*   **`PackageManager_EffectInference_ShouldMapNuGetPackagesCorrectly`**:
    *   **Error:** `System.IO.InvalidDataException : Central Directory corrupt.`
    *   **Idea:** This error occurs when trying to read a corrupted or invalid ZIP archive. The `BindingGenerator.GenerateBindingsAsync` method is likely trying to open a `MemoryStream` that doesn't contain a valid NuGet package structure. This points to an issue with how the mock NuGet package stream is being provided.
    *   **Action:**
        1.  Review the `MockNuGetClient.DownloadPackageAsync` method. It needs to return a valid, albeit minimal, stream representing a `.nupkg` file for the `BindingGenerator` to process.
        2.  Consider creating a small, valid mock `.nupkg` file in memory or on disk for this test, or ensure the mock client correctly simulates a valid package download.

### 2. Transpiler/Code Generator Tests

These tests indicate issues with how Cadenza code is being translated into C#.

*   **`Transpiler_ShouldTranspileComplexExpressions`**:
    *   **Error:** Assertion fails because the generated C# expression `a + b * c > 10 && a - b < c || c == 0;` does not match the expected `(a + b * c) > 10 && (a - b) < c || c == 0;`. This indicates an operator precedence issue in the generated code. The generated code is missing parentheses.
    *   **Idea:** The C# generator is not correctly preserving operator precedence from the Cadenza AST.
    *   **Action:**
        1.  Review the `CSharpGenerator`'s implementation for handling binary expressions and operator precedence.
        2.  Ensure that parentheses are correctly added in the generated C# to enforce the intended order of operations, especially for mixed arithmetic and logical operators.

*   **`Transpiler_ShouldTranspileErrorPropagation`**:
    *   **Error:** Assertion fails because the generated C# code for error propagation (`!parseNumber(input).IsSuccess ? throw new InvalidOperationException(parseNumber(input).Error) : parseNumber(input).Value;`) does not match the expected `var result_result = parseNumber(input);`. The generated code is overly complex and not idiomatic C#.
    *   **Idea:** The C# generator is producing a more complex and potentially less readable form of error propagation than expected. The test expects a simpler `var result_result = ...` pattern, which aligns with the `Result<T, E>` implementation described in `docs/roadmap/01-result-types.md`.
    *   **Action:**
        1.  Examine the `CSharpGenerator`'s logic for handling the `?` operator and `Result` types.
        2.  Adjust the generator to produce the simpler `var result_result = parseNumber(input); if (result_result.IsError) return result_result; var result = result_result.Value;` pattern, as outlined in the `RESULT_IMPLEMENTATION_SUMMARY.md` and `01-result-types.md` documents.

*   **`CodeGenerator_ShouldGenerateComparisonOperators`**:
    *   **Error:** Assertion fails because the generated C# code `a > b + c <= d;` does not match the expected `a > b && c <= d;`. This indicates an incorrect translation of logical operators and potentially operator precedence.
    *   **Idea:** The generator is incorrectly translating `&&` to `+` and is not handling operator precedence correctly for comparison and logical operators.
    *   **Action:**
        1.  Review the `CSharpGenerator`'s logic for handling logical AND (`&&`) and comparison operators.
        2.  Ensure correct C# syntax is generated for logical operations and that operator precedence is maintained.

*   **`CodeGenerator_ShouldGenerateComplexResultTypes`**:
    *   **Error:** Assertion fails because the generated C# code `Result.Ok<object, string>(Result.Ok<object, string>(42));` does not match the expected `Result.Ok(Result.Ok(42))`. This suggests an issue with generic type inference or explicit type arguments in the generated C#.
    *   **Idea:** The generator is adding explicit `object` type arguments where they might not be needed or are incorrect for nested `Result` types.
    *   **Action:**
        1.  Inspect the `CSharpGenerator`'s handling of nested generic types, specifically `Result<T, E>`. Ensure that type arguments are correctly inferred or explicitly provided in a way that matches the expected C# output.

*   **`CodeGenerator_ShouldGenerateComplexStringInterpolation`**:
    *   **Error:** Assertion fails because the generated C# code uses `$` string interpolation directly (`$"User {name} has {getCount()} messages";`) instead of `string.Format()`. The test expects `string.Format()`.
    *   **Idea:** The `STRING_IMPLEMENTATION_SUMMARY.md` and `02-string-literals.md` documents state that string interpolation should generate `string.Format()`. The current generator seems to be using direct C# 6.0+ string interpolation.
    *   **Action:**
        1.  Decide on the desired behavior: either update the test to expect direct C# string interpolation (if that's the new standard) or modify the `CSharpGenerator` to produce `string.Format()` calls as per the documentation. Given the documentation, it's likely the generator needs to be updated.

*   **`CodeGenerator_ShouldGenerateErrorPropagationInLetStatement`**:
    *   **Error:** Assertion fails because the generated C# code for `let` with error propagation (`var x = !getValue().IsSuccess ? throw new InvalidOperationException(getValue().Error) : getValue().Value;`) is overly complex and not idiomatic. The test expects `var x_result = getValue();`.
    *   **Idea:** Similar to `Transpiler_ShouldTranspileErrorPropagation`, the generated C# for `let` statements with `?` is not following the expected pattern.
    *   **Action:**
        1.  Adjust the `CSharpGenerator`'s logic for `let` statements with the `?` operator to produce the simpler and more idiomatic C# pattern for error propagation, consistent with the `Result<T, E>` implementation.

*   **`CodeGenerator_ShouldGenerateErrorResultExpression`**:
    *   **Error:** Assertion fails because the generated C# code uses `Result.Error<int, string>("failed");` instead of `return Result.Error("failed");`. The test expects a `return` statement.
    *   **Idea:** The test expects the `Error` expression to be part of a `return` statement, but the generator is producing just the `Result.Error` call.
    *   **Action:**
        1.  Ensure the `CSharpGenerator` correctly wraps `Error` expressions within `return` statements when they are the final expression in a function.

*   **`CodeGenerator_ShouldGenerateImportStatements`**:
    *   **Error:** Assertion fails because the generated C# code includes `using Cadenza.Modules.Math;` instead of the expected `using Math;`.
    *   **Idea:** The generated namespace for modules is `Cadenza.Modules.Math` instead of just `Math`. The test expects a simpler `using` statement, as suggested in `docs/examples/modules.md`.
    *   **Action:**
        1.  Decide whether the generated namespace `Cadenza.Modules.Math` is the desired behavior. If so, update the test assertion.
        2.  If the desired behavior is `using Math;`, then adjust the `CSharpGenerator` to produce simpler namespaces or aliases, potentially by configuring the module system to use shorter, more direct namespaces.

*   **`CodeGenerator_ShouldGenerateLogicalOperators`**:
    *   **Error:** Assertion fails because the generated C# code `return a + b + !c;` does not match the expected `return a && b || !c;`. This is a critical error in translating logical operators.
    *   **Idea:** The generator is incorrectly translating logical operators (`&&`, `||`) into arithmetic operators (`+`). This is a fundamental issue.
    *   **Action:**
        1.  Immediately fix the `CSharpGenerator`'s logic for translating logical AND (`&&`) and logical OR (`||`) operators. This is a high-priority fix.

*   **`CodeGenerator_ShouldGenerateModuleAsStaticClass`**:
    *   **Error:** Assertion fails because the generated C# code uses `public static class Math` within a namespace `Cadenza.Modules.Math`, instead of the expected `public static class MathModule`.
    *   **Idea:** The test expects the module to be generated as `MathModule` within the namespace, but the generator is using `Math` as the class name, leading to a conflict with the namespace name.
    *   **Action:**
        1.  Adjust the `CSharpGenerator` to consistently name the generated static class for a module (e.g., `MathModule` or `MathUtils`) to avoid conflicts and match the test's expectation.

*   **`CodeGenerator_ShouldGenerateModuleWithMultipleFunctions`**:
    *   **Error:** Similar to `CodeGenerator_ShouldGenerateModuleAsStaticClass`, the test expects `public static class MathModule` but gets `public static class Math`.
    *   **Idea:** Consistent naming is needed for generated module classes.
    *   **Action:**
        1.  Apply the same fix as for `CodeGenerator_ShouldGenerateModuleAsStaticClass` to ensure consistent naming of generated module classes.

*   **`CodeGenerator_ShouldGenerateQualifiedFunctionCall`**:
    *   **Error:** Assertion fails because the generated C# code uses `return Math.add(1, 2);` instead of `return MathModule.add(1, 2);`.
    *   **Idea:** This is related to the module naming issue. If the module is generated as `public static class Math`, then `Math.add` is correct. If it's `public static class MathModule`, then `MathModule.add` is correct. The test expects `MathModule.add`.
    *   **Action:**
        1.  Ensure consistency between the generated module class name and how qualified calls are generated, matching the test's expectation of `MathModule.add`.

*   **`CodeGenerator_ShouldGenerateResultClass`**:
    *   **Error:** Assertion fails because the generated C# `Result` class has `public readonly bool IsSuccess; public readonly T Value; public readonly E Error;` but the test expects `public T Value`. The generated `Result` class also includes `Option<T>` and `CadenzaProgram` which are not expected.
    *   **Idea:** The generated `Result` class structure is not matching the test's expectation. It seems to be including extra, unrelated code.
    *   **Action:**
        1.  Review the `CSharpGenerator`'s logic for generating the `Result` class. Ensure it generates only the `Result` class and its members as expected by the test, without including `Option` or `CadenzaProgram` definitions.

*   **`CodeGenerator_ShouldGenerateResultExpression`**:
    *   **Error:** Assertion fails because the generated C# code uses `Result.Ok<int, string>(42);` instead of `return Result.Ok(42);`. The test expects a `return` statement.
    *   **Idea:** Similar to `CodeGenerator_ShouldGenerateErrorResultExpression`, the test expects the `Ok` expression to be part of a `return` statement.
    *   **Action:**
        1.  Ensure the `CSharpGenerator` correctly wraps `Ok` expressions within `return` statements when they are the final expression in a function.

*   **`CodeGenerator_ShouldGenerateStringInterpolation`**:
    *   **Error:** Assertion fails because the generated C# code uses `$"Hello {name}!";` instead of `string.Format()`.
    *   **Idea:** Similar to `CodeGenerator_ShouldGenerateComplexStringInterpolation`, the generator is using direct C# string interpolation instead of `string.Format()` as expected by the documentation.
    *   **Action:**
        1.  Align the `CSharpGenerator`'s string interpolation output with the documented `string.Format()` approach, or update the test if the direct C# interpolation is the intended behavior.

*   **`CodeGenerator_ShouldGenerateValidCSharpSyntax`**:
    *   **Error:** `Generated code has compilation errors: The type or namespace name 'bool' could not be found...` and similar errors for `string`, `true`, etc. This indicates fundamental issues with the generated C# code's ability to compile, likely due to missing `using` directives or incorrect type references.
    *   **Idea:** The generated C# code is missing essential `using` directives for basic types or is generating invalid syntax that prevents compilation.
    *   **Action:**
        1.  Ensure the `CSharpGenerator` adds all necessary `using System;`, `using System.Collections.Generic;`, etc., directives to the generated C# files.
        2.  Verify that basic types like `int`, `string`, `bool` are correctly mapped and referenced in the generated C#.

*   **`CodeGenerator_ShouldGenerateXmlDocumentation`**:
    *   **Error:** Assertion fails because the generated C# code does not contain `/// <summary>`.
    *   **Idea:** The `SPECIFICATION_IMPLEMENTATION_SUMMARY.md` and `language-reference.md` documents indicate that specification blocks should generate XML documentation. The generator is not producing these comments.
    *   **Action:**
        1.  Implement or fix the logic in `CSharpGenerator` to correctly generate XML documentation comments (`/// <summary>`, `/// <param>`, `/// <returns>`) from Cadenza's specification blocks and function signatures.

*   **`CodeGenerator_MatchExpression_ShouldGenerateCorrectCode`**:
    *   **Error:** `Assert.That(csharpCode.Contains("int test_match()"), Is.True)` Expected: True But was: False. This indicates the generated C# for `match` expressions is not as expected.
    *   **Idea:** The `TODO.md` and `language-fundamentals.md` documents mention `match` expressions as a planned feature. This test suggests the generation for `match` is either incomplete or incorrect.
    *   **Action:**
        1.  Review the `CSharpGenerator`'s implementation for `match` expressions. Ensure it correctly translates Cadenza `match` syntax into valid and idiomatic C# `switch` expressions or `if/else if` chains.

### 3. Regression Tests

These tests are designed to prevent regressions of previously fixed bugs or ensure core functionality remains stable.

*   **`Regression_BasicLanguageFeatures`**:
    *   **Error:** `Regression test 'BasicFunction' failed: Expected pattern 'public static int test()' not found in output`. The generated C# is a single line with no spaces (`usingSystem;publicclassResult...`).
    *   **Idea:** The generated C# code is minified or has incorrect formatting, making pattern matching fail. This is likely related to the `NormalizeWhitespace()` call or a general issue in the code generation.
    *   **Action:**
        1.  Ensure the `CSharpGenerator` produces well-formatted C# code with proper whitespace and line breaks. The `NormalizeWhitespace()` call should format, not minify.

*   **`Regression_ErrorHandling`**:
    *   **Error:** `Expected error for input: pure function test() uses [Database] -> int { return 42 }` Expected: `<System.Exception>` But was: `null`.
    *   **Idea:** This test expects a compilation error (an exception) when a `pure` function declares effects, but no exception is being thrown. This indicates that the semantic analysis for the effect system is not fully implemented or is not correctly throwing errors, as described in `docs/planning/EFFECT_SYSTEM_IMPLEMENTATION_PLAN.md`.
    *   **Action:**
        1.  Review the `SemanticAnalyzer` (or equivalent component) responsible for validating effect system rules.
        2.  Ensure that `pure` functions attempting to declare effects or call effectful functions correctly trigger a compilation error (exception).

*   **`Regression_GeneratedCodeCompilation`**:
    *   **Error:** `Generated code has compilation errors: ; expected, Identifier expected, Syntax error, ',' expected, ... Program using top-level statements must be an executable., The name 'usingSystem' does not exist in the current context, ...`
    *   **Idea:** This is a broad error indicating that the generated C# code for various regression test cases is not compiling. The specific errors (`usingSystem`, missing semicolons, etc.) point to fundamental issues in the C# generation process, similar to `CodeGenerator_ShouldGenerateValidCSharpSyntax`.
    *   **Action:**
        1.  This is a high-priority, broad issue. It suggests that the `CSharpGenerator` is producing invalid C# syntax.
        2.  Focus on the specific syntax errors reported (e.g., `usingSystem` indicates missing space or incorrect `using` directive generation).
        3.  Address these systematically in the `CSharpGenerator`.

*   **`Regression_PreventBreakingChanges`**:
    *   **Error:** `Regression test failures detected: BasicFunction: Output integrity check failed ...`
    *   **Idea:** This test compares the generated output against a baseline to ensure no unintended changes. The failures indicate that the generated C# output has changed from the expected golden files. This is likely a cascading failure from other transpilation issues.
    *   **Action:**
        1.  First, fix all other transpilation and code generation errors.
        2.  Once the transpiler is producing correct C# output, if the output is intentionally different (e.g., due to improvements or refactoring), update the golden files by running the `GoldenFile_ExpectedOutputs_ShouldRegenerate` test (but only after confirming the new output is correct).
        3.  If the output is unintentionally different, debug the `CSharpGenerator` to understand why the output has changed.

### 4. Analysis Tool Tests

These tests are located in `tests/unit/analysis/` and relate to the static analysis and linting features.

*   **`Analysis_PureFunctionValidationRule_ShouldDetectPureFunctionWithEffects`**:
    *   **Error:** `Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Error))` Expected: Error But was: Warning.
    *   **Idea:** The test expects an `Error` severity for a pure function declaring effects, but it's currently a `Warning`. This is a configuration or rule definition issue.
    *   **Action:**
        1.  Adjust the severity level of this rule in the static analysis configuration or the rule definition itself to `Error`, as per the `docs/static-analysis.md` and `docs/planning/PHASE_5_ANALYSIS_MIGRATION_PLAN.md` documents.

*   **`Analysis_AnalysisReport_ShouldDeterminePassingResult`**:
    *   **Error:** `Assert.That(report.HasPassingResult(DiagnosticSeverity.Error), Is.False)` Expected: False But was: True.
    *   **Idea:** This test checks if the analysis report correctly identifies a "passing" result (no errors). The failure indicates that even with errors, the report is showing a passing result.
    *   **Action:**
        1.  Review the logic in `AnalysisReport.HasPassingResult` to ensure it correctly aggregates diagnostic severities and returns `false` if any errors are present.

### 5. Package Manager Unit Tests

These are unit tests for the package manager's internal logic.

*   **`PackageManager_ResolveAsync_ShouldReportConflict_WithVersionConflict`**:
    *   **Error:** `Expected: <Cadenza.Package.DependencyResolutionException>` But was: `null`.
    *   **Idea:** The test expects a `DependencyResolutionException` to be thrown when a version conflict occurs, but no exception is being thrown. This indicates an issue with the dependency resolver's conflict detection or exception throwing mechanism.
    *   **Action:**
        1.  Review the `DependencyResolver`'s logic for detecting and reporting version conflicts. Ensure it throws the expected exception when conflicts are found.

*   **`PackageManager_AddPackage_ShouldAddToConfig`**:
    *   **Error:** `Assert.That(result.Success, Is.True)` Expected: True But was: False.
    *   **Idea:** The `AddPackage` operation is failing to successfully add a package to the configuration. This could be due to issues with writing to `cadenzac.json` or internal logic.
    *   **Action:**
        1.  Debug the `PackageManager.AddPackage` method to understand why `result.Success` is `false`. Check file writing permissions, JSON serialization/deserialization, and internal package resolution.

*   **`PackageManager_AddPackage_ShouldAddToDevDependencies_WhenDevFlag`**:
    *   **Error:** `Assert.That(result.Success, Is.True)` Expected: True But was: False.
    *   **Idea:** Similar to the previous `AddPackage` failure, but specifically for dev dependencies.
    *   **Action:**
        1.  Apply the same debugging and fixes as for `PackageManager_AddPackage_ShouldAddToConfig`, focusing on how dev dependencies are handled.

*   **`PackageManager_RemovePackage_ShouldRemoveFromConfig`**:
    *   **Error:** `Assert.That(result.Success, Is.True)` Expected: True But was: False.
    *   **Idea:** The `RemovePackage` operation is failing.
    *   **Action:**
        1.  Debug the `PackageManager.RemovePackage` method. Check file writing, JSON manipulation, and internal logic for removing packages from the configuration.

### 6. Refactoring Tests

*   **`CodeGenerator_BasicFunctionality_ShouldWork`**:
    *   **Error:** `Assert.That(csharpCode.Contains("int test()"), Is.True)` Expected: True But was: False.
    *   **Idea:** This test seems to be a basic check for code generation after refactoring. The failure suggests that even basic function generation is not producing the expected C# output. This could be a symptom of broader code generation issues.
    *   **Action:**
        1.  Investigate the `CSharpGenerator`'s basic function generation. Ensure it correctly produces simple C# function signatures and bodies. This might be a foundational issue affecting many other tests.

## Next Steps for the New Team Member

1.  **Start with the most fundamental code generation issues.** The `CodeGenerator_ShouldGenerateLogicalOperators` and `CodeGenerator_ShouldGenerateValidCSharpSyntax` are critical, as they indicate the generated C# is fundamentally incorrect. Fixing these might resolve many other cascading failures.
2.  **Address the `Transpiler_ShouldTranspileErrorPropagation` and `CodeGenerator_ShouldGenerateErrorPropagationInLetStatement` tests.** These are related to the core `Result<T, E>` type and its propagation, which is a key feature of Cadenza. Refer to `docs/roadmap/01-result-types.md` and `archive/RESULT_IMPLEMENTATION_SUMMARY.md`.
3.  **Work on the `PackageIntegration_PackageCreation_Should_GenerateValidPackage` and `PackageManager_EffectInference_ShouldMapNuGetPackagesCorrectly` tests.** These are integration tests for the package manager, which is a significant component. Refer to `docs/package-manager.md` and `archive/ENHANCED_PACKAGE_MANAGER_SUMMARY.md`.
4.  **Systematically work through the remaining `CodeGenerator_ShouldGenerate...` tests.** For each failing test, inspect the generated C# output (using `TestContext.WriteLine` if needed). Compare it against the expected C# output. Identify the discrepancy and fix the `CSharpGenerator` accordingly.
5.  **Finally, address the `Regression_PreventBreakingChanges` test.** Once all other transpilation issues are resolved, this test will likely still fail because the generated output has changed. If the new generated output is correct and desired, update the golden files by running the `GoldenFile_ExpectedOutputs_ShouldRegenerate` test (but only after confirming the new output is correct).

Good luck! Let me know if you have any questions as you go through these.
