# Cadenza Next Sprint Plan - Phase 5: Core Language Bug Fixes

## Sprint Goal
**Fix critical language bugs that impact LLM-generated code quality and reliability.**

## Background
During Phase 5 testing, discovered that the existing .cdz tools used invalid syntax, revealing fundamental issues with the Cadenza transpiler. More importantly, there are critical bugs in core language features that directly impact LLM code generation quality.

## Key Discoveries
- **String interpolation bug**: `$"Hello, {name}!"` generates malformed C# code
- **Type casing issues**: Inconsistent type mapping affects code quality
- **Complex expression handling**: Nested if-else and logical operators have transpiler bugs
- **Result type handling**: Error propagation `?` operator has implementation gaps

## 🎯 REVISED SPRINT PLAN - PHASE 5: CORE LANGUAGE FIXES

### Sprint Overview
Focus on fixing the core language bugs that affect LLM-generated code quality, rather than building complex tooling that LLMs don't need.

### 🚨 Critical Language Bugs (Week 1-2)

#### Task 1: Fix String Interpolation Bug (CRITICAL)
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

#### Task 2: Fix Type Casing Issues (LOW)
**Goal**: Ensure consistent C# type mapping

**Current State**: ⚠️ PARTIAL
- Most types work correctly (string, int, bool)
- Some XML documentation shows `String` instead of `string`
- **Status**: COSMETIC - affects code quality but not functionality

**Tasks**:
1. **Find type mapping functions** in code generator
2. **Ensure consistent lowercase** for C# primitives
3. **Test generated code quality**

#### Task 3: Fix Complex Expression Handling (MEDIUM)
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

### Technical Priorities

#### High Priority (Core Language)
1. **String interpolation fix** - directly impacts LLM code quality
2. **Result type error handling** - affects error propagation patterns
3. **Basic expression evaluation** - affects all LLM-generated logic

#### Medium Priority (Code Quality)
1. **Type casing consistency** - affects generated code professionalism
2. **Complex expression handling** - affects advanced LLM patterns

#### Low Priority (Documentation)
1. **Repository cleanup** - removes unused files
2. **Documentation updates** - focuses on LLM-relevant features

### Success Metrics

#### Sprint Success Criteria
- [ ] String interpolation generates correct C# code
- [ ] Type casing is consistent in generated code
- [ ] Complex expressions transpile without errors
- [ ] Repository focused on core language features
- [ ] Documentation reflects LLM-relevant features

#### Quality Gates
- [ ] All existing examples compile without errors
- [ ] String interpolation test cases pass
- [ ] Generated C# code is clean and professional
- [ ] No unused/incomplete tooling files remain

### Benefits

#### Immediate Benefits
- **Better LLM code quality**: String interpolation works correctly
- **Cleaner generated code**: Consistent type casing
- **More reliable transpilation**: Complex expressions work
- **Focused codebase**: Only essential features remain

#### Long-term Benefits
- **Solid foundation**: Core language features are reliable
- **LLM-friendly**: Language works well for LLM-generated code
- **Maintainable**: Focused codebase without unused tooling
- **Quality**: Professional generated C# code

### Timeline (3 weeks)

#### Week 1: Critical Bug Fixes
- **Day 1-2**: Fix string interpolation bug
- **Day 3-4**: Fix type casing issues
- **Day 5**: Test and verify fixes

#### Week 2: Expression Handling
- **Day 1-3**: Fix complex expression transpilation
- **Day 4-5**: Test complex patterns and edge cases

#### Week 3: Cleanup and Documentation
- **Day 1-2**: Remove unused C# tooling
- **Day 3-4**: Update documentation
- **Day 5**: Final testing and validation

### Strategic Impact

This sprint transforms Cadenza into a **reliable, LLM-friendly language** by:
- **Fixing core bugs** that affect code quality
- **Removing complexity** that LLMs don't need
- **Focusing on essentials** - compilation, syntax, basic features
- **Ensuring quality** - professional generated code

The focus on core language reliability makes Cadenza much more suitable for LLM-assisted development workflows.