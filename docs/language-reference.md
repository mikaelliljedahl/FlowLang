# FlowLang Language Reference

This document provides a complete reference for the FlowLang programming language, covering all syntax, features, and semantics.

## Table of Contents

1. [Language Overview](#language-overview)
2. [Lexical Elements](#lexical-elements)
3. [Types](#types)
4. [Functions](#functions)
5. [Variables and Let Statements](#variables-and-let-statements)
6. [Control Flow](#control-flow)
7. [Expressions](#expressions)
8. [Result Types](#result-types)
9. [Effect System](#effect-system)
10. [Module System](#module-system)
11. [String Features](#string-features)
12. [Specification Blocks](#specification-blocks)
13. [Comments](#comments)
14. [Keywords](#keywords)
15. [Operators](#operators)
16. [Grammar](#grammar)

## Language Overview

FlowLang is a statically-typed, functional programming language that transpiles to C#. It emphasizes:

- **Explicit side effects** through an effect system
- **Safe error handling** with Result types
- **Pure functional programming** with explicit effect annotations
- **Strong type safety** with null safety by default
- **Module system** for code organization
- **String interpolation** for readable text formatting
- **Specification preservation** with embedded specification blocks for atomic intent-code linking

## Lexical Elements

### Identifiers

Identifiers must start with a letter or underscore, followed by letters, digits, or underscores:

```flowlang
valid_identifier
validIdentifier
_private
userName
user1
```

**Invalid identifiers:**
```flowlang
1invalid    // Cannot start with digit
user-name   // Hyphens not allowed
if          // Reserved keyword
```

### Literals

#### Integer Literals

```flowlang
42
0
1234567890
```

#### String Literals

```flowlang
"Hello, World!"
"Line 1\nLine 2"           // With escape sequences
"She said \"Hello\""       // Escaped quotes
"C:\\Users\\Name\\file"    // Escaped backslashes
""                         // Empty string
```

#### String Interpolation

```flowlang
$"Hello, {name}!"
$"User {user} has {count} items"
$"Result: {calculate(a, b)}"
```

### Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Addition/Concatenation | `a + b`, `"Hello" + " World"` |
| `-` | Subtraction | `a - b` |
| `*` | Multiplication | `a * b` |
| `/` | Division | `a / b` |
| `==` | Equality | `a == b` |
| `!=` | Inequality | `a != b` |
| `>` | Greater than | `a > b` |
| `<` | Less than | `a < b` |
| `>=` | Greater than or equal | `a >= b` |
| `<=` | Less than or equal | `a <= b` |
| `&&` | Logical AND | `a && b` |
| `\|\|` | Logical OR | `a \|\| b` |
| `!` | Logical NOT | `!condition` |
| `?` | Error propagation | `result?` |
| `=` | Assignment (let statements) | `let x = 5` |

### Delimiters

| Symbol | Description |
|--------|-------------|
| `(` `)` | Function calls, grouping |
| `{` `}` | Code blocks, modules |
| `[` `]` | Effect lists, arrays |
| `,` | Parameter/argument separation |
| `;` | Statement termination (optional) |
| `:` | Type annotation |
| `->` | Function return type |

## Types

### Basic Types

```flowlang
int     // 32-bit signed integer
string  // Unicode string
bool    // Boolean (true/false)
```

### Result Types

Result types encapsulate success or error values:

```flowlang
Result<int, string>        // Success type int, error type string
Result<User, DatabaseError> // Success type User, error type DatabaseError
```

### Function Types

Functions have types based on their signature:

```flowlang
// Type: (int, int) -> int
pure function add(a: int, b: int) -> int

// Type: (string) uses [Database] -> Result<int, string>
function saveUser(name: string) uses [Database] -> Result<int, string>
```

### Type Annotations

Type annotations are required for function parameters and return types:

```flowlang
function example(param: int) -> string {
    return "Value: " + param
}
```

## Functions

### Function Declaration

```flowlang
function functionName(param1: type1, param2: type2) -> returnType {
    // Function body
    return expression
}
```

### Pure Functions

Pure functions have no side effects:

```flowlang
pure function add(a: int, b: int) -> int {
    return a + b
}

pure function formatMessage(name: string) -> string {
    return "Hello, " + name
}
```

### Functions with Effects

Functions can declare side effects:

```flowlang
function logMessage(msg: string) uses [Logging] -> Result<string, string> {
    // Implementation would log the message
    return Ok(msg)
}

function saveData(data: string) uses [Database, Logging] -> Result<int, string> {
    // Implementation would save to database and log
    return Ok(42)
}
```

### Function Calls

```flowlang
// Simple function call
let result = add(5, 3)

// Function call with error propagation
let user = createUser("Alice")?

// Qualified function call (module)
let sum = Math.add(10, 20)
```

### Parameters

Parameters must have type annotations:

```flowlang
function example(
    name: string,
    age: int,
    isActive: bool
) -> string {
    return $"User {name}, age {age}, active: {isActive}"
}
```

## Variables and Let Statements

### Variable Declaration

Variables are declared using `let` statements:

```flowlang
let x = 42
let message = "Hello"
let result = calculate(10, 20)
```

### Type Inference

FlowLang infers types from the assigned expression:

```flowlang
let number = 42        // Inferred as int
let text = "Hello"     // Inferred as string
let flag = true        // Inferred as bool
```

### Error Propagation in Let Statements

```flowlang
function example() -> Result<int, string> {
    let step1 = riskyOperation()?    // Propagates error if step1 fails
    let step2 = anotherOperation(step1)?  // Uses step1 if successful
    return Ok(step2)
}
```

## Control Flow

### If Statements

```flowlang
if condition {
    // then branch
} else {
    // else branch
}

// Nested if-else
if age < 13 {
    return "Child"
} else if age < 18 {
    return "Teen"
} else {
    return "Adult"
}
```

### Guard Statements

Guard statements provide early returns for validation:

```flowlang
guard condition else {
    // executed if condition is false
    return Error("Validation failed")
}
// continues here if condition is true
```

### Boolean Expressions

Complex boolean logic is supported:

```flowlang
// Logical operators
if isAdmin && (hasPermission || isOwner) {
    // Complex condition
}

// Parentheses for grouping
if (age >= 18 && age <= 65) || isSpecialCase {
    // Grouped conditions
}

// Logical NOT
if !isEmpty && !isInvalid {
    // Negation
}
```

### Examples

```flowlang
function validateUser(name: string, age: int) -> Result<string, string> {
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    guard age >= 0 && age <= 150 else {
        return Error("Invalid age")
    }
    
    if age < 18 {
        return Ok("Minor user")
    } else {
        return Ok("Adult user")
    }
}
```

## Expressions

### Binary Expressions

```flowlang
// Arithmetic
a + b
a - b
a * b
a / b

// Comparison
a == b
a != b
a > b
a < b
a >= b
a <= b

// Logical
a && b
a || b

// String concatenation
"Hello" + " " + "World"
```

### Unary Expressions

```flowlang
!condition    // Logical NOT
```

### Function Call Expressions

```flowlang
functionName(arg1, arg2)
Module.functionName(arg1, arg2)
```

### Operator Precedence

From highest to lowest precedence:

1. `!` (unary NOT)
2. `*`, `/` (multiplication, division)
3. `+`, `-` (addition, subtraction)
4. `>`, `<`, `>=`, `<=` (comparison)
5. `==`, `!=` (equality)
6. `&&` (logical AND)
7. `||` (logical OR)

Use parentheses to override precedence:

```flowlang
// Without parentheses: (a + b) * c
a + b * c

// With parentheses: a + (b * c)
(a + b) * c
```

## Result Types

### Creating Result Values

```flowlang
// Success value
return Ok(42)
return Ok("Success message")

// Error value
return Error("Something went wrong")
return Error("Validation failed: " + details)
```

### Error Propagation

Use the `?` operator to propagate errors:

```flowlang
function chainedOperations() -> Result<int, string> {
    let step1 = firstOperation()?     // Returns early if error
    let step2 = secondOperation(step1)?  // Uses step1 if successful
    let step3 = thirdOperation(step2)?   // Uses step2 if successful
    return Ok(step3)
}
```

### Result Type Examples

```flowlang
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

function complexCalculation(x: int, y: int) -> Result<int, string> {
    let divided = safeDivide(x, y)?
    let doubled = divided * 2
    return Ok(doubled)
}
```

## Effect System

### Available Effects

FlowLang provides these built-in effects:

- `Database` - Database operations
- `Network` - Network/HTTP calls
- `Logging` - Logging operations
- `FileSystem` - File system operations
- `Memory` - Memory allocation operations
- `IO` - Input/output operations

### Declaring Effects

```flowlang
// Single effect
function logMessage(msg: string) uses [Logging] -> Result<string, string> {
    return Ok(msg)
}

// Multiple effects
function fetchUser(id: int) uses [Database, Network, Logging] -> Result<string, string> {
    return Ok("User data")
}

// All effects
function complexOperation() uses [Database, Network, Logging, FileSystem, Memory, IO] -> Result<string, string> {
    return Ok("Complex result")
}
```

### Effect Composition

When a function calls other functions with effects, it must declare all used effects:

```flowlang
function saveUser(name: string) uses [Database] -> Result<int, string> {
    return Ok(42)
}

function logAction(action: string) uses [Logging] -> Result<string, string> {
    return Ok(action)
}

// Must declare both Database and Logging effects
function processUser(name: string) uses [Database, Logging] -> Result<int, string> {
    let saved = saveUser(name)?
    let logged = logAction("User saved: " + name)?
    return Ok(saved)
}
```

### Pure Functions

Pure functions cannot have effects and cannot call functions with effects:

```flowlang
pure function calculate(a: int, b: int) -> int {
    return a + b  // No side effects allowed
}

// ❌ Error: Pure functions cannot have effects
pure function invalid() uses [Database] -> int {
    return 42
}
```

## Module System

### Module Declaration

```flowlang
module ModuleName {
    // Module contents
    function internalFunction() -> int {
        return 42
    }
    
    function publicFunction() -> string {
        return "Hello from module"
    }
    
    // Export declarations
    export {publicFunction}
}
```

### Import Statements

#### Simple Import

```flowlang
import ModuleName
```

#### Selective Import

```flowlang
import ModuleName.{function1, function2}
```

#### Wildcard Import

```flowlang
import ModuleName.*
```

#### Qualified Import

```flowlang
import Package.SubModule.{specificFunction}
```

### Using Imported Functions

```flowlang
import Math.{add, multiply}
import Utils.*

function calculate() -> int {
    let sum = add(5, 3)           // From selective import
    let doubled = double(sum)     // From wildcard import
    let product = Math.multiply(doubled, 2)  // Qualified call
    return product
}
```

### Export Statements

```flowlang
module UserService {
    function createUser(name: string) -> int {
        return 42
    }
    
    function deleteUser(id: int) -> bool {
        return true
    }
    
    function internalHelper() -> string {
        return "helper"
    }
    
    // Only export public functions
    export {createUser, deleteUser}
}
```

### Module Examples

**math_utils.flow:**
```flowlang
module MathUtils {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    pure function square(x: int) -> int {
        return x * x
    }
    
    export {add, multiply, square}
}
```

**string_utils.flow:**
```flowlang
module StringUtils {
    pure function reverse(s: string) -> string {
        // Implementation would reverse the string
        return s
    }
    
    pure function uppercase(s: string) -> string {
        // Implementation would convert to uppercase
        return s
    }
    
    export {reverse, uppercase}
}
```

**main.flow:**
```flowlang
import MathUtils.{add, square}
import StringUtils.*

function main() -> string {
    let sum = add(5, 3)
    let squared = square(sum)
    let message = $"Result: {squared}"
    return uppercase(message)
}
```

## String Features

### String Literals

```flowlang
"Simple string"
"String with \"quotes\""
"Multi-line\nstring\nwith\nbreaks"
"Path: C:\\Users\\Name\\file.txt"
""  // Empty string
```

### Escape Sequences

| Sequence | Description |
|----------|-------------|
| `\"` | Double quote |
| `\\` | Backslash |
| `\n` | Newline |
| `\t` | Tab |
| `\r` | Carriage return |

### String Concatenation

```flowlang
let greeting = "Hello" + " " + "World"
let message = "User: " + userName + ", Age: " + userAge
```

### String Interpolation

```flowlang
let name = "Alice"
let age = 30
let message = $"User {name} is {age} years old"

// Complex expressions in interpolation
let calculation = $"The result of {a} + {b} = {a + b}"

// Nested interpolation
let details = $"Status: {isActive ? "Active" : "Inactive"}"
```

### String Examples

```flowlang
function formatUserInfo(name: string, age: int, email: string) -> string {
    return $"User Profile:\nName: {name}\nAge: {age}\nEmail: {email}"
}

function buildErrorMessage(operation: string, code: int) -> string {
    let base = "Error in operation: " + operation
    return $"{base}\nError Code: {code}\nPlease contact support"
}
```

## Specification Blocks

### Overview

Specification blocks allow you to embed intent, business rules, and formal specifications directly with your code. This creates an atomic link between the "why" (specification) and the "how" (implementation), preventing context loss that commonly occurs in software development.

### Syntax

Specification blocks use the `/*spec ... spec*/` syntax and are placed directly before the function or module they describe:

```flowlang
/*spec
intent: "Brief description of what this function does and why"
rules:
  - "Business rule or constraint 1"
  - "Business rule or constraint 2"
postconditions:
  - "Expected outcome 1"
  - "Expected outcome 2"
source_doc: "optional-reference-to-external-documentation.md"
spec*/
function functionName(params) -> ReturnType {
    // Implementation
}
```

### Specification Fields

#### intent (Required)
A natural language description of the function's purpose and business context:

```flowlang
/*spec
intent: "Transfer funds between two accounts atomically, ensuring sufficient balance"
spec*/
```

#### rules (Optional)
A list of business rules, constraints, or validation requirements:

```flowlang
/*spec
intent: "Process user registration with validation"
rules:
  - "Email must be valid format"
  - "Password must be at least 8 characters"
  - "Username must be unique in system"
  - "User must be 13 or older"
spec*/
```

#### postconditions (Optional)
Expected outcomes or state changes after successful execution:

```flowlang
/*spec
intent: "Create new user account"
postconditions:
  - "User record exists in database"
  - "Welcome email is sent"
  - "User can log in with provided credentials"
  - "User has default permissions assigned"
spec*/
```

#### source_doc (Optional)
Reference to external documentation or requirements:

```flowlang
/*spec
intent: "Calculate tax based on jurisdiction rules"
source_doc: "requirements/tax-calculation-v2.3.md"
spec*/
```

### Function-Level Specifications

Place specification blocks directly before function declarations:

```flowlang
/*spec
intent: "Safely divide two numbers with explicit error handling"
rules:
  - "Divisor cannot be zero"
  - "Both numbers must be integers"
postconditions:
  - "Returns quotient on success"
  - "Returns descriptive error on division by zero"
spec*/
function safeDivide(a: int, b: int) -> Result<int, string> {
    guard b != 0 else {
        return Error("Division by zero not allowed")
    }
    return Ok(a / b)
}
```

### Module-Level Specifications

Specify the purpose and scope of entire modules:

```flowlang
/*spec
intent: "User authentication and authorization utilities"
rules:
  - "All operations must be logged for security audit"
  - "Password handling must follow security best practices"
  - "Session management follows OAuth 2.0 standards"
postconditions:
  - "Secure user authentication"
  - "Proper session lifecycle management"
source_doc: "security/auth-requirements.md"
spec*/
module AuthService {
    // Module implementation
}
```

### Complex Example

```flowlang
/*spec
intent: "Process e-commerce order with inventory management and payment"
rules:
  - "Must verify product availability before payment"
  - "Payment processing must be atomic (all-or-nothing)"
  - "Inventory must be reserved during payment processing"
  - "Failed payments must release reserved inventory"
  - "Successful orders must update inventory and create shipping record"
postconditions:
  - "Payment is processed successfully"
  - "Inventory is decremented by ordered quantities"
  - "Shipping record is created with tracking information"
  - "Customer receives order confirmation email"
  - "Order is logged for business analytics"
source_doc: "business/order-processing-workflow.md"
spec*/
function processOrder(order: Order) 
    uses [Database, Network, Logging] -> Result<OrderConfirmation, OrderError> {
    
    // Verify inventory availability
    let availability = checkInventory(order.items)?
    guard availability.allAvailable else {
        return Error(OrderError.InsufficientInventory)
    }
    
    // Reserve inventory during payment
    let reservation = reserveInventory(order.items)?
    
    // Process payment
    let payment = processPayment(order.payment, order.total)?
    
    // Update inventory and create shipping
    let inventory = updateInventory(order.items)?
    let shipping = createShipping(order)?
    
    // Send confirmation
    let confirmation = sendConfirmation(order, shipping.tracking)?
    
    return Ok(OrderConfirmation.new(payment.id, shipping.tracking))
}
```

### Benefits for LLM Development

1. **Context Preservation**: LLMs have complete understanding of both intent and implementation
2. **Consistency Checking**: Specifications can be validated against implementation
3. **Change Reasoning**: When modifying code, LLMs can ensure changes align with specifications
4. **Test Generation**: Specifications provide clear criteria for automated test generation
5. **Documentation**: Self-documenting code eliminates separate specification files

### Best Practices

1. **Be Specific**: Use concrete, actionable language in rules and postconditions
2. **Focus on Business Logic**: Capture the "why" not just the "what"
3. **Keep Current**: Update specifications when implementation changes
4. **Use Examples**: Include concrete examples in intent descriptions
5. **Reference External Docs**: Link to detailed requirements when appropriate

### Generated Code Impact

Specification blocks are preserved in generated C# code as comprehensive XML documentation:

**FlowLang Input:**
```flowlang
/*spec
intent: "Calculate user discount based on loyalty tier"
rules:
  - "Platinum users get 15% discount"
  - "Gold users get 10% discount"
  - "Silver users get 5% discount"
spec*/
function calculateDiscount(user: User) -> float
```

**Generated C# Output:**
```csharp
/// <summary>
/// Calculate user discount based on loyalty tier
/// 
/// Business Rules:
/// - Platinum users get 15% discount
/// - Gold users get 10% discount  
/// - Silver users get 5% discount
/// </summary>
/// <param name="user">Parameter of type User</param>
/// <returns>Returns float</returns>
public static float calculateDiscount(User user)
```

## Comments

### Single-line Comments

```flowlang
// This is a single-line comment
function example() -> int {
    return 42  // End-of-line comment
}
```

### Comment Examples

```flowlang
// Calculate user tax based on income and rate
function calculateTax(income: int, rate: int) -> int {
    // Apply tax rate as percentage
    return income * rate / 100
}

// Process user registration with validation
function registerUser(name: string, email: string) -> Result<int, string> {
    // Validate input parameters
    guard name != "" else {
        return Error("Name required")  // Return error for empty name
    }
    
    // Continue with registration logic
    return Ok(42)
}
```

## Keywords

### Reserved Keywords

These words are reserved and cannot be used as identifiers:

```
function    return      if          else        guard
let         pure        uses        module      import  
export      from        true        false       Ok
Error       Result      int         string      bool
Database    Network     Logging     FileSystem  Memory
IO          and         or          not
```

### Context-sensitive Keywords

Some keywords are only reserved in specific contexts:

- `Ok`, `Error` - Only in Result expressions
- Effect names (`Database`, `Network`, etc.) - Only in effect lists
- `export` - Only in module contexts

## Operators

### Operator Reference

| Category | Operator | Description | Associativity | Precedence |
|----------|----------|-------------|---------------|------------|
| Unary | `!` | Logical NOT | Right | 1 (highest) |
| Multiplicative | `*` | Multiplication | Left | 2 |
| Multiplicative | `/` | Division | Left | 2 |
| Additive | `+` | Addition/Concatenation | Left | 3 |
| Additive | `-` | Subtraction | Left | 3 |
| Relational | `>` | Greater than | Left | 4 |
| Relational | `<` | Less than | Left | 4 |
| Relational | `>=` | Greater than or equal | Left | 4 |
| Relational | `<=` | Less than or equal | Left | 4 |
| Equality | `==` | Equal | Left | 5 |
| Equality | `!=` | Not equal | Left | 5 |
| Logical AND | `&&` | Logical AND | Left | 6 |
| Logical OR | `\|\|` | Logical OR | Left | 7 |
| Error Prop | `?` | Error propagation | Postfix | 8 (lowest) |

### Operator Examples

```flowlang
// Arithmetic operations
let sum = a + b
let product = a * b
let quotient = a / b
let difference = a - b

// Comparison operations
if age >= 18 && age <= 65 {
    // Adult working age
}

// String operations
let fullName = firstName + " " + lastName
let message = $"Hello, {fullName}!"

// Logical operations
if isAdmin || (isUser && hasPermission) {
    // Access granted
}

// Error propagation
let result = riskyOperation()?
```

## Grammar

### EBNF Grammar

```ebnf
program = { statement } ;

statement = moduleDeclaration
          | importStatement  
          | exportStatement
          | functionDeclaration ;

moduleDeclaration = "module" identifier "{" { statement } "}" ;

importStatement = "import" qualifiedName [ "." ( "*" | "{" identifierList "}" ) ] ;

exportStatement = "export" "{" identifierList "}" ;

functionDeclaration = [ "pure" ] "function" identifier 
                     "(" [ parameterList ] ")" 
                     [ effectAnnotation ]
                     "->" type 
                     "{" { functionStatement } "}" ;

effectAnnotation = "uses" "[" identifierList "]" ;

parameterList = parameter { "," parameter } ;

parameter = identifier ":" type ;

functionStatement = returnStatement
                  | letStatement
                  | ifStatement
                  | guardStatement
                  | expressionStatement ;

returnStatement = "return" expression ;

letStatement = "let" identifier "=" expression ;

ifStatement = "if" expression "{" { functionStatement } "}" 
             [ "else" ( ifStatement | "{" { functionStatement } "}" ) ] ;

guardStatement = "guard" expression "else" "{" { functionStatement } "}" ;

expressionStatement = expression ;

expression = logicalOrExpression ;

logicalOrExpression = logicalAndExpression { "||" logicalAndExpression } ;

logicalAndExpression = equalityExpression { "&&" equalityExpression } ;

equalityExpression = comparisonExpression { ( "==" | "!=" ) comparisonExpression } ;

comparisonExpression = additiveExpression { ( ">" | "<" | ">=" | "<=" ) additiveExpression } ;

additiveExpression = multiplicativeExpression { ( "+" | "-" ) multiplicativeExpression } ;

multiplicativeExpression = unaryExpression { ( "*" | "/" ) unaryExpression } ;

unaryExpression = [ "!" ] primaryExpression ;

primaryExpression = numberLiteral
                  | stringLiteral  
                  | stringInterpolation
                  | identifier [ "(" [ argumentList ] ")" ] [ "?" ]
                  | qualifiedName [ "(" [ argumentList ] ")" ] [ "?" ]
                  | okExpression
                  | errorExpression
                  | "(" expression ")" ;

okExpression = "Ok" "(" expression ")" ;

errorExpression = "Error" "(" expression ")" ;

argumentList = expression { "," expression } ;

type = "int" | "string" | "bool" | resultType | identifier ;

resultType = "Result" "<" type "," type ">" ;

qualifiedName = identifier "." identifier ;

identifierList = identifier { "," identifier } ;

identifier = letter { letter | digit | "_" } ;

numberLiteral = digit { digit } ;

stringLiteral = '"' { character | escapeSequence } '"' ;

stringInterpolation = '$"' { character | escapeSequence | "{" expression "}" } '"' ;

escapeSequence = "\" ( '"' | "\" | "n" | "t" | "r" ) ;

letter = "a" | "b" | ... | "z" | "A" | "B" | ... | "Z" | "_" ;

digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;

character = (* any Unicode character except '"' and '\' *) ;
```

### Production Rules Explanation

- **program**: A FlowLang program consists of zero or more statements
- **statement**: Top-level declarations (modules, imports, exports, functions)
- **functionDeclaration**: Function definitions with optional purity and effects
- **expression**: Nested expression hierarchy following operator precedence
- **type**: Type annotations including basic types and Result types

## Summary

FlowLang provides a complete, type-safe programming experience with:

- **Strong typing** with type inference
- **Explicit effect system** for side effect management  
- **Safe error handling** with Result types
- **Module system** for code organization
- **Modern string features** including interpolation
- **Functional programming** with pure function support
- **Clear syntax** designed for LLM assistance

This reference covers all implemented language features. For practical examples and tutorials, see the other documentation files in this directory.