# String Literals Implementation

## Overview
Add comprehensive string support to FlowLang including literals, concatenation, and interpolation.

## Goals
- Parse string literals with proper escape sequences
- Support string concatenation
- Add string interpolation syntax
- Generate clean C# string code

## Technical Requirements

### 1. Lexer Changes
- Add string literal tokenization: `"hello world"`
- Handle escape sequences: `\"`, `\\`, `\n`, `\t`, etc.
- Add string interpolation tokens: `$"Hello {name}"`

### 2. Parser Changes
- Parse string literals in expressions
- Parse string interpolation expressions
- Add string concatenation with `+` operator

### 3. AST Changes
- Add `StringLiteral` AST node
- Add `StringInterpolation` AST node
- Update `BinaryExpression` to handle string concatenation

### 4. Code Generator Changes
- Generate C# string literals
- Generate string interpolation code
- Handle string concatenation

## Example FlowLang Code
```flowlang
function greet(name: string) -> string {
    return "Hello, " + name + "!"
}

function format_message(user: string, count: int) -> string {
    return $"User {user} has {count} items"
}

function get_error_message() -> string {
    return "Error: Something went wrong\nPlease try again"
}
```

## Expected C# Output
```csharp
public static string greet(string name)
{
    return "Hello, " + name + "!";
}

public static string format_message(string user, int count)
{
    return $"User {user} has {count} items";
}

public static string get_error_message()
{
    return "Error: Something went wrong\nPlease try again";
}
```

## Implementation Tasks
1. Add string literal tokenization to lexer
2. Handle escape sequences in lexer
3. Add string interpolation tokenization
4. Add string parsing to parser
5. Add string interpolation parsing
6. Create string AST nodes
7. Generate C# string literals
8. Generate string interpolation code
9. Handle string concatenation in codegen
10. Test with various string examples

## Success Criteria
- String literals parse correctly
- Escape sequences work properly
- String interpolation generates correct C# code
- String concatenation works as expected
- Generated C# code compiles and runs

## Dependencies
- Current lexer/parser/codegen infrastructure
- Updated AST nodes for expressions