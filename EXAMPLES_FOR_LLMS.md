# FlowLang Examples for LLMs 🤖

**This guide shows LLMs how FlowLang solves common coding problems with explicit, predictable patterns.**

## 🎯 Why FlowLang is LLM-Friendly

Traditional languages have multiple ways to do everything. FlowLang has **one clear way** for each task.

## 🔥 Side-by-Side Comparisons

### Error Handling: FlowLang vs Others

❌ **C# - Multiple approaches confuse LLMs:**
```csharp
// Approach 1: Exceptions (hidden failure modes)
public int Divide(int a, int b) {
    if (b == 0) throw new DivideByZeroException();
    return a / b;
}

// Approach 2: Nullable returns (null propagation unclear)
public int? Divide(int a, int b) {
    if (b == 0) return null;
    return a / b;
}

// Approach 3: Out parameters (awkward)
public bool TryDivide(int a, int b, out int result) {
    if (b == 0) { result = 0; return false; }
    result = a / b; return true;
}
```

✅ **FlowLang - One clear way:**
```flowlang
// Only one way to handle errors - Result types
function divide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

// Error propagation is explicit with ?
function chainedDivision(a: int, b: int, c: int) -> Result<int, string> {
    let step1 = divide(a, b)?  // ? makes error propagation visible
    let step2 = divide(step1, c)?
    return Ok(step2)
}
```

### Side Effects: FlowLang vs Others

❌ **C# - Hidden side effects:**
```csharp
// LLM cannot know this function writes to database!
public string ProcessUser(string name) {
    Logger.Log($"Processing {name}");      // Hidden logging
    var user = Database.Save(name);        // Hidden database write
    EmailService.Send($"Welcome {name}");  // Hidden network call
    return user.Id;
}
```

✅ **FlowLang - Explicit effects:**
```flowlang
// All side effects are declared upfront
function processUser(name: string) uses [Database, Logging, Network] -> Result<string, string> {
    log_info($"Processing {name}")         // Logging effect declared
    let user = database_save(name)?        // Database effect declared
    send_email($"Welcome {name}")?         // Network effect declared
    return Ok(user.id)
}
```

### String Operations: FlowLang vs Others

❌ **Python - Multiple string approaches:**
```python
# Approach 1: % formatting
message = "Hello %s, you are %d years old" % (name, age)

# Approach 2: .format()
message = "Hello {}, you are {} years old".format(name, age)

# Approach 3: f-strings
message = f"Hello {name}, you are {age} years old"

# Approach 4: + concatenation
message = "Hello " + name + ", you are " + str(age) + " years old"
```

✅ **FlowLang - One clear way:**
```flowlang
// Only string interpolation with $ syntax
function greet(name: string, age: int) -> string {
    return $"Hello {name}, you are {age} years old"
}
```

## 🛡️ LLM Safety Examples

### Example 1: User Registration System

**Task:** Create a user registration function that validates input and saves to database.

✅ **FlowLang makes it impossible for LLMs to generate unsafe code:**

```flowlang
function registerUser(email: string, password: string) uses [Database, Logging] -> Result<string, string> {
    // Input validation with guard clauses
    guard email != "" else {
        return Error("Email cannot be empty")
    }
    
    guard email.Contains("@") else {
        return Error("Invalid email format")
    }
    
    guard password.Length >= 8 else {
        return Error("Password must be at least 8 characters")
    }
    
    // Log the attempt (effect declared)
    log_info($"Registering user: {email}")
    
    // Save to database (effect declared, error handling explicit)
    let userId = database_create_user(email, password)?
    
    return Ok(userId)
}
```

### Example 2: API Call with Retry Logic

**Task:** Make an HTTP request with retry logic and error handling.

✅ **FlowLang forces explicit error handling:**

```flowlang
function fetchUserData(userId: string, maxRetries: int) uses [Network, Logging] -> Result<string, string> {
    let attempts = 0
    
    while attempts < maxRetries {
        log_info($"Attempt {attempts + 1} for user {userId}")
        
        let response = http_get($"https://api.example.com/users/{userId}")
        
        // Pattern matching on Result type
        if response.IsOk {
            return Ok(response.Value)
        } else {
            log_warning($"Attempt {attempts + 1} failed: {response.Error}")
            attempts = attempts + 1
        }
    }
    
    return Error($"Failed after {maxRetries} attempts")
}
```

### Example 3: Configuration Loading

**Task:** Load configuration from file with validation.

✅ **FlowLang prevents runtime crashes:**

```flowlang
function loadConfig(filePath: string) uses [FileSystem] -> Result<Config, string> {
    // File existence check
    guard file_exists(filePath) else {
        return Error($"Config file not found: {filePath}")
    }
    
    // Read file (can fail)
    let content = file_read_text(filePath)?
    
    // Parse JSON (can fail)
    let jsonData = json_parse(content)?
    
    // Validate required fields
    guard jsonData.HasField("apiKey") else {
        return Error("Missing required field: apiKey")
    }
    
    guard jsonData.HasField("database") else {
        return Error("Missing required field: database")
    }
    
    // Create config object
    let config = Config {
        apiKey: jsonData.GetString("apiKey"),
        database: jsonData.GetString("database")
    }
    
    return Ok(config)
}
```

## 🤖 LLM Prompting Patterns

### Pattern 1: Error-Safe Function Generation

**Prompt:** "Create a FlowLang function that..."

The LLM will automatically:
1. Use `Result<T, E>` return types for fallible operations
2. Add appropriate `uses [Effects]` declarations
3. Include guard clauses for validation
4. Use `?` operator for error propagation

### Pattern 2: Effect-Aware API Design

**Prompt:** "Create a FlowLang service that reads from database and sends emails"

The LLM will automatically:
1. Declare `uses [Database, Network]` effects
2. Use Result types for all operations
3. Make error handling explicit
4. Chain operations safely with `?`

### Pattern 3: Pure vs Effectful Functions

**Prompt:** "Create calculation functions and data processing functions"

The LLM will automatically:
1. Mark mathematical functions as `pure`
2. Add effect declarations to I/O functions
3. Separate pure logic from side effects
4. Use appropriate return types

## 🎯 Testing LLM Understanding

### Quick Test 1: Function Classification

Ask the LLM to classify these FlowLang functions:

```flowlang
// Function A
pure function add(a: int, b: int) -> int {
    return a + b
}

// Function B  
function saveUser(name: string) uses [Database] -> Result<string, string> {
    return database_insert("users", name)
}

// Function C
function processOrder(order: Order) uses [Database, Network, Logging] -> Result<string, string> {
    log_info($"Processing order {order.id}")
    let saved = database_save_order(order)?
    let confirmation = send_confirmation_email(order.email)?
    return Ok(confirmation)
}
```

**Expected LLM responses:**
- Function A: Pure function, no side effects, safe for any context
- Function B: Database function, can fail, requires error handling
- Function C: Complex function with multiple effects, needs careful orchestration

### Quick Test 2: Error Handling

Ask the LLM to identify issues in this FlowLang code:

```flowlang
// ❌ This has issues - can the LLM spot them?
function badExample(input: string) -> string {
    let result = riskyOperation(input)  // Missing ? operator
    return result
}

function riskyOperation(input: string) -> Result<string, string> {
    return Ok(input.ToUpper())
}
```

**Expected LLM response:**
- Missing error handling for Result type
- Should use `?` operator or explicit Result handling
- Return type should be `Result<string, string>`

## 🏆 Benefits for LLM-Generated Code

### 1. **Predictable Patterns**
- One way to handle errors (Result types)
- One way to declare effects (uses clause)
- One way to interpolate strings ($ syntax)

### 2. **Compile-Time Safety**
- Effect system catches missing declarations
- Result types prevent null reference errors
- Type system catches mismatches

### 3. **Self-Documenting**
- Function signatures show all effects
- Result types show failure modes
- Pure functions guarantee no side effects

### 4. **Easy Refactoring**
- Effect changes require declaration updates
- Result type changes are visible
- Pure functions can be moved anywhere

## 🎪 Try It Yourself

Copy any of these examples into `.flow` files and run:

```bash
flowc run example.flow
```

The generated C# will be clean, safe, and follow .NET best practices!

---

**FlowLang: Making LLM-generated code predictable, safe, and maintainable.** 🤖✨