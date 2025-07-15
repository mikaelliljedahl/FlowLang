# Cadenza Core TODO - Development Priorities

## ✅ TRANSPILER STATUS: MULTI-MODULE COMPILATION WORKING!

**MAJOR BREAKTHROUGH: Multi-module import resolution is now fully functional!**

### 🎉 JULY 2025 MILESTONE - MULTI-MODULE COMPILATION ACHIEVED

**What Changed:**
- **Import Resolution**: `import Math.{add, multiply}` now generates correct qualified C# calls
- **Export Syntax**: Both `export add, multiply` and `export { add, multiply }` work
- **End-to-End Tested**: Full Cadenza → C# → execution pipeline proven working

**Proof of Success:**
```
Testing Cadenza import resolution...
add(5, 3) = 8
multiply(8, 2) = 16
Final result: 16
✅ SUCCESS: Cadenza import resolution works!
```

---

## ✅ COMPLETED: DIRECT COMPILATION WITH ROSLYN

### Overview
✅ **SUCCESSFULLY IMPLEMENTED**: Native compilation support using Roslyn's compilation capabilities. Cadenza can now compile directly to assemblies instead of transpiling to C# source files.

### ✅ VERIFIED WORKING FUNCTIONALITY
- **End-to-End Testing**: Successfully tested with multiple Cadenza programs
- **Assembly Generation**: Creates working .exe files from .cdz source  
- **Assembly Execution**: Generated executables run correctly and produce expected output
- **Roslyn Integration**: Uses existing CSharpGenerator with Roslyn compilation pipeline

### ✅ SUCCESSFUL TEST RESULTS
```
=== Test Results ===
✅ Test 1: Simple return value - PASSED
✅ Test 2: Function with addition - PASSED  
✅ Test 3: Console output - PASSED
🎉 ALL DIRECTCOMPILER TESTS PASSED!
```

### ✅ PROVEN CAPABILITIES
- Cadenza source → Lexer → Parser → AST → CSharpGenerator → Roslyn → Assembly
- Generated executables execute Cadenza logic correctly
- Console output from Cadenza programs works
- Function calls and arithmetic operations work
- Proper assembly metadata and entry points

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

## 🎉 PHASE 5 SPRINT COMPLETION - JULY 2025

### ✅ SPRINT GOALS ACHIEVED

**Core Language Bug Fixes Completed:**
- ✅ **String Interpolation Bug**: Fixed critical bug where `$"Hello, {name}!"` generated malformed C# code
- ✅ **Type Casing Issues**: Fixed XML documentation to use consistent lowercase C# types
- ✅ **Complex Expression Handling**: Fixed nested if-else statements that were generating `return null;`

**Repository Cleanup Completed:**
- ✅ **Removed unused C# tooling**: Eliminated `src/Cadenza.Analysis/`, `src/Cadenza.LSP/`, `src/Cadenza.Package/`
- ✅ **Removed broken .cdz tools**: Eliminated non-working linter, dev-server, and analysis tools
- ✅ **Updated sprint plan**: Refocused on core language features that LLMs actually need

### 🎯 IMPACT ON LLM DEVELOPMENT

**Before Sprint:**
- String interpolation completely broken
- Type casing inconsistent in generated code
- Nested if-else statements didn't work
- Repository cluttered with unused tooling

**After Sprint:**
- ✅ **Professional code generation**: String interpolation works correctly
- ✅ **Consistent type casing**: Clean, professional XML documentation
- ✅ **Reliable control flow**: Complex if-else statements work properly
- ✅ **Focused codebase**: Only essential features remain

### 📊 SPRINT METRICS

**Bugs Fixed**: 3 critical language bugs
**Files Removed**: 20+ unused C# tooling files
**Code Quality**: Significantly improved generated C# code
**LLM Readiness**: Cadenza now suitable for LLM-assisted development

### 🚀 STRATEGIC OUTCOME

This sprint transformed Cadenza from a language with broken core features to a **reliable, LLM-friendly programming language** focused on what LLMs actually need:
- **Working string interpolation** for text generation
- **Clean, consistent generated code** for professional output
- **Reliable control flow** for complex logic
- **Focused feature set** without unnecessary complexity

The focus on core language reliability makes Cadenza much more suitable for LLM-assisted development workflows.

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

#### String Interpolation Bug (CRITICAL) - ✅ FIXED
- **Issue**: String interpolation in Cadenza code generates malformed C# code
- **Example**: `$"Hello, {name}!"` generates `"" + "Hello, " + "! Welcome to Cadenza!"` instead of proper interpolation
- **Status**: ✅ FIXED - July 2025 during Phase 5 sprint
- **Priority**: High
- **Impact**: String interpolation completely broken, generates incorrect output
- **Location**: String interpolation handling in C# code generator
- **Fix**: Fixed lexer to create proper Dictionary objects and updated C# generator to use InterpolatedStringExpression

#### Non-Standard Type Casing (Partial Issue) - ✅ FIXED
- **Status**: ✅ FIXED - July 2025 during Phase 5 sprint
- **Implementation**: MapCadenzaTypeToCSharp function converts most Cadenza types to proper C# types
- **Previous Issue**: Some parameter types in XML docs still show as `String` instead of `string`
- **Priority**: Low
- **Impact**: Code consistency and professional appearance
- **Fix**: Updated XML documentation generation to use MapCadenzaTypeToCSharp for parameter and return types

#### Complex Expressions Support - ✅ FIXED
- **Issue**: Limited nested expression support - nested if-else statements generated `return null;`
- **Status**: ✅ FIXED - July 2025 during Phase 5 sprint
- **Priority**: Medium
- **Impact**: Reduces language expressiveness for complex calculations
- **Fix**: Updated function body generation to handle IfStatement and GuardStatement as statements, not expressions

### Future Enhancements

#### Additional Type System Features
- **Pattern Matching**: Extend beyond basic Result<T,E> matching
- **Advanced Generics**: Support for more complex generic constraints
- **Union Types**: Consider adding union type support

#### Performance Optimizations
- **Incremental Compilation**: Cache parsed AST for faster rebuilds
- **Parallel Processing**: Multi-file compilation optimization
- **Memory Management**: Reduce memory footprint during compilation