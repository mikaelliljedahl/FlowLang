# Refactoring Plan for cadenzac-core.cs

The `cadenzac-core.cs` file has grown too large and needs to be split into multiple files to improve maintainability, readability, and separation of concerns. The proposed file structure follows the logical phases of the compiler.

## Proposed File Structure:

1.  **`Tokens.cs`**:
    *   **Responsibility**: Contains all definitions related to lexical tokens.
    *   **Contents**:
        *   `TokenType` enum
        *   `Token` record

2.  **`Ast.cs`**:
    *   **Responsibility**: Contains all Abstract Syntax Tree (AST) node definitions. This provides a single source for the language's data structure.
    *   **Contents**:
        *   `ASTNode` base record
        *   All record types inheriting from `ASTNode` (e.g., `Program`, `FunctionDeclaration`, `BinaryExpression`, `ComponentDeclaration`, etc.).

3.  **`Lexer.cs`**:
    *   **Responsibility**: The lexical analysis phase.
    *   **Contents**:
        *   `CadenzaLexer` class.

4.  **`Parser.cs`**:
    *   **Responsibility**: The parsing phase, which builds the AST from tokens.
    *   **Contents**:
        *   `CadenzaParser` class.

5.  **`Transpiler.cs`**:
    *   **Responsibility**: Transpiling the Cadenza AST to C# source code.
    *   **Contents**:
        *   `CadenzaTranspiler` class.

6.  **`Compiler.cs`**:
    *   **Responsibility**: Orchestrating the compilation process and interacting with the C# compiler.
    *   **Contents**:
        *   `CadenzaCompiler` class (the main driver).
        *   `CSharpCompiler` class (the Roslyn wrapper).

7.  **`Program.cs`** (or keep in `Compiler.cs`):
    *   **Responsibility**: The command-line entry point for the compiler.
    *   **Contents**:
        *   The `Program` class with the `Main` method.

## Action Items:

- [ ] Create the new files as outlined above.
- [ ] Move the corresponding code from `cadenzac-core.cs` into each new file.
- [ ] Ensure all necessary `using` statements are present in the new files.
- [ ] Update the `.csproj` file if necessary to include the new files.
- [ ] Delete the original `cadenzac-core.cs` file after all code has been moved.
- [ ] Verify that the project still compiles and all tests pass after the refactoring.
- [ ] Add a separate test project based on Nunit (it seems the old tests are not using a real testing framework) that references the existing project & new files to ensure that the refactoring did not break any functionality.
---

## 🚨 CRITICAL DISCOVERY: PHASE 5 .CDZ TOOLS HAVE MAJOR SYNTAX ISSUES

### Overview
During Phase 5 self-hosting migration testing (July 2025), discovered that all existing .cdz tools in `src/Cadenza.Tools/` use **invalid Cadenza syntax** that doesn't exist in the language.

### 🚨 CRITICAL ISSUES FOUND

#### Issue 1: Implement `match` Expression
- **Files affected**: `linter.cdz`, `dev-server.cdz`, `simple-dev-server.cdz` (and many examples)
- **Problem**: The `match` expression, a critical feature for control flow, is not implemented in the compiler. It is used in multiple tool and documentation examples, but is not supported by the language.
- **Status**: **BLOCKING**
- **Requirements**: The implementation must support the full semantics as defined in `docs/language-reference.md`. This includes:
  1. **Result Type Matching**: Correctly handling `Ok(value)` and `Error(err)` branches, unwrapping the inner value for use in the corresponding block.
  2. **General Value Matching**: Functioning like a `switch` statement for other types (e.g., `int`, `string`).
  3. **Exhaustiveness**: The compiler must enforce that all possible cases are handled.
  4. **Wildcard `_`**: Support for a default case to ensure exhaustiveness.
  5. **Expression-based**: The `match` structure must be able to return a value that can be assigned to a variable.
- **Fix needed**: Implement the `match` expression in the parser and compiler. Update all examples and tools that currently use invalid `if/else` workarounds.

#### Issue 2: List Types and Array Access
- **Files affected**: `linter.cdz` (uses `List<string>` and `files[0]` syntax)
- **Problem**: List types and array indexing don't exist in Cadenza
- **Status**: **BLOCKING** - Transpiler doesn't support these types
- **Fix needed**: Use basic types and function calls

#### Issue 3: Result Type Handling
- **Files affected**: All .cdz tools
- **Problem**: Using `.IsSuccess` and `.Error` properties that don't exist
- **Status**: **BLOCKING** - Generates invalid C# code
- **Fix needed**: Use proper `?` operator for error propagation

#### Issue 4: Runtime Bridge Implementation Gaps
- **Files affected**: All .cdz tools
- **Problem**: Some `Cadenza.Runtime.*` methods return void but assigned to variables
- **Status**: **MODERATE** - Causes compilation errors
- **Fix needed**: Don't assign void return values to variables

### 🔧 TESTED WORKING SOLUTIONS

#### ✅ Simple Cadenza Tools Work
Successfully compiled and ran simplified versions:
- `linter-simple.cdz` - Basic analysis tool (✅ WORKING)
- `dev-server-simple.cdz` - Basic server demo (✅ WORKING)

#### ✅ Proven Working Patterns
```cadenza
// ✅ GOOD: Simple function calls
function main() -> int {
    let result = doSomething()
    return 0
}

// ✅ GOOD: Basic error handling
function process() -> Result<int, string> {
    let value = riskyOperation()?
    return Ok(value)
}

// ❌ BAD: Pattern matching (doesn't exist)
match result {
    Ok(value) -> processValue(value)
    Error(err) -> handleError(err)
}

// ❌ BAD: List types (don't exist)
let files: List<string> = []
let first = files[0]
```

### 📋 PHASE 5 REMEDIATION PLAN

#### Priority 1: Fix Syntax Issues (Week 1)
1. **Rewrite linter.cdz** using proper Cadenza syntax
2. **Rewrite dev-server.cdz** using working patterns
3. **Rewrite simple-dev-server.cdz** with runtime bridge fixes
4. **Test all tools compile and run**

#### Priority 2: Runtime Bridge Fixes (Week 2)
1. **Fix void return assignments** in runtime calls
2. **Implement missing runtime methods** that tools expect
3. **Add proper error handling** for runtime bridge calls
4. **Test runtime bridge functionality**

#### Priority 3: Enhanced Tooling (Week 3+)
1. **Add actual file system operations** to linter
2. **Implement HTTP server functionality** in dev-server
3. **Add WebSocket support** for hot reload
4. **Full integration testing**

### 🎯 SPRINT IMPACT

**Original Sprint Plan**: Test existing .cdz tools
**Reality**: All .cdz tools use invalid syntax and don't compile
**Adjusted Plan**: Fix syntax issues first, then implement runtime bridge

**Timeline Impact**: 
- Week 1: Fix syntax (unplanned work)
- Week 2: Test tools (original plan)
- Week 3+: Implement features (original plan)

### 💡 LESSONS LEARNED

1. **Validation Gap**: .cdz tools were written without testing against actual Cadenza syntax
2. **Language Limitations**: Cadenza is more limited than the tool authors assumed
3. **Testing Critical**: Must test compilation before building features
4. **Documentation Needed**: Clear syntax examples prevent these issues

---

### Implementation Details

#### Phase 1: Core Compilation Infrastructure
**Location**: `src/Cadenza.Core/cadenzac-core.cs`

**1.1 Extend CSharpGenerator Class**
- Add `CompileToAssembly(Program program, string outputPath)` method
- Leverage existing `GenerateFromAST()` for syntax tree generation
- Use `CSharpCompilation.Create()` pattern from test validation code
- Support both `OutputKind.ConsoleApplication` and `OutputKind.DynamicallyLinkedLibrary`

**1.2 Reference Resolution System**
```csharp
private MetadataReference[] GetDefaultReferences()
{
    return new[]
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
    };
}
```

**1.3 Compilation Method Implementation**
```csharp
public CompilationResult CompileToAssembly(Program program, string outputPath)
{
    // 1. Generate syntax tree (existing functionality)
    var syntaxTree = GenerateFromAST(program);
    
    // 2. Create compilation
    var compilation = CSharpCompilation.Create(
        Path.GetFileNameWithoutExtension(outputPath),
        new[] { syntaxTree },
        GetDefaultReferences(),
        new CSharpCompilationOptions(OutputKind.ConsoleApplication)
    );
    
    // 3. Emit assembly
    var result = compilation.Emit(outputPath);
    
    // 4. Return diagnostics and success status
    return new CompilationResult(result.Success, result.Diagnostics);
}
```

#### Phase 2: CLI Integration
**Location**: `src/Cadenza.Core/cadenzac-core.cs` (FlowCoreProgram class)

**2.1 Command Line Options**
- Add `--compile` flag: Compile to assembly instead of transpiling
- Add `--output` flag: Specify output assembly path (default: `{filename}.exe`)
- Add `--run` flag: Compile and execute in one step
- Add `--library` flag: Generate DLL instead of executable

**2.2 Enhanced Main Method**
```csharp
public static async Task<int> Main(string[] args)
{
    var options = ParseArguments(args);
    
    if (options.Compile)
    {
        return await CompileFlow(options);
    }
    else
    {
        return await TranspileFlow(options); // Existing behavior
    }
}
```

#### Phase 3: Error Reporting Enhancement
- Map Roslyn `Diagnostic` objects back to Cadenza source locations
- Enhance error messages to reference Cadenza syntax instead of generated C#
- Implement `CadenzaDiagnostic` wrapper class

#### Phase 4: Performance Optimizations
- Cache `CSharpCompilation` objects for incremental builds
- Track file dependencies and modification times
- Implement `CompilationCache` class

### Benefits

**Immediate Benefits**
- **Performance**: 30-50% faster compilation (eliminates intermediate C# file I/O)
- **User Experience**: Single command to compile and run Cadenza programs
- **Memory Efficiency**: No string manipulation of C# source code
- **Cleaner Pipeline**: Direct Cadenza → Assembly compilation

**Long-term Benefits**
- **Incremental Compilation**: Foundation for faster rebuilds
- **Better Debugging**: Direct source mapping from Cadenza to IL
- **Tool Integration**: Easier IDE integration with direct compilation
- **Multi-target Support**: Foundation for JavaScript, WASM, and native targets

### Implementation Priority

**High Priority (Sprint 1)**
1. Phase 1: Core compilation infrastructure
2. Phase 2: Basic CLI integration (--compile flag)
3. Basic testing infrastructure

**Medium Priority (Sprint 2)**
1. Phase 2: Advanced CLI options (--run, --library)
2. Phase 3: Enhanced error reporting
3. Performance benchmarks

**Low Priority (Sprint 3)**
1. Phase 4: Performance optimizations
2. Advanced source location mapping
3. Parallel compilation support

### Technical Considerations

**Backward Compatibility**
- Maintain existing transpilation workflow as default
- Add compilation as opt-in feature via command line flags
- Preserve all existing test cases and examples

**Success Metrics**
- [ ] Can compile simple Cadenza programs to executables
- [ ] Generated executables run correctly
- [ ] Compilation time < 80% of transpile + dotnet build time
- [ ] Clear error messages referencing Cadenza syntax
- [ ] Backward compatibility with existing workflows

---

## Current Known Issues & Improvements Needed

### Parser Enhancement Needs

#### Complex Nested Expressions
- **Issue**: Parser fails on complex nested if-else expressions
- **Priority**: Medium
- **Impact**: Limits expressiveness of conditional logic

#### Error Reporting
- **Issue**: Parser errors could be more descriptive
- **Priority**: Low  
- **Impact**: Developer experience

### Transpiler Code Generation Gaps

### Future Enhancements

#### Additional Type System Features
- **Pattern Matching**: Extend beyond basic Result<T,E> matching
- **Advanced Generics**: Support for more complex generic constraints
- **Union Types**: Consider adding union type support

#### Performance Optimizations
- **Incremental Compilation**: Cache parsed AST for faster rebuilds
- **Parallel Processing**: Multi-file compilation optimization
- **Memory Management**: Reduce memory footprint during compilation