# Control Flow Implementation

## Overview
Add if/else statements, boolean expressions, and basic control flow constructs to FlowLang.

## Goals
- Support if/else statements with proper nesting
- Add boolean expressions with &&, ||, ! operators
- Add guard clauses for early returns
- Support comparison operators in boolean contexts

## Technical Requirements

### 1. Lexer Changes
- Add `&&` (logical AND) token
- Add `||` (logical OR) token  
- Add `!` (logical NOT) token
- Add `==` (equality) token
- Add `!=` (inequality) token
- Add `guard` keyword token

### 2. Parser Changes
- Parse if/else statements
- Parse boolean expressions with proper precedence
- Parse guard clauses: `guard condition else { return }`
- Handle nested if/else statements

### 3. AST Changes
- Add `IfStatement` AST node
- Add `GuardStatement` AST node
- Add `UnaryExpression` AST node (for !)
- Update `BinaryExpression` for logical operators

### 4. Code Generator Changes
- Generate C# if/else statements
- Generate boolean expressions
- Generate guard clauses as if statements
- Handle logical operator precedence

## Example FlowLang Code
```flowlang
function validate_age(age: int) -> Result<string, string> {
    guard age >= 0 else {
        return Error("Age cannot be negative")
    }
    
    if age < 18 {
        return Ok("Minor")
    } else if age < 65 {
        return Ok("Adult")
    } else {
        return Ok("Senior")
    }
}

function is_valid_user(name: string, age: int) -> bool {
    return name != "" && age > 0 && age < 150
}

function complex_logic(a: int, b: int, c: bool) -> bool {
    return (a > 0 && b > 0) || (c && a != b)
}
```

## Expected C# Output
```csharp
public static Result<string, string> validate_age(int age)
{
    if (!(age >= 0))
    {
        return Result<string, string>.Error("Age cannot be negative");
    }
    
    if (age < 18)
    {
        return Result<string, string>.Ok("Minor");
    }
    else if (age < 65)
    {
        return Result<string, string>.Ok("Adult");
    }
    else
    {
        return Result<string, string>.Ok("Senior");
    }
}

public static bool is_valid_user(string name, int age)
{
    return name != "" && age > 0 && age < 150;
}

public static bool complex_logic(int a, int b, bool c)
{
    return (a > 0 && b > 0) || (c && a != b);
}
```

## Implementation Tasks
1. Add logical operator tokens to lexer
2. Add equality/inequality tokens to lexer
3. Add guard keyword to lexer
4. Add if/else statement parsing
5. Add boolean expression parsing with precedence
6. Add guard statement parsing
7. Create control flow AST nodes
8. Generate C# if/else statements
9. Generate boolean expressions with proper precedence
10. Generate guard clauses
11. Test with complex control flow examples

## Success Criteria
- If/else statements parse and generate correctly
- Boolean expressions work with proper precedence
- Guard clauses generate correct C# code
- Nested control flow works properly
- Generated C# code compiles and runs

## Dependencies
- Current lexer/parser/codegen infrastructure
- Result types (for guard clause examples)
- String literals (for error messages)