# FlowLang Static Analysis & Linting

FlowLang includes a comprehensive static analysis and linting system accessible via `flowc lint`. This system helps detect issues, enforce best practices, and suggest improvements for code quality, performance, and security.

## Quick Start

```bash
# Analyze current directory
flowc lint

# Analyze specific files or directories
flowc lint src/ examples/

# Run only specific rule categories
flowc lint --effects --results

# Output in JSON format for CI/CD
flowc lint --format json

# Show detailed help
flowc help lint
```

## Analysis Categories

### Effect System Analysis

FlowLang's unique effect system is validated through several rules:

#### `effect-completeness` (Error)
Functions must declare all effects they actually use.

```flowlang
// ❌ Error: Missing effect declaration
function process_data() -> Result<string, string> {
    database_query("SELECT * FROM users")  // Uses Database but not declared
    return Ok("done")
}

// ✅ Correct: Effect properly declared
function process_data() uses [Database] -> Result<string, string> {
    database_query("SELECT * FROM users")
    return Ok("done")
}
```

#### `effect-minimality` (Warning)
Functions shouldn't declare effects they don't use.

```flowlang
// ❌ Warning: Network effect declared but not used
function calculate_tax(amount: int) uses [Database, Network] -> int {
    return amount * 8 / 100
}

// ✅ Correct: Only necessary effects declared
function calculate_tax(amount: int) -> int {
    return amount * 8 / 100
}
```

#### `pure-function-validation` (Error)
Pure functions cannot declare or use any effects.

```flowlang
// ❌ Error: Pure function cannot have effects
pure function calculate() uses [Database] -> int {
    return 42
}

// ✅ Correct: Pure function with no effects
pure function calculate() -> int {
    return 42
}
```

#### `effect-propagation` (Error)
Callers must declare effects of functions they call.

```flowlang
function database_save() uses [Database] -> Result<string, string> { ... }

// ❌ Error: Missing Database effect
function process_user() -> Result<string, string> {
    database_save()
}

// ✅ Correct: Effect propagated
function process_user() uses [Database] -> Result<string, string> {
    database_save()
}
```

### Result Type Analysis

FlowLang's Result<T, E> types are analyzed for proper usage:

#### `unused-results` (Error)
All Result values must be handled with `?` or explicit pattern matching.

```flowlang
// ❌ Error: Result value ignored
function bad_example() -> int {
    divide(10, 2)  // Result<int, string> ignored
    return 42
}

// ✅ Correct: Result handled with error propagation
function good_example() -> Result<int, string> {
    let result = divide(10, 2)?
    return Ok(result + 10)
}
```

#### `error-handling` (Error)
Functions returning Result must handle all error paths properly.

```flowlang
// ❌ Error: Missing error handling
function incomplete() -> Result<int, string> {
    return Ok(42)
    // Missing error cases
}

// ✅ Correct: Complete error handling
function complete(x: int) -> Result<int, string> {
    if x < 0 {
        return Error("Negative value")
    }
    return Ok(x * 2)
}
```

#### `error-propagation-validation` (Error)
Error propagation operator `?` must be used correctly.

```flowlang
// ❌ Error: Using ? on non-Result function
function bad() -> int {
    non_result_function()?  // ? used on non-Result
    return 42
}

// ❌ Error: Using ? in non-Result context
function bad2() -> int {
    let x = result_function()?  // Function doesn't return Result
    return x
}
```

### Code Quality Analysis

General code quality rules for maintainable code:

#### `dead-code` (Warning)
Detect unused functions, variables, and imports.

```flowlang
// ❌ Warning: Unused function
function unused_helper(x: int) -> int {
    return x * 2
}

// ❌ Warning: Unused variable
function example() -> int {
    let unused_var = 42
    return 100
}
```

#### `naming-convention` (Info)
Enforce consistent naming patterns.

```flowlang
// ❌ Info: Function names should use camelCase
function ProcessUserData() -> int { ... }

// ✅ Correct: camelCase function name
function processUserData() -> int { ... }

// ❌ Info: Module names should use PascalCase
module userService { ... }

// ✅ Correct: PascalCase module name
module UserService { ... }
```

#### `function-complexity` (Warning)
Functions should not be too complex.

```flowlang
// ❌ Warning: Function too long/complex
function complex_function(a: int, b: int, c: int, d: int, e: int, f: int) -> int {
    // 50+ lines of code with high cyclomatic complexity
    if a > 0 {
        if b > 0 {
            if c > 0 {
                // ... deeply nested logic
            }
        }
    }
    // ... more complex logic
    return result
}
```

### Performance Analysis

Suggestions for better performance:

#### `string-concatenation` (Info)
Suggest StringBuilder for multiple string concatenations.

```flowlang
// ❌ Info: Multiple concatenations inefficient
function build_message(parts: List<string>) -> string {
    let result = ""
    result = result + "Hello "
    result = result + "beautiful "
    result = result + "world!"
    return result
}

// ✅ Better: Use string interpolation
function build_message(name: string, adjective: string) -> string {
    return $"Hello {adjective} {name}!"
}
```

#### `inefficient-effect-patterns` (Warning)
Detect suboptimal effect usage patterns.

```flowlang
// ❌ Warning: Too many effect types
function kitchen_sink() uses [Database, Network, FileSystem, Memory, IO, Logging] -> Result<string, string> {
    // Consider breaking this into smaller functions
}
```

### Security Analysis

Security best practices and vulnerability detection:

#### `input-validation` (Warning)
Functions handling external input should validate it.

```flowlang
// ❌ Warning: No input validation
function process_user_input(data: string) uses [Database] -> Result<string, string> {
    let query = $"SELECT * FROM users WHERE name = '{data}'"
    return database_execute(query)
}

// ✅ Correct: Input validation
function process_user_input(data: string) uses [Database] -> Result<string, string> {
    guard data != "" else {
        return Error("Empty input")
    }
    guard !contains_sql_injection(data) else {
        return Error("Invalid input")
    }
    return database_execute_safe(data)
}
```

#### `secret-detection` (Error)
Detect hardcoded secrets and sensitive data.

```flowlang
// ❌ Error: Hardcoded API key
function connect_api() -> string {
    let api_key = "sk-1234567890abcdef1234567890abcdef"
    return api_key
}

// ✅ Correct: Use environment variables
function connect_api() uses [IO] -> Result<string, string> {
    let api_key = get_env_var("API_KEY")?
    return Ok(api_key)
}
```

#### `unsafe-string-interpolation` (Warning)
Detect potentially unsafe string interpolation.

```flowlang
// ❌ Warning: SQL injection risk
function risky_query(user_input: string) uses [Database] -> Result<string, string> {
    let query = $"SELECT * FROM users WHERE name = '{user_input}'"
    return database_execute(query)
}

// ✅ Correct: Parameterized query
function safe_query(user_name: string) uses [Database] -> Result<string, string> {
    return database_query_param("SELECT * FROM users WHERE name = ?", user_name)
}
```

## Configuration

### flowlint.json

Create a `flowlint.json` file in your project root to customize linting behavior:

```json
{
  "rules": {
    "effect-completeness": "error",
    "effect-minimality": "warning",
    "unused-results": "error",
    "dead-code": "warning",
    "naming-convention": "info",
    "function-complexity": {
      "level": "warning",
      "enabled": true,
      "parameters": {
        "maxLines": 50,
        "maxParams": 5
      }
    }
  },
  "exclude": [
    "generated/",
    "*.test.flow",
    "temp_*"
  ],
  "severityThreshold": "warning",
  "autoFix": false,
  "outputFormat": "text"
}
```

### Rule Configuration

Each rule can be configured with:

- **Level**: `"error"`, `"warning"`, `"info"`
- **Enabled**: `true` or `false`
- **Parameters**: Rule-specific configuration

```json
{
  "rules": {
    "function-complexity": {
      "level": "warning",
      "enabled": true,
      "parameters": {
        "maxLines": 100,
        "maxParams": 8,
        "maxComplexity": 15
      }
    },
    "line-length": {
      "level": "info",
      "enabled": true,
      "parameters": {
        "maxLength": 120
      }
    }
  }
}
```

## Command Line Usage

### Basic Commands

```bash
# Analyze current directory with default rules
flowc lint

# Analyze specific files/directories
flowc lint src/ examples/ tests/

# Use custom configuration
flowc lint --config my-lint-rules.json

# Run specific rule categories only
flowc lint --effects --results --security
```

### Output Formats

```bash
# Human-readable text (default)
flowc lint

# JSON format for CI/CD integration
flowc lint --format json

# SARIF format for security tools
flowc lint --format sarif
```

### Filtering Options

```bash
# Show detailed diagnostics (default)
flowc lint --details

# Include info-level messages
flowc lint --include-info

# Auto-fix issues where possible
flowc lint --fix
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: FlowLang Lint
  run: |
    flowc lint --format sarif > flowlang-results.sarif
    # Upload SARIF results to GitHub Security tab
    - uses: github/codeql-action/upload-sarif@v2
      with:
        sarif_file: flowlang-results.sarif
```

### Exit Codes

- `0`: No issues found or only issues below severity threshold
- `1`: Issues found at or above severity threshold

### JSON Output Schema

```json
{
  "diagnostics": [
    {
      "ruleId": "effect-completeness",
      "message": "Function uses effects but doesn't declare them",
      "severity": "error",
      "location": {
        "filePath": "src/main.flow",
        "line": 10,
        "column": 5,
        "length": 12,
        "sourceText": "function test() {"
      },
      "category": "effect-system",
      "fixSuggestion": "Add 'uses [Database]' to function declaration"
    }
  ],
  "metrics": {
    "totalIssues": 5,
    "errors": 2,
    "warnings": 2,
    "infos": 1,
    "totalFunctions": 15,
    "pureFunctions": 8,
    "functionsWithEffects": 7
  },
  "ruleCounts": {
    "effect-completeness": 2,
    "unused-results": 1,
    "dead-code": 2
  },
  "analysisTime": "00:00:01.234",
  "filesAnalyzed": 5
}
```

## Best Practices

### Gradual Adoption

1. Start with error-level rules only
2. Gradually enable warning-level rules
3. Add info-level rules for style consistency
4. Configure rule parameters for your team's preferences

### Team Configuration

```json
{
  "severityThreshold": "warning",
  "rules": {
    "effect-completeness": "error",
    "unused-results": "error",
    "dead-code": "warning",
    "naming-convention": "info"
  }
}
```

### Pre-commit Hooks

```bash
#!/bin/bash
# .git/hooks/pre-commit
flowc lint --format json > lint-results.json
if [ $? -ne 0 ]; then
    echo "FlowLang linting failed. Please fix issues before committing."
    flowc lint --details
    exit 1
fi
```

## Rule Development

The linting system is extensible. New rules can be added by:

1. Implementing the `LintRule` base class
2. Registering the rule with the appropriate analyzer
3. Adding configuration options
4. Writing comprehensive tests

See the source code in `src/analysis/` for examples of rule implementation.

## Performance

The static analyzer is designed for speed:

- **Typical Analysis**: <500ms for 1000+ line files
- **Parallel Analysis**: Multiple files processed concurrently
- **Incremental Analysis**: Only changed files in IDE integration
- **Memory Efficient**: Streaming analysis for large codebases

## Troubleshooting

### Common Issues

**Configuration not loading:**
```bash
# Verify config file path
flowc lint --config ./flowlint.json
```

**Too many false positives:**
```json
{
  "severityThreshold": "error",
  "exclude": ["generated/", "vendor/"]
}
```

**Performance issues:**
```bash
# Analyze specific directories only
flowc lint src/ --exclude "node_modules/" "build/"
```

### Debug Mode

```bash
# Enable verbose output (if implemented)
flowc lint --verbose

# Check rule configuration
flowc lint --show-config
```

The FlowLang static analysis system helps maintain high code quality while leveraging the language's unique safety features. Regular use of `flowc lint` will help catch issues early and improve overall code maintainability.