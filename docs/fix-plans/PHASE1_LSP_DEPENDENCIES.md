# Phase 1: LSP Dependencies & Project Setup

## Context
The Cadenza project is a backend programming language compiler with LSP support. The test suite has 85 failing tests out of 263 total tests. Phase 1 focuses on fixing LSP-related dependencies and project setup issues.

## Project Overview
- **Language**: Cadenza (compiles to C#)
- **Architecture**: Lexer → Parser → AST → Transpiler → C# → Assembly
- **Key Components**: 
  - `src/Cadenza.Core/` - Core compiler (lexer, parser, transpiler)
  - `src/Cadenza.LanguageServer/` - LSP implementation (recently recreated)
  - `src/Cadenza.Analysis/` - Static analysis
  - `src/Cadenza.Package/` - Package management
  - `tests/` - Test suite using NUnit

## LSP Project Status
The `src/Cadenza.LanguageServer/` project was recently recreated and contains:
- `CompletionProvider.cs` - Auto-completion functionality
- `DocumentManager.cs` - Document management
- `CadenzaLanguageServer.cs` - Main LSP server
- `HoverProvider.cs` - Hover information
- `DefinitionProvider.cs` - Go-to-definition

## Current Issues

### 1. Missing NuGet Dependencies
The LSP project is missing required NuGet packages:
- `Microsoft.VisualStudio.LanguageServer.Protocol` - Used by all LSP classes
- Possibly missing other LSP-related packages

### 2. Test Project References
The main test project (`tests/Cadenza.Tests.csproj`) may not reference the LSP project properly.

### 3. LSP Test Failures
LSP tests are failing because they expect certain classes that should be in the LSP project:
- `tests/unit/lsp/CompletionProviderTests.cs`
- `tests/unit/lsp/DiagnosticsProviderTests.cs`
- `tests/unit/lsp/DocumentManagerTests.cs`

## Specific Failing Tests

Based on the test analysis, LSP-related tests are failing due to:
1. Missing `Microsoft.VisualStudio.LanguageServer.Protocol` package
2. Test project not referencing `Cadenza.LanguageServer` project
3. Potential compilation errors in LSP classes

## Tasks to Complete

### Task 1: Add Missing NuGet Packages
1. Add `Microsoft.VisualStudio.LanguageServer.Protocol` package to `src/Cadenza.LanguageServer/Cadenza.LanguageServer.csproj`
2. Add any other missing dependencies identified during compilation
3. Ensure package versions are compatible with .NET 10.0

### Task 2: Fix LSP Project References
1. Update `tests/Cadenza.Tests.csproj` to reference `Cadenza.LanguageServer` project
2. Ensure all necessary using statements are present in LSP classes
3. Fix any namespace issues

### Task 3: Fix LSP Compilation Issues
1. Build the LSP project and fix any compilation errors
2. Ensure all classes implement expected interfaces
3. Fix any syntax errors or missing methods

### Task 4: Update Test References
1. Update LSP test files to use the proper LSP project classes (not stubs)
2. Remove or update any references to `LSPStubs.cs` in favor of real implementations
3. Ensure test setup methods create proper instances

## Expected Files to Modify

1. `src/Cadenza.LanguageServer/Cadenza.LanguageServer.csproj` - Add NuGet packages
2. `tests/Cadenza.Tests.csproj` - Add project reference
3. `tests/unit/lsp/*.cs` - Update test implementations
4. Various LSP class files - Fix compilation issues

## Success Criteria

1. ✅ LSP project builds without errors
2. ✅ All LSP tests compile successfully
3. ✅ LSP tests run (pass/fail is secondary to compilation)
4. ✅ No missing reference errors in test output

## Reference Files

- **Project Structure**: `docs/PROJECT_STRUCTURE_CLEAN.md`
- **Language Reference**: `docs/language-fundamentals.md`
- **Core TODO**: `src/Cadenza.Core/TODO.md`
- **Test Framework**: `tests/framework/TestDiscovery.cs`

## Implementation Notes

1. **Priority**: High - LSP tests are blocking other test categories
2. **Dependencies**: None - this phase can run independently
3. **Estimated Time**: 1-2 hours
4. **Risk Level**: Low - mainly dependency and reference fixes

## Commands to Run

After implementation, verify with:
```bash
# Build LSP project
dotnet build src/Cadenza.LanguageServer/Cadenza.LanguageServer.csproj

# Run LSP tests specifically
dotnet test tests/Cadenza.Tests.csproj --filter "FullyQualifiedName~LSP"

# Check overall test status
dotnet test tests/Cadenza.Tests.csproj --logger "console;verbosity=normal"
```

The goal is to reduce the failing test count from 85 to approximately 60-70 by fixing all LSP-related issues.