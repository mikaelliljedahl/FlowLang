# Cadenza Syntax Cheat Sheet 📝

**Quick reference for Cadenza syntax - perfect for LLMs and humans!**

## 🏗️ Basic Structure

### Function Declaration
```cadenza
// Basic function
function functionName(param: type) -> returnType {
    return value
}

// Pure function (no side effects)
pure function add(a: int, b: int) -> int {
    return a + b
}

// Function with effects
function saveData(data: string) uses [Database, Logging] -> Result<string, string> {
    return Ok("saved")
}
```

### Main Function (Entry Point)
```cadenza
function main() -> string {
    return "Hello, Cadenza!"
}
```

## 📊 Types

### Basic Types
```cadenza
// Primitive types
let number: int = 42
let text: string = "hello"
let flag: bool = true

// Arrays
let numbers: int[] = [1, 2, 3, 4, 5]
let names: string[] = ["Alice", "Bob"]
```

### Result Types (Error Handling)
```cadenza
// Function that can fail
function divide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

// Using Result types
function useResult() -> string {
    let result = divide(10, 2)
    if result.IsOk {
        return $"Success: {result.Value}"
    } else {
        return $"Error: {result.Error}"
    }
}

// Error propagation with ?
function chainResults() -> Result<int, string> {
    let result1 = divide(10, 2)?  // ? automatically propagates errors
    let result2 = divide(result1, 2)?
    return Ok(result2)
}
```

## 🔤 String Operations

### String Interpolation
```cadenza
function greet(name: string, age: int) -> string {
    return $"Hello {name}! You are {age} years old."
}

// Nested expressions
function calculate(a: int, b: int) -> string {
    return $"The sum of {a} and {b} is {a + b}"
}
```

### String Concatenation
```cadenza
function combine(first: string, second: string) -> string {
    return first + " " + second
}
```

## 🔀 Control Flow

### If/Else Statements
```cadenza
function checkScore(score: int) -> string {
    if score >= 90 {
        return "Excellent"
    } else if score >= 70 {
        return "Good"
    } else {
        return "Needs improvement"
    }
}
```

### Guard Clauses
```cadenza
function validateInput(value: int) -> Result<string, string> {
    guard value >= 0 else {
        return Error("Value must be positive")
    }
    
    guard value <= 100 else {
        return Error("Value must be <= 100")
    }
    
    return Ok("Valid input")
}
```

### Boolean Logic
```cadenza
function complexCheck(a: int, b: int, flag: bool) -> bool {
    return (a > 0 && b > 0) || (flag && a != b)
}

function negate(value: bool) -> bool {
    return !value
}
```

## ⚡ Effect System

### Pure Functions
```cadenza
// No side effects allowed
pure function multiply(x: int, y: int) -> int {
    return x * y
}
```

### Functions with Effects
```cadenza
// Common effects: Database, Network, Logging, FileSystem, Memory, IO
function fetchUserData(id: string) uses [Database, Network] -> Result<string, string> {
    // Implementation would use database and network
    return Ok("user data")
}

function processWithLogging(data: string) uses [Logging] -> Result<string, string> {
    // Log the operation
    return Ok("processed: " + data)
}
```

### Effect Propagation
```cadenza
// Callers must declare effects of functions they call
function orchestrate(id: string) uses [Database, Network, Logging] -> Result<string, string> {
    let data = fetchUserData(id)?        // Uses Database, Network
    let result = processWithLogging(data)? // Uses Logging
    return result
}
```

## 📋 Specification Blocks

Specification blocks link business intent directly to the code, ensuring context is never lost. They are version-controlled with the implementation.

### Structure and Fields
A `spec` block is a structured comment placed directly above a function or module.

```cadenza
/*spec
intent: "Required: What this function/module does and why it exists."
rules:
  - "Optional: A list of business rules or constraints the code must follow."
postconditions:
  - "Optional: A list of expected outcomes or state changes after successful execution."
source_doc: "Optional: A reference to external documentation (e.g., requirements.md)."
spec*/
```

- **`intent`**: The core purpose. What business value does this code provide?
- **`rules`**: Testable business logic, constraints, and edge cases.
- **`postconditions`**: The expected state of the system after the code runs successfully.

### Function-Level Example
This example shows how a `spec` block documents a critical business function.

```cadenza
/*spec
intent: "Process a product return request, validating eligibility and calculating the refund."
rules:
  - "Return must be requested within 30 days of the delivery date."
  - "The product must be in its original condition (not damaged or used)."
  - "Digital products are not eligible for return."
  - "The refund amount is the original price minus any restocking fee."
  - "Shipping costs are non-refundable unless the item was defective."
postconditions:
  - "A return request is created with a unique RMA number."
  - "The calculated refund amount is held pending approval."
  - "The customer is sent an email with return shipping instructions."
  - "The original order's status is updated to 'return-pending'."
source_doc: "policy/return-and-refund-policy.md"
spec*/
function processReturnRequest(orderId: OrderId, productId: ProductId, reason: string) 
    uses [Database, Email, Inventory] -> Result<ReturnAuthorization, ReturnError> {
    
    let order = getOrder(orderId)?
    guard order.deliveryDate.isWithinDays(30) else {
        return Error(ReturnError.PastReturnWindow)
    }
    
    let product = getProduct(productId)?
    guard !product.isDigital else {
        return Error(ReturnError.DigitalProductNotReturnable)
    }
    
    let restockingFee = product.category.restockingFee
    let refundAmount = product.price - restockingFee
    
    let rma = createReturnAuthorization(orderId, productId, refundAmount, reason)?
    sendReturnInstructions(order.customerEmail, rma)?
    updateOrderStatus(orderId, "ReturnPending")?
    
    return Ok(rma)
}
```

### Module-Level Example
Specs can also define the high-level purpose of an entire module.

```cadenza
/*spec
intent: "A comprehensive user management system for the e-commerce platform."
rules:
  - "All user data operations must be GDPR compliant."
  - "Password handling must follow OWASP security guidelines."
  - "Administrative operations require elevated permissions and are logged for audit."
postconditions:
  - "Provides a secure and compliant user lifecycle management system."
  - "Maintains a comprehensive audit trail for all sensitive operations."
spec*/
module UserManagement {
    // ... function definitions for createUser, updateProfile, etc.
    
    export { createUser, updateProfile }
}
```

## 📦 Module System

### Defining Modules
```cadenza
module MathUtils {
    pure function square(x: int) -> int {
        return x * x
    }
    
    pure function cube(x: int) -> int {
        return x * x * x
    }
    
    // Only export what should be public
    export { square, cube }
}

module StringUtils {
    pure function upper(text: string) -> string {
        return text.ToUpper()
    }
    
    export { upper }
}
```

### Using Modules
```cadenza
// Selective imports
import MathUtils.{square}
import StringUtils.{upper}

// Wildcard imports
import MathUtils.*

// Qualified calls (without import)
function useModule() -> int {
    return MathUtils.square(5)
}

function main() -> string {
    let num = square(4)           // From selective import
    let text = upper("hello")     // From selective import
    return $"{text}: {num}"
}
```

## 🎯 Common Patterns

### Validation Pattern
```cadenza
function validateUser(name: string, age: int) -> Result<string, string> {
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    guard age >= 0 && age <= 150 else {
        return Error("Invalid age")
    }
    
    return Ok("Valid user")
}
```

### Processing Pattern
```cadenza
function processData(input: string) uses [Database] -> Result<string, string> {
    // Validate input
    guard input != "" else {
        return Error("Input cannot be empty")
    }
    
    // Process
    let processed = input.ToUpper()
    
    // Save and return
    let saveResult = saveToDatabase(processed)?
    return Ok(saveResult)
}
```

### Chaining Pattern
```cadenza
function complexOperation(input: int) -> Result<int, string> {
    let step1 = validateInput(input)?
    let step2 = processStep(step1)?
    let step3 = finalizeStep(step2)?
    return Ok(step3)
}
```

## 🔧 CLI Commands

### File Operations
```bash
# Run single file
cadenzac run myfile.cdz

# Create new project
cadenzac new my-project

# Build project
cadenzac build

# Run tests
cadenzac test
```

### Analysis and Quality
```bash
# Static analysis
cadenzac lint

# Security audit
cadenzac audit

# Start Language Server (IDE integration)
cadenzac lsp
```

### Help
```bash
# General help
cadenzac --help

# Command-specific help
cadenzac help run
cadenzac help new
```

## 💡 Best Practices

1. **Always use Result types** for operations that can fail
2. **Mark pure functions** with the `pure` keyword
3. **Declare all effects** with `uses [Effect1, Effect2]`
4. **Use guard clauses** for input validation
5. **Use string interpolation** instead of concatenation
6. **Keep functions small** and focused
7. **Use meaningful names** for variables and functions

## 🚀 Quick Examples

**Hello World:**
```cadenza
function main() -> string {
    return "Hello, Cadenza!"
}
```

**Safe Division:**
```cadenza
function safeDivide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}
```

**String Interpolation:**
```cadenza
function greet(name: string) -> string {
    return $"Hello, {name}!"
}
```

**Guard Clauses:**
```cadenza
function validate(age: int) -> Result<string, string> {
    guard age >= 0 else {
        return Error("Age must be positive")
    }
    return Ok("Valid")
}
```

---

**That's it!** You now have everything you need to write Cadenza code. 🎉

For more details, see the [full documentation](docs/) or try the [quick start examples](QUICKSTART.md).