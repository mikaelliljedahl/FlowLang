# Phase 5 - Analysis Tools Migration Plan

## Overview
Migrate FlowLang analysis tools from incomplete C# implementations to working .flow implementations. This will provide comprehensive static analysis capabilities.

## Current State
**Existing C# files** (incomplete, no .csproj, untested):
- `src/FlowLang.Analysis/StaticAnalyzer.cs`
- `src/FlowLang.Analysis/LintRuleEngine.cs`
- `src/FlowLang.Analysis/EffectAnalyzer.cs`
- `src/FlowLang.Analysis/ResultTypeAnalyzer.cs`
- `src/FlowLang.Analysis/CodeQualityAnalyzer.cs`
- `src/FlowLang.Analysis/PerformanceAnalyzer.cs`
- `src/FlowLang.Analysis/SecurityAnalyzer.cs`
- `src/FlowLang.Analysis/AnalysisReport.cs`

**Existing .flow tool** (partial implementation):
- `src/FlowLang.Tools/linter.flow` (298 lines) - Basic analysis patterns

## Tasks

### Task 1: Extend Existing Linter
**Enhance `src/FlowLang.Tools/linter.flow`**:
- Add comprehensive rule engine
- Support multiple analysis types
- Improve error reporting
- Add configuration support

### Task 2: Create Specialized Analyzers
**Effect System Analyzer**:
- File: `src/FlowLang.Tools/analysis/effect-analyzer.flow`
- Replace: `EffectAnalyzer.cs`
- Rules:
  - Effect completeness (all side effects declared)
  - Effect minimality (no unused effects)
  - Effect propagation (effects properly bubbled up)
  - Effect consistency (consistent effect usage)

**Result Type Analyzer**:
- File: `src/FlowLang.Tools/analysis/result-analyzer.flow`
- Replace: `ResultTypeAnalyzer.cs`
- Rules:
  - Result type usage (functions with effects should return Result)
  - Error propagation (proper ? operator usage)
  - Match completeness (all Result branches handled)
  - Error message quality (descriptive error messages)

**Code Quality Analyzer**:
- File: `src/FlowLang.Tools/analysis/quality-analyzer.flow`
- Replace: `CodeQualityAnalyzer.cs`
- Rules:
  - Function complexity (cyclomatic complexity)
  - Naming conventions (consistent naming)
  - Documentation coverage (spec blocks present)
  - Code duplication detection

**Security Analyzer**:
- File: `src/FlowLang.Tools/analysis/security-analyzer.flow`
- Replace: `SecurityAnalyzer.cs`
- Rules:
  - SQL injection detection
  - XSS vulnerability detection
  - Insecure random number generation
  - Hardcoded secrets detection

**Performance Analyzer**:
- File: `src/FlowLang.Tools/analysis/performance-analyzer.flow`
- Replace: `PerformanceAnalyzer.cs`
- Rules:
  - Inefficient string concatenation
  - Unnecessary allocations
  - Loop optimization opportunities
  - Database query optimization

### Task 3: Create Analysis Engine
**Main Analysis Coordinator**:
- File: `src/FlowLang.Tools/analysis/analysis-engine.flow`
- Replace: `StaticAnalyzer.cs`
- Functionality:
  - Coordinate all analyzers
  - Collect and merge results
  - Generate comprehensive reports
  - Support configuration files

### Task 4: Analysis Reporting
**Report Generator**:
- File: `src/FlowLang.Tools/analysis/report-generator.flow`
- Replace: `AnalysisReport.cs`
- Formats:
  - Human-readable text output
  - JSON for CI/CD integration
  - SARIF for security tools
  - HTML for detailed browsing

### Task 5: Configuration System
**Analysis Configuration**:
- File: `src/FlowLang.Tools/analysis/config-manager.flow`
- Functionality:
  - Load `flowlint.json` configuration
  - Rule enable/disable
  - Severity level configuration
  - Custom rule parameters

### Task 6: Integration with CLI
**CLI Integration**:
- Extend main CLI with analysis commands
- Support `flowc-core --lint` command
- Integration with existing linter tool
- Batch analysis of multiple files

## Expected Outcomes
- Comprehensive static analysis for FlowLang code
- Multiple analysis types (effects, results, quality, security, performance)
- Configurable rule engine
- Multiple output formats
- Integration with development workflow

## Success Criteria
- [ ] Effect analyzer detects all major effect system issues
- [ ] Result analyzer validates proper Result type usage
- [ ] Quality analyzer provides actionable code improvements
- [ ] Security analyzer detects common vulnerabilities
- [ ] Performance analyzer suggests optimizations
- [ ] All analyzers work together in analysis engine
- [ ] Configuration system allows rule customization
- [ ] CLI integration provides seamless analysis workflow

## Dependencies
- **Runtime Bridge Fixes**: Need working .flow tools for file operations
- **Existing Linter**: Build on existing `linter.flow` implementation

## Priority
**MEDIUM** - Important for code quality but not critical path for self-hosting

## File Structure
```
src/FlowLang.Tools/analysis/
├── analysis-engine.flow         # Main coordinator
├── effect-analyzer.flow         # Effect system analysis
├── result-analyzer.flow         # Result type analysis
├── quality-analyzer.flow        # Code quality analysis
├── security-analyzer.flow       # Security analysis
├── performance-analyzer.flow    # Performance analysis
├── report-generator.flow        # Report generation
└── config-manager.flow          # Configuration management
```