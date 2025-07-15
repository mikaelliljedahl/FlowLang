# Effect System Implementation

## Overview
Implement Cadenza's effect tracking system to explicitly declare and track side effects in functions.

## Goals
- Add `uses` effect annotations to function signatures
- Track common effects: Database, Network, Logging, FileSystem
- Generate C# comments initially (future: runtime checking)
- Validate effect annotations at compile time

## Technical Requirements

### 1. Lexer Changes
- Add `uses` keyword token
- Add effect names as identifiers: Database, Network, Logging, etc.

### 2. Parser Changes
- Parse effect annotations: `uses [Database, Network]`
- Parse effect lists in function signatures
- Validate effect names against known effects

### 3. AST Changes
- Add `EffectAnnotation` AST node
- Update `FunctionDeclaration` to include effects
- Add effect validation logic

### 4. Code Generator Changes
- Generate C# comments for effects
- Add effect information to method documentation
- Future: Generate effect checking code

## Example Cadenza Code
```cadenza
function save_user(user: User) 
    uses [Database, Logging] 
    -> Result<UserId, DatabaseError> {
    
    log_info("Saving user: " + user.name)
    let result = database.save(user)
    return result
}

function fetch_user_data(user_id: UserId) 
    uses [Database, Network, Logging] 
    -> Result<UserData, ApiError> {
    
    let user = database.get_user(user_id)?
    let profile = api.fetch_profile(user.profile_id)?
    log_info("Fetched user data for: " + user.name)
    
    return Ok(UserData {
        user: user,
        profile: profile
    })
}

pure function calculate_tax(amount: Money) -> Money {
    return amount * 0.08
}
```

## Expected C# Output
```csharp
/// <summary>
/// Effects: Database, Logging
/// </summary>
public static Result<UserId, DatabaseError> save_user(User user)
{
    // Effect: Logging
    log_info("Saving user: " + user.name);
    // Effect: Database
    var result = database.save(user);
    return result;
}

/// <summary>
/// Effects: Database, Network, Logging
/// </summary>
public static Result<UserData, ApiError> fetch_user_data(UserId user_id)
{
    // Effect: Database
    var getUserResult = database.get_user(user_id);
    if (getUserResult.IsError) return getUserResult;
    var user = getUserResult.Value;
    
    // Effect: Network
    var fetchProfileResult = api.fetch_profile(user.profile_id);
    if (fetchProfileResult.IsError) return fetchProfileResult;
    var profile = fetchProfileResult.Value;
    
    // Effect: Logging
    log_info("Fetched user data for: " + user.name);
    
    return Result<UserData, ApiError>.Ok(new UserData
    {
        user = user,
        profile = profile
    });
}

/// <summary>
/// Pure function - no side effects
/// </summary>
public static Money calculate_tax(Money amount)
{
    return amount * 0.08;
}
```

## Implementation Tasks
1. Add `uses` and `pure` keywords to lexer
2. Add effect name tokens to lexer
3. Add effect annotation parsing to parser
4. Add effect validation logic
5. Create effect AST nodes
6. Update function declaration AST
7. Generate C# effect comments
8. Add effect information to documentation
9. Validate effect usage in function bodies
10. Test with effect examples

## Success Criteria
- Effect annotations parse correctly
- Effect validation works properly
- Generated C# includes effect documentation
- Pure functions are properly marked
- Effect tracking is accurate

## Dependencies
- Current lexer/parser/codegen infrastructure
- Result types (for effect examples)
- String literals (for logging messages)
- Control flow (for effect examples)