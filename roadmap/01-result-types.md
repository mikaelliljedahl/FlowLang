# Result Types Implementation

## Overview
Implement FlowLang's core error handling system using Result<T, E> types that map to C# equivalents.

## Goals
- Add Result<T, E> type to FlowLang syntax
- Generate equivalent C# code using nullable reference types or custom Result class
- Support Ok() and Error() constructors
- Handle error propagation with `?` operator
- Pattern matching for Result types

## Technical Requirements

### 1. Lexer Changes
- Add `Result` keyword token
- Add `Ok` and `Error` keyword tokens
- Add `?` operator token for error propagation

### 2. Parser Changes
- Add Result type parsing: `Result<int, String>`
- Add Ok/Error expression parsing: `Ok(42)`, `Error("message")`
- Add error propagation operator: `someFunction()?`

### 3. AST Changes
- Add `ResultType` AST node
- Add `OkExpression` and `ErrorExpression` AST nodes
- Add `ErrorPropagationExpression` AST node

### 4. Code Generator Changes
- Map Result<T, E> to C# equivalent (nullable or custom class)
- Generate Ok/Error constructors
- Generate error propagation logic

## Example FlowLang Code
```flowlang
function divide(a: int, b: int) -> Result<int, String> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

function calculate(x: int, y: int) -> Result<int, String> {
    let result = divide(x, y)?
    return Ok(result * 2)
}
```

## Expected C# Output
```csharp
public static Result<int, string> divide(int a, int b)
{
    if (b == 0)
    {
        return Result<int, string>.Error("Division by zero");
    }
    return Result<int, string>.Ok(a / b);
}

public static Result<int, string> calculate(int x, int y)
{
    var divideResult = divide(x, y);
    if (divideResult.IsError) return divideResult;
    var result = divideResult.Value;
    return Result<int, string>.Ok(result * 2);
}
```

## Implementation Tasks
1. Add Result, Ok, Error tokens to lexer
2. Add Result type parsing to parser
3. Add Ok/Error expression parsing
4. Add error propagation operator parsing
5. Create Result AST nodes
6. Generate C# Result class or use nullable types
7. Generate Ok/Error constructors
8. Generate error propagation logic
9. Test with examples

## Success Criteria
- FlowLang Result types parse correctly
- Generated C# code compiles and runs
- Error propagation works as expected
- Examples demonstrate full functionality

## Dependencies
- Current lexer/parser/codegen infrastructure
- String literal support (for error messages)
- Basic if/else statements (for error handling)