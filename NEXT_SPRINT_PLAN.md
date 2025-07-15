# Cadenza Next Sprint Plan - Phase 5: Core Language Features & Fixes

## Sprint Goal
**Implement the `match` expression, fix critical language bugs, and begin self-hosted tooling with a new linter.**

## Background
The language is missing the `match` expression, a fundamental feature for control flow and safe error handling. Its absence is a blocker for writing robust tools in Cadenza. Additionally, several core language features have bugs that impact the quality and reliability of LLM-generated code. This sprint addresses these issues head-on.

## Key Discoveries
- **`match` expression is unimplemented**: A critical feature used in many examples is missing from the compiler.
- **String interpolation bug**: `$"Hello, {name}!"` generates malformed C# code.
- **Complex expression handling**: Nested if-else and logical operators have transpiler bugs.
- **Self-hosted tools are broken**: The linter cannot be built because it relies on unimplemented features.

## 🎯 REVISED SPRINT PLAN - PHASE 5: CORE LANGUAGE FEATURES & FIXES

### Sprint Overview
The primary goal is to implement the `match` expression, a critical missing feature. Following that, we will fix core language bugs that affect LLM-generated code quality and then begin the implementation of a self-hosted linter.

### 🚀 Core Language Implementation (Week 1-2)

#### Task 1: Implement `match` Expression (CRITICAL)
**Goal**: Implement the `match` expression as a first-class language feature.
**Status**: **BLOCKING** - This is a fundamental control-flow feature required for safe error handling and is a prerequisite for building the linter and other tools in Cadenza.
**Requirements**:
1.  **Implement Parser Support**: Add the `match` keyword and expression structure to the language grammar.
2.  **Implement Compiler Support**: Generate correct C# code for `match` expressions, supporting both `Result` type matching and general value matching (like a `switch` statement).
3.  **Enforce Exhaustiveness**: The compiler must ensure all `match` expressions are exhaustive (handle all cases or include a `_` wildcard).
4.  **Update Documentation**: Ensure the `language-reference.md` is fully aligned with the final implementation.

---

### 🐞 Critical Language Bug Fixes (Concurrent with `match` implementation)

#### Task 2: Fix String Interpolation Bug (HIGH)
**Goal**: Fix string interpolation to generate proper C# code

**Current State**: ❌ BROKEN
- `$"Hello, {name}!"` generates `"" + "Hello, " + "! Welcome to Cadenza!"`
- Should generate: `$"Hello, {name}!"`
- **Status**: BLOCKING - affects all LLM string generation

**Tasks**:
1. **Locate string interpolation code** in `src/Cadenza.Core/cadenzac-core.cs`
2. **Fix interpolation parsing** to preserve expressions inside `{}`
3. **Test with various interpolation patterns**
4. **Verify proper C# generation**

#### Task 3: Fix Type Casing Issues (LOW)
**Goal**: Ensure consistent C# type mapping

**Current State**: ⚠️ PARTIAL
- Most types work correctly (string, int, bool)
- Some XML documentation shows `String` instead of `string`
- **Status**: COSMETIC - affects code quality but not functionality

**Tasks**:
1. **Find type mapping functions** in code generator
2. **Ensure consistent lowercase** for C# primitives
3. **Test generated code quality**

#### Task 4: Fix Complex Expression Handling (MEDIUM)
**Goal**: Fix nested if-else and logical expression bugs

**Current State**: ❌ BROKEN
- Nested if-else returns `null` instead of proper logic
- Complex boolean expressions have generation issues
- **Status**: MODERATE - affects LLM conditional logic

**Tasks**:
1. **Fix nested if-else transpilation**
2. **Test complex boolean expressions**
3. **Verify logical operator precedence**

### Phase 5B: Repository Cleanup (Week 3)

#### Task 1: Remove Non-Working C# Tools
**Goal**: Clean up unused/incomplete C# tooling files

**Tasks**:
1. **Keep core compiler** (`src/Cadenza.Core/`) - essential
2. **Remove unused tooling** (`src/Cadenza.Analysis/`, `src/Cadenza.LSP/`, `src/Cadenza.Package/`)
3. **Keep language documentation** (roadmap about language features)
4. **Remove only tool-specific documentation**

#### Task 2: Update Documentation
**Goal**: Focus documentation on language features LLMs use

**Tasks**:
1. **Update README.md** with current working features
2. **Keep language reference** - essential for LLMs
3. **Keep roadmap for language features** (Result types, string literals, etc.)
4. **Remove only tooling-specific docs**

### Phase 5C: Self-Hosted Tooling (Week 3)

#### Task 1: Implement Core Linter in Cadenza
**Goal**: Create a working, extensible linter written in Cadenza.
**Prerequisite**: A working `match` expression implementation.
**Scope**:
1.  **Make it Compilable**: Rewrite `linter.cdz` using valid Cadenza syntax, including the newly implemented `match` expression.
2.  **AST Traversal Engine**: Implement the core logic to walk the Abstract Syntax Tree of a Cadenza file.
3.  **Implement Initial Rules**:
    *   Unused variable check.
    *   Function parameter limit.
    *   Discourage `?` on the last line of a function without an explicit `Ok()` wrap.
4.  **CLI Integration**: Integrate with `cadenzac` to be runnable via `cadenzac lint <file.cdz>`.

### Technical Priorities

#### High Priority (Core Language)
1. **`match` expression implementation** - critical for language completeness.
2. **String interpolation fix** - directly impacts LLM code quality.
3. **Result type error handling** - affects error propagation patterns.
4. **Basic expression evaluation** - affects all LLM-generated logic.

#### Medium Priority (Code Quality & Tooling)
1. **Type casing consistency** - affects generated code professionalism.
2. **Complex expression handling** - affects advanced LLM patterns.
3. **Core Linter Implementation** - enables self-hosted static analysis.

#### Low Priority (Documentation)
1. **Repository cleanup** - removes unused files.
2. **Documentation updates** - focuses on LLM-relevant features.

### Success Metrics

#### Sprint Success Criteria
- [ ] `match` expression is fully implemented and tested.
- [ ] String interpolation generates correct C# code.
- [ ] Type casing is consistent in generated code.
- [ ] Complex expressions transpile without errors.
- [ ] A basic, working linter written in Cadenza is integrated.
- [ ] Repository focused on core language features.
- [ ] Documentation reflects LLM-relevant features.

#### Quality Gates
- [ ] All existing examples compile without errors.
- [ ] `match` expression test cases pass.
- [ ] String interpolation test cases pass.
- [ ] Generated C# code is clean and professional.
- [ ] No unused/incomplete tooling files remain.
- [ ] Linter correctly identifies violations for its core rules.

### Benefits

#### Immediate Benefits
- **Language Completeness**: `match` expression makes the language far more usable.
- **Better LLM code quality**: String interpolation works correctly.
- **Cleaner generated code**: Consistent type casing.
- **More reliable transpilation**: Complex expressions work.
- **Focused codebase**: Only essential features remain.

#### Long-term Benefits
- **Solid foundation**: Core language features are reliable.
- **Self-Hosting**: The linter is the first major tool written in Cadenza.
- **LLM-friendly**: Language works well for LLM-generated code.
- **Maintainable**: Focused codebase without unused tooling.
- **Quality**: Professional generated C# code.

### Timeline (3 weeks)

#### Week 1: `match` Expression & Critical Fixes
- **Day 1-3**: Implement `match` expression parser and compiler support.
- **Day 4-5**: Fix string interpolation bug and type casing issues.

#### Week 2: Expression Handling & Linter Prep
- **Day 1-3**: Fix complex expression transpilation.
- **Day 4-5**: Begin linter implementation in Cadenza, assuming `match` is working.

#### Week 3: Linter and Cleanup
- **Day 1-2**: Complete core linter implementation and CLI integration.
- **Day 3-4**: Remove unused C# tooling and update documentation.
- **Day 5**: Final testing and validation of all sprint tasks.

### Strategic Impact

This sprint transforms Cadenza into a **reliable, more complete, and LLM-friendly language** by:
- **Implementing critical features** like `match`.
- **Fixing core bugs** that affect code quality.
- **Beginning the self-hosting journey** with the new linter.
- **Removing complexity** that LLMs don't need.
- **Focusing on essentials** - compilation, syntax, basic features.
- **Ensuring quality** - professional generated code.

The focus on core language reliability and completeness makes Cadenza much more suitable for both human and LLM-assisted development workflows.
