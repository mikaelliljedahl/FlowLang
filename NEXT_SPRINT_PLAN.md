# Cadenza Next Sprint Plan - Phase 5: Self-Hosting Migration & Frontend

## Sprint Goal
**Migrate Cadenza tooling from C# to Cadenza itself (.cdz files) and implement frontend code generation, creating a fully self-hosted ecosystem.**

## Background
With direct compilation successfully implemented in Phase 4, Cadenza now has the foundation to become truly self-hosted. The repository contains C# implementations of analysis tools, LSP server, and package management that were never completed or tested. Meanwhile, `src/Cadenza.Tools/` contains .cdz implementations that demonstrate the self-hosting pattern but have never been tested.

## Key Opportunity
The architecture supports self-hosting through the runtime bridge pattern:
- **Existing .cdz Tools**: 3 tools already written in Cadenza with proper effects/Result types
- **Runtime Bridge**: `Cadenza.Runtime.*` calls enable system operations from .cdz code
- **Direct Compilation**: New `--compile` flag can generate executables from .cdz tools
- **Proven Patterns**: Spec blocks, effects system, and modular design already established

## 🎯 NEXT SPRINT PLAN - PHASE 5: SELF-HOSTING MIGRATION & FRONTEND

### Sprint Overview
Transform Cadenza from a language with C# tooling to a fully self-hosted ecosystem where Cadenza tools are written in Cadenza itself, plus add frontend code generation capabilities.

### 🚨 Critical Discovery
**The existing .cdz tools have never been tested!**
- `src/Cadenza.Tools/linter.cdz` (298 lines) - Static analysis tool
- `src/Cadenza.Tools/dev-server.cdz` (66 lines) - Hot reload development server  
- `src/Cadenza.Tools/simple-dev-server.cdz` (165 lines) - HTTP server with file watching
- **No compiled outputs found** - these tools are completely untested

### Phase 5A: Foundation & Testing (Week 1)
**PRIORITY: CRITICAL** - Test existing .cdz tools before building on them

#### Task 1: Test & Validate Existing .cdz Tools
**Goal**: Verify existing .cdz tools compile and run correctly

**Current State**: ❌ UNTESTED
- 3 .cdz tools written but never compiled
- Runtime bridge calls may be unimplemented
- No evidence of functionality verification

**Tasks**:
1. **Compile existing .cdz tools**:
   ```bash
   cadenzac-core --compile src/Cadenza.Tools/linter.cdz -o bin/linter.exe
   cadenzac-core --compile src/Cadenza.Tools/dev-server.cdz -o bin/dev-server.exe
   cadenzac-core --compile src/Cadenza.Tools/simple-dev-server.cdz -o bin/simple-dev-server.exe
   ```

2. **Test functionality**:
   - Run linter on Cadenza codebase to validate static analysis
   - Start dev-server and test hot reload capabilities
   - Verify HTTP server serves content and handles requests
   - Fix any compilation errors or runtime issues

3. **Document runtime dependencies**:
   - Identify which `Cadenza.Runtime.*` calls need implementation
   - Create missing runtime bridge components
   - Ensure proper error handling and logging

**Acceptance Criteria**:
- All 3 .cdz tools compile successfully to executables
- Linter produces meaningful analysis output on Cadenza codebase
- Dev server starts and serves HTTP content
- File watching triggers recompilation events
- Runtime bridge calls function correctly

### Phase 5B: Parallel Development Tracks (Week 2-6)

#### Track 1: Frontend Implementation (Subagent) 🎨
**Goal**: Implement JavaScript/React code generation for UI components

**Current State**: ❌ MISSING
- UI syntax exists in examples but no code generation
- `examples/simple_ui_test.cdz` and `examples/advanced_ui_test.cdz` are syntactically complete
- No JavaScript/React output capability

**Subagent Tasks**:
1. **Analyze existing UI syntax**:
   - Study component declarations with state, events, effects
   - Understand render blocks and JSX-like syntax
   - Map Cadenza UI patterns to React patterns

2. **Create React generator**:
   - `src/Cadenza.Tools/frontend/react-generator.cdz`
   - Component state → React hooks (useState, useEffect)
   - Event handlers → React event handlers
   - Render blocks → JSX syntax
   - Effects → appropriate React patterns

3. **Extend DirectCompiler** with `--target react` option:
   - Add JavaScript/React target to DirectCompiler
   - Generate TypeScript definitions
   - Bundle optimization for frontend deployment

4. **Test with examples**:
   ```bash
   cadenzac-core --target react examples/simple_ui_test.cdz -o output/SimpleUI.jsx
   cadenzac-core --target react examples/advanced_ui_test.cdz -o output/Dashboard.tsx
   ```

**Acceptance Criteria**:
- UI components compile to valid React components
- Generated JSX matches Cadenza component structure
- TypeScript definitions are accurate
- Components work in browser environment
- Hot reload integration with dev server

#### Track 2: Analysis Tools Migration (Main) 🔍
**Goal**: Replace `src/Cadenza.Analysis/` C# files with .cdz equivalents

**Current State**: ❌ INCOMPLETE C# FILES
- 8 .cs files with analysis logic but no .csproj
- No evidence of testing or integration
- Existing `linter.cdz` has basic analysis patterns

**Tasks**:
1. **Extend existing linter.cdz** with comprehensive analysis capabilities
2. **Create specialized analyzers**:
   - `analysis/effect-analyzer.cdz` (replace EffectAnalyzer.cs)
   - `analysis/result-analyzer.cdz` (replace ResultTypeAnalyzer.cs)
   - `analysis/quality-analyzer.cdz` (replace CodeQualityAnalyzer.cs)
   - `analysis/security-analyzer.cdz` (replace SecurityAnalyzer.cs)
   - `analysis/performance-analyzer.cdz` (replace PerformanceAnalyzer.cs)

3. **Follow established patterns**:
   - Use `Cadenza.Runtime` bridge for file operations
   - Implement proper effects and Result types
   - Include spec blocks with intent and rules

4. **Test against existing codebase**:
   - Run analysis tools on Cadenza source code
   - Verify rule detection and reporting
   - Compare output to expected analysis results

**Acceptance Criteria**:
- Analysis tools detect all major Cadenza patterns
- Rule violations are correctly identified
- Output is actionable and well-formatted
- Performance is acceptable for real codebases
- C# files can be safely removed

#### Track 3: LSP Server Migration (Main) 📝
**Goal**: Replace `src/Cadenza.LSP/` C# files with .cdz equivalents

**Current State**: ❌ INCOMPLETE C# FILES
- 6 .cs files with LSP logic but no .csproj
- No evidence of testing or IDE integration
- Language server protocol implementation needed

**Tasks**:
1. **Create LSP foundation**:
   - `lsp/language-server.cdz` (replace CadenzaLanguageServer.cs)
   - `lsp/completion-provider.cdz` (replace CompletionProvider.cs)
   - `lsp/diagnostics-provider.cdz` (replace DiagnosticsProvider.cs)
   - `lsp/hover-provider.cdz` (replace HoverProvider.cs)
   - `lsp/definition-provider.cdz` (replace DefinitionProvider.cs)
   - `lsp/document-manager.cdz` (replace DocumentManager.cs)

2. **Use runtime bridge** for JSON-RPC communication:
   - Implement LSP protocol message handling
   - Integrate with analysis tools for diagnostics
   - Support real-time syntax and semantic analysis

3. **Test with VS Code** integration:
   - Create VS Code extension configuration
   - Test auto-completion and error detection
   - Verify hover information and go-to-definition

**Acceptance Criteria**:
- LSP server starts and accepts connections
- Real-time diagnostics work in VS Code
- Auto-completion provides relevant suggestions
- Hover information shows type and effect details
- Go-to-definition navigation works correctly

#### Track 4: Package Management Migration (Main) 📦
**Goal**: Replace `src/Cadenza.Package/` C# files with .cdz equivalents

**Current State**: ❌ INCOMPLETE C# FILES
- 6 .cs files with package logic but no .csproj
- No evidence of testing or registry integration
- Package management commands needed

**Tasks**:
1. **Create package tools**:
   - `package/package-manager.cdz` (replace PackageManager.cs)
   - `package/dependency-resolver.cdz` (replace DependencyResolver.cs)
   - `package/registry-client.cdz` (replace CadenzaRegistry.cs)
   - `package/security-scanner.cdz` (replace SecurityScanner.cs)
   - `package/project-config.cdz` (replace ProjectConfig.cs)

2. **Implement CLI commands**: `cadenzac add`, `cadenzac install`, `cadenzac audit`
3. **Test with real packages**:
   - Create test packages and dependencies
   - Verify version constraint resolution
   - Test security scanning functionality

**Acceptance Criteria**:
- Package commands integrate with main CLI
- Dependency resolution handles complex scenarios
- Security scanning detects vulnerabilities
- Registry integration works with NuGet and Cadenza packages
- cadenzac.json configuration is properly managed

### Phase 5C: Repository Cleanup (Week 7)
**Goal**: Remove C# tooling, consolidate structure

#### Tasks:
1. **Remove empty C# folders**:
   - Delete `src/Cadenza.Analysis/` (8 untested .cs files)
   - Delete `src/Cadenza.LSP/` (6 untested .cs files)
   - Delete `src/Cadenza.Package/` (6 untested .cs files)

2. **Migrate C# implementations to .cdz**:
   - **BlazorGenerator.cs → blazor-generator.cdz** (currently in C# for quick implementation)
   - **DirectCompiler extensions → .cdz tools**
   - **Any remaining C# tooling code**

2. **Reorganize .cdz tools**:
   - Consolidate all tools in `src/Cadenza.Tools/`
   - Create logical subfolders: `analysis/`, `lsp/`, `package/`, `frontend/`
   - Maintain consistent naming and structure

3. **Update build system**:
   - Compile .cdz tools as part of main build
   - Generate tool executables in bin/ directory
   - Update CI/CD to test .cdz tools

4. **Integration testing**:
   - Test complete toolchain together
   - Verify all tools work with each other
   - Performance testing of .cdz tools vs C# equivalents

**Acceptance Criteria**:
- Repository structure is clean and logical
- All .cdz tools compile and run correctly
- Build system generates all necessary executables
- Integration tests pass for complete toolchain
- Performance is acceptable for real usage

### Technical Architecture

#### Runtime Bridge Pattern
All .cdz tools use the established runtime bridge pattern:
```cadenza
// File operations
Cadenza.Runtime.FileSystemRuntime.ReadFile(path)
Cadenza.Runtime.FileSystemRuntime.WriteFile(path, content)

// Process execution
Cadenza.Runtime.ProcessRuntime.ExecuteCommand(cmd, args)

// Network operations
Cadenza.Runtime.HttpServerRuntime.CreateServer(port)
Cadenza.Runtime.WebSocketRuntime.BroadcastMessage(msg)

// Logging
Cadenza.Runtime.LoggingRuntime.LogInfo(message)
Cadenza.Runtime.LoggingRuntime.LogError(error)
```

#### Architecture Consistency
- **Spec blocks**: Document intent, rules, postconditions
- **Effects system**: Explicit `uses [FileSystem, Network, Logging]`
- **Result types**: Comprehensive `Result<T, E>` error handling
- **Modular design**: One responsibility per .cdz file

### Success Metrics

#### Sprint Success Criteria
- [ ] All existing .cdz tools compile and run correctly
- [ ] Frontend code generation produces working React components
- [ ] Analysis tools provide comprehensive Cadenza analysis
- [ ] LSP server works with VS Code/JetBrains IDEs
- [ ] Package management handles real dependencies
- [ ] Repository structure is clean and consolidated
- [ ] Complete self-hosted toolchain functions end-to-end

#### Quality Gates
- [ ] Performance of .cdz tools acceptable for real usage
- [ ] All C# tooling files safely removed
- [ ] Integration tests pass for complete toolchain
- [ ] Documentation updated for self-hosted architecture
- [ ] CI/CD builds and tests .cdz tools

### Benefits

#### Immediate Benefits
- **Consistency**: All tooling uses same language and patterns
- **Maintainability**: Single language to maintain (Cadenza)
- **Dogfooding**: Proves Cadenza's real-world capabilities
- **Frontend Support**: React/JavaScript code generation

#### Long-term Benefits
- **Language Evolution**: Tools evolve with language changes
- **Community Contributions**: Easier for developers to contribute
- **Educational Value**: Shows how to build complex tools in Cadenza
- **Self-Hosting**: Foundation for eventually self-hosting compiler

### Risk Mitigation

#### Technical Risks
- **Runtime Bridge Gaps**: Some `Cadenza.Runtime.*` calls may be unimplemented
- **Performance Concerns**: .cdz tools may be slower than C# equivalents
- **Integration Complexity**: LSP and package management are complex protocols
- **Frontend Complexity**: React generation requires sophisticated code mapping

#### Mitigation Strategies
- **Test existing tools first** before building on them
- **Incremental migration** with proven replacements
- **Comprehensive testing** at each development stage
- **Performance monitoring** and optimization
- **Rollback capability** if .cdz tools underperform

### Timeline (7 weeks)

#### Week 1: Foundation & Testing
- **Main**: Test existing .cdz tools, fix runtime issues
- **Subagent**: Analyze UI syntax, design React generator

#### Week 2-3: Core Development
- **Main**: Analysis tools migration + LSP server foundation
- **Subagent**: React generator implementation + DirectCompiler extension

#### Week 4-5: Integration & Features
- **Main**: Package management + LSP completion
- **Subagent**: Frontend testing + TypeScript definitions

#### Week 6: Advanced Features
- **Main**: Integration testing of migrated tools
- **Subagent**: Browser testing + hot reload integration

#### Week 7: Cleanup & Polish
- **Both**: Repository cleanup, final testing, documentation

### Deliverables

1. **Tested .cdz Tools**: All existing tools compile and run correctly
2. **Frontend Code Generation**: React/JavaScript output for UI components
3. **Analysis Tools**: Comprehensive static analysis in Cadenza
4. **LSP Server**: IDE integration with real-time diagnostics
5. **Package Management**: Dependency resolution and registry integration
6. **Clean Repository**: Consolidated structure with all C# tooling removed
7. **Complete Documentation**: Self-hosted architecture guide

### Strategic Impact

This sprint transforms Cadenza into a fully self-hosted ecosystem:
- **Proof of Concept**: Demonstrates Cadenza's capabilities for complex tools
- **Developer Experience**: Complete toolchain in single language
- **Frontend Support**: Expands Cadenza into UI development
- **Community Building**: Easier contribution and extension
- **Future Foundation**: Basis for compiler self-hosting

The self-hosting migration proves Cadenza's maturity while the frontend implementation expands its reach into full-stack development scenarios.