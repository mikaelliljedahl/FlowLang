# C# to FlowLang Migration Guide

This guide helps C# developers understand FlowLang concepts and migrate existing C# code to FlowLang. FlowLang transpiles to C#, so you keep all the benefits of the .NET ecosystem while gaining FlowLang's safety and explicitness features.

## Table of Contents

1. [Key Differences Overview](#key-differences-overview)
2. [Basic Syntax Translation](#basic-syntax-translation)
3. [Error Handling Migration](#error-handling-migration)
4. [Type System Differences](#type-system-differences)
5. [Effect System Introduction](#effect-system-introduction)
6. [Module System vs Namespaces](#module-system-vs-namespaces)
7. [String Handling Improvements](#string-handling-improvements)
8. [Control Flow Enhancements](#control-flow-enhancements)
9. [Best Practices Migration](#best-practices-migration)
10. [Common Migration Patterns](#common-migration-patterns)
11. [Migration Examples](#migration-examples)
12. [Gradual Migration Strategy](#gradual-migration-strategy)

## Key Differences Overview

| Aspect | C# | FlowLang |
|--------|----|---------| 
| **Error Handling** | Exceptions | Result types |
| **Side Effects** | Implicit | Explicit effect system |
| **Null Safety** | Nullable reference types | No nulls by default |
| **Functions** | Methods in classes | Standalone functions |
| **Immutability** | Mutable by default | Encouraged immutability |
| **String Formatting** | String interpolation | Enhanced string interpolation |
| **Module System** | Namespaces | Explicit import/export |

## Basic Syntax Translation

### Function Declarations

**C#:**
```csharp
public static int Add(int a, int b)
{
    return a + b;
}

public static string Greet(string name)
{
    return $"Hello, {name}!";
}
```

**FlowLang:**
```flowlang
pure function add(a: int, b: int) -> int {
    return a + b
}

pure function greet(name: string) -> string {
    return $"Hello, {name}!"
}
```

### Variable Declarations

**C#:**
```csharp
var x = 42;
var message = "Hello";
var result = CalculateValue();
```

**FlowLang:**
```flowlang
let x = 42
let message = "Hello"
let result = calculateValue()
```

### Function Calls

**C#:**
```csharp
var sum = Add(5, 3);
var greeting = Greet("Alice");
```

**FlowLang:**
```flowlang
let sum = add(5, 3)
let greeting = greet("Alice")
```

## Error Handling Migration

### From Exceptions to Result Types

**C# (Exception-based):**
```csharp
public static int Divide(int a, int b)
{
    if (b == 0)
        throw new ArgumentException("Division by zero");
    
    return a / b;
}

public static void ProcessData()
{
    try
    {
        var result = Divide(10, 0);
        Console.WriteLine($"Result: {result}");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
```

**FlowLang (Result-based):**
```flowlang
function divide(a: int, b: int) -> Result<int, string> {
    if b == 0 {
        return Error("Division by zero")
    }
    return Ok(a / b)
}

function processData() -> Result<string, string> {
    let result = divide(10, 0)?
    return Ok($"Result: {result}")
}
```

### Error Propagation Patterns

**C#:**
```csharp
public static int ComplexCalculation(int x, int y, int z)
{
    try
    {
        var step1 = Divide(x, y);
        var step2 = Divide(step1, z);
        return step2 * 2;
    }
    catch (Exception ex)
    {
        // Handle or rethrow
        throw;
    }
}
```

**FlowLang:**
```flowlang
function complexCalculation(x: int, y: int, z: int) -> Result<int, string> {
    let step1 = divide(x, y)?
    let step2 = divide(step1, z)?
    return Ok(step2 * 2)
}
```

## Type System Differences

### Basic Types

| C# Type | FlowLang Type | Notes |
|---------|---------------|-------|
| `int` | `int` | Same |
| `string` | `string` | Same |
| `bool` | `bool` | Same |
| `void` | No equivalent | Use Result types or explicit returns |
| `T?` | Not needed | No nulls in FlowLang |

### Generic Types

**C#:**
```csharp
public static T Identity<T>(T value)
{
    return value;
}
```

**FlowLang:** (Not directly supported, use specific types)
```flowlang
pure function identityInt(value: int) -> int {
    return value
}

pure function identityString(value: string) -> string {
    return value
}
```

### Nullable Types

**C# (with nullable reference types):**
```csharp
public static string? GetUserName(int userId)
{
    if (userId <= 0)
        return null;
    
    return "User" + userId;
}

public static string ProcessUser(int userId)
{
    var name = GetUserName(userId);
    if (name == null)
        return "Unknown user";
    
    return $"Hello, {name}";
}
```

**FlowLang (with Result types):**
```flowlang
function getUserName(userId: int) -> Result<string, string> {
    if userId <= 0 {
        return Error("Invalid user ID")
    }
    return Ok("User" + userId)
}

function processUser(userId: int) -> Result<string, string> {
    let name = getUserName(userId)?
    return Ok($"Hello, {name}")
}
```

## Effect System Introduction

The effect system makes side effects explicit, which is a major difference from C#.

### Database Operations

**C#:**
```csharp
public static class UserService
{
    public static void SaveUser(string name)
    {
        // Database operation - side effect not explicit
        Database.Save(new User { Name = name });
    }
    
    public static User GetUser(int id)
    {
        // Another implicit side effect
        return Database.Get<User>(id);
    }
}
```

**FlowLang:**
```flowlang
function saveUser(name: string) uses [Database] -> Result<int, string> {
    // Side effect explicitly declared
    return Ok(42)  // User ID
}

function getUser(id: int) uses [Database] -> Result<string, string> {
    // Side effect explicitly declared
    return Ok("User name")
}
```

### Logging Operations

**C#:**
```csharp
public static void ProcessOrder(int orderId)
{
    Logger.Info($"Processing order {orderId}");
    
    // Process order...
    
    Logger.Info("Order processed successfully");
}
```

**FlowLang:**
```flowlang
function processOrder(orderId: int) uses [Logging] -> Result<string, string> {
    // Logging effect declared
    let message = $"Processing order {orderId}"
    
    // Process order...
    
    return Ok("Order processed successfully")
}
```

### Pure Functions

**C#:**
```csharp
// No explicit indication this is pure
public static int CalculateTax(int amount, int rate)
{
    return amount * rate / 100;
}
```

**FlowLang:**
```flowlang
// Explicitly marked as pure - compiler enforces it
pure function calculateTax(amount: int, rate: int) -> int {
    return amount * rate / 100
}
```

## Module System vs Namespaces

### C# Namespaces

**C#:**
```csharp
namespace MyProject.Utils
{
    public static class MathHelper
    {
        public static int Add(int a, int b) => a + b;
        public static int Multiply(int a, int b) => a * b;
    }
}

namespace MyProject.Services
{
    using MyProject.Utils;
    
    public static class CalculationService
    {
        public static int Calculate(int x, int y)
        {
            return MathHelper.Add(x, MathHelper.Multiply(y, 2));
        }
    }
}
```

**FlowLang Modules:**

**math_utils.flow:**
```flowlang
module MathUtils {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    export {add, multiply}
}
```

**calculation_service.flow:**
```flowlang
import MathUtils.{add, multiply}

function calculate(x: int, y: int) -> int {
    return add(x, multiply(y, 2))
}
```

### Import Patterns

**C#:**
```csharp
using System;
using System.Collections.Generic;
using static MyProject.Utils.MathHelper;
```

**FlowLang:**
```flowlang
import MathUtils.*                    // Wildcard import
import StringUtils.{reverse, upper}   // Selective import
import ComplexModule                  // Simple import
```

## String Handling Improvements

### String Interpolation

**C#:**
```csharp
var name = "Alice";
var age = 30;
var message = $"User {name} is {age} years old";

// Complex expressions
var calculation = $"Result: {Add(5, 3)}";
```

**FlowLang:**
```flowlang
let name = "Alice"
let age = 30
let message = $"User {name} is {age} years old"

// Complex expressions
let calculation = $"Result: {add(5, 3)}"
```

### String Concatenation

**C#:**
```csharp
var fullName = firstName + " " + lastName;
var path = "C:\\Users\\" + username + "\\Documents";
```

**FlowLang:**
```flowlang
let fullName = firstName + " " + lastName
let path = "C:\\Users\\" + username + "\\Documents"
```

## Control Flow Enhancements

### Guard Clauses

**C# (traditional approach):**
```csharp
public static string ProcessUser(string name, int age)
{
    if (string.IsNullOrEmpty(name))
    {
        throw new ArgumentException("Name cannot be empty");
    }
    
    if (age < 0 || age > 150)
    {
        throw new ArgumentException("Invalid age");
    }
    
    // Continue processing...
    return "User processed";
}
```

**FlowLang (with guards):**
```flowlang
function processUser(name: string, age: int) -> Result<string, string> {
    guard name != "" else {
        return Error("Name cannot be empty")
    }
    
    guard age >= 0 && age <= 150 else {
        return Error("Invalid age")
    }
    
    // Continue processing...
    return Ok("User processed")
}
```

### Conditional Logic

**C#:**
```csharp
public static string GetUserType(int age)
{
    if (age < 13)
        return "Child";
    else if (age < 18)
        return "Teen";
    else if (age < 65)
        return "Adult";
    else
        return "Senior";
}
```

**FlowLang:**
```flowlang
function getUserType(age: int) -> string {
    if age < 13 {
        return "Child"
    } else if age < 18 {
        return "Teen"  
    } else if age < 65 {
        return "Adult"
    } else {
        return "Senior"
    }
}
```

## Best Practices Migration

### From Mutable to Immutable

**C# (mutable style):**
```csharp
public static List<int> ProcessNumbers(List<int> numbers)
{
    var result = new List<int>();
    
    foreach (var num in numbers)
    {
        if (num > 0)
        {
            result.Add(num * 2);
        }
    }
    
    return result;
}
```

**FlowLang (functional style):**
```flowlang
pure function doublePositive(num: int) -> int {
    return num * 2
}

pure function isPositive(num: int) -> bool {
    return num > 0
}

// In a real implementation, you'd work with collections
// This shows the functional approach
function processNumber(num: int) -> Result<int, string> {
    if isPositive(num) {
        return Ok(doublePositive(num))
    }
    return Error("Number must be positive")
}
```

### From Classes to Functions

**C#:**
```csharp
public class Calculator
{
    private readonly int _factor;
    
    public Calculator(int factor)
    {
        _factor = factor;
    }
    
    public int Multiply(int value)
    {
        return value * _factor;
    }
    
    public int Add(int a, int b)
    {
        return a + b + _factor;
    }
}
```

**FlowLang (functional approach):**
```flowlang
pure function multiply(value: int, factor: int) -> int {
    return value * factor
}

pure function addWithFactor(a: int, b: int, factor: int) -> int {
    return a + b + factor
}

// Usage pattern: pass factor explicitly
function calculate(value: int) -> int {
    let factor = 10
    return multiply(value, factor)
}
```

## Common Migration Patterns

### Pattern 1: Service Classes

**C# Service Class:**
```csharp
public class UserService
{
    private readonly ILogger _logger;
    private readonly IUserRepository _repository;
    
    public UserService(ILogger logger, IUserRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    public async Task<User> CreateUserAsync(string name, string email)
    {
        _logger.LogInformation($"Creating user: {name}");
        
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name required");
            
        var user = new User { Name = name, Email = email };
        await _repository.SaveAsync(user);
        
        _logger.LogInformation($"User created with ID: {user.Id}");
        return user;
    }
}
```

**FlowLang Module:**
```flowlang
module UserService {
    function createUser(name: string, email: string) 
        uses [Logging, Database] 
        -> Result<int, string> {
        
        guard name != "" else {
            return Error("Name required")
        }
        
        // Would log and save in real implementation
        let userId = 42
        return Ok(userId)
    }
    
    export {createUser}
}
```

### Pattern 2: Data Validation

**C# Validation:**
```csharp
public static class UserValidator
{
    public static ValidationResult ValidateUser(UserDto user)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(user.Name))
            errors.Add("Name is required");
            
        if (user.Age < 0 || user.Age > 150)
            errors.Add("Age must be between 0 and 150");
            
        if (string.IsNullOrEmpty(user.Email) || !IsValidEmail(user.Email))
            errors.Add("Valid email is required");
            
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}
```

**FlowLang Validation:**
```flowlang
function validateUser(name: string, age: int, email: string) -> Result<string, string> {
    guard name != "" else {
        return Error("Name is required")
    }
    
    guard age >= 0 && age <= 150 else {
        return Error("Age must be between 0 and 150")
    }
    
    guard email != "" else {
        return Error("Valid email is required")
    }
    
    return Ok("User is valid")
}
```

### Pattern 3: Configuration and Settings

**C# Configuration:**
```csharp
public class AppSettings
{
    public string ConnectionString { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool EnableLogging { get; set; }
}

public class ConfigurationService
{
    public static AppSettings LoadSettings()
    {
        // Load from file, environment, etc.
        return new AppSettings
        {
            ConnectionString = "Server=localhost;Database=mydb",
            TimeoutSeconds = 30,
            EnableLogging = true
        };
    }
}
```

**FlowLang Configuration:**
```flowlang
module Configuration {
    function loadConnectionString() uses [FileSystem] -> Result<string, string> {
        // Load from configuration file
        return Ok("Server=localhost;Database=mydb")
    }
    
    function loadTimeout() uses [FileSystem] -> Result<int, string> {
        return Ok(30)
    }
    
    pure function getDefaultLogging() -> bool {
        return true
    }
    
    export {loadConnectionString, loadTimeout, getDefaultLogging}
}
```

## Migration Examples

### Example 1: API Controller

**C# Web API Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request.Name, request.Email);
            return Ok(new UserDto { Id = user.Id, Name = user.Name, Email = user.Email });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}
```

**FlowLang API Module:**
```flowlang
module UsersApi {
    function createUser(name: string, email: string) 
        uses [Database, Logging, Network] 
        -> Result<string, string> {
        
        let validationResult = validateUser(name, email)?
        let userId = UserService.createUser(name, email)?
        
        return Ok($"User created with ID: {userId}")
    }
    
    function validateUser(name: string, email: string) -> Result<string, string> {
        guard name != "" else {
            return Error("Name is required")
        }
        
        guard email != "" else {
            return Error("Email is required")
        }
        
        return Ok("Valid")
    }
    
    export {createUser}
}
```

### Example 2: Data Processing Pipeline

**C# Processing Pipeline:**
```csharp
public class DataProcessor
{
    public async Task<ProcessingResult> ProcessDataAsync(RawData data)
    {
        try
        {
            var validated = ValidateData(data);
            var transformed = TransformData(validated);
            var enriched = await EnrichDataAsync(transformed);
            var saved = await SaveDataAsync(enriched);
            
            return new ProcessingResult { Success = true, Id = saved.Id };
        }
        catch (ValidationException ex)
        {
            return new ProcessingResult { Success = false, Error = ex.Message };
        }
        catch (Exception ex)
        {
            return new ProcessingResult { Success = false, Error = "Processing failed" };
        }
    }
}
```

**FlowLang Processing Pipeline:**
```flowlang
module DataProcessor {
    function processData(rawData: string) 
        uses [Database, Network, Logging] 
        -> Result<int, string> {
        
        let validated = validateData(rawData)?
        let transformed = transformData(validated)?
        let enriched = enrichData(transformed)?
        let saved = saveData(enriched)?
        
        return Ok(saved)
    }
    
    pure function validateData(data: string) -> Result<string, string> {
        guard data != "" else {
            return Error("Data cannot be empty")
        }
        return Ok(data)
    }
    
    pure function transformData(data: string) -> Result<string, string> {
        // Transform logic
        return Ok("transformed_" + data)
    }
    
    function enrichData(data: string) uses [Network] -> Result<string, string> {
        // Enrich with external data
        return Ok("enriched_" + data)
    }
    
    function saveData(data: string) uses [Database] -> Result<int, string> {
        // Save to database
        return Ok(42)
    }
    
    export {processData}
}
```

## Gradual Migration Strategy

### Phase 1: New Code in FlowLang

1. **Start with utilities**: Migrate pure functions first
2. **Create FlowLang modules** for new features
3. **Use FlowLang for business logic** that doesn't require C# integration

### Phase 2: Interface Boundaries

1. **Create FlowLang services** that C# can call through transpiled code
2. **Define clear boundaries** between C# and FlowLang code
3. **Use Result types** at interfaces for consistent error handling

### Phase 3: Core Migration

1. **Migrate service classes** to FlowLang modules
2. **Convert validation logic** to use guard clauses and Result types
3. **Refactor error handling** from exceptions to Results

### Phase 4: Complete Migration

1. **Move remaining logic** to FlowLang
2. **Optimize for FlowLang patterns** (immutability, effects)
3. **Clean up** any remaining C# wrapper code

### Migration Checklist

- [ ] Identify pure functions for easy migration
- [ ] Map C# classes to FlowLang modules
- [ ] Convert exceptions to Result types
- [ ] Make side effects explicit with effect annotations
- [ ] Replace null checks with Result types
- [ ] Convert validation logic to guard clauses
- [ ] Organize code into proper modules with exports
- [ ] Update string formatting to FlowLang interpolation
- [ ] Test transpiled C# code for compatibility

## Benefits After Migration

### 1. Explicit Error Handling
- No more unhandled exceptions
- Clear error propagation paths
- Type-safe error handling

### 2. Effect Transparency
- Clear understanding of side effects
- Better testability
- Easier reasoning about code

### 3. Null Safety
- No null reference exceptions
- Explicit handling of optional values
- More robust code

### 4. Better Documentation
- Function signatures describe behavior
- Effects and types serve as documentation
- Clearer interfaces

### 5. LLM-Friendly Code
- Explicit patterns help AI understand code
- Consistent style reduces ambiguity
- Self-documenting effect system

## Conclusion

Migrating from C# to FlowLang brings significant benefits in safety, explicitness, and maintainability. The migration can be gradual, starting with new code and moving existing code over time. The resulting FlowLang code transpiles to clean, efficient C#, so you maintain all the performance and ecosystem benefits while gaining FlowLang's safety features.

Key takeaways:
- **Start small**: Begin with pure functions and utilities
- **Use Result types**: Replace exceptions with explicit error handling
- **Make effects explicit**: Declare side effects in function signatures  
- **Embrace immutability**: Prefer functional patterns over mutable state
- **Organize with modules**: Use FlowLang's module system for clear boundaries

For more information, see:
- [Getting Started Guide](getting-started.md) - Basic FlowLang usage
- [Language Reference](language-reference.md) - Complete syntax reference
- [CLI Reference](cli-reference.md) - Development tools
- [Examples](examples/) - Working code samples