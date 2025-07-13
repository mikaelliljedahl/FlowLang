# FlowLang Core Transpiler - TODO Items

## Critical Issues

### Guard Statement Support Missing ⚠️
**Priority: HIGH**

**Issue**: Guard statements are not currently supported by the parser, causing compilation failures.

**Example Failing Code**:
```flowlang
function validate_age(age: int) -> Result<string, string> {
    guard age >= 0 else {
        return Error("Age cannot be negative")
    }
    return Ok("Valid")
}
```

**Error**: `Unexpected token '{' at line 2`

**Impact**: 
- Multiple example files fail to compile (`specification_example.flow`, `test_guard.flow`, etc.)
- Guard statements are used extensively across the codebase (40+ occurrences)
- Core language feature is non-functional

**Expected Behavior**:
Guard statements should transpile to equivalent if-else logic in C#:
```csharp
public static Result<string, string> validate_age(int age) {
    if (!(age >= 0)) {
        return Result.Error("Age cannot be negative");
    }
    return Result.Ok("Valid");
}
```

**Files Affected**:
- `examples/specification_example.flow` - Contains 10 guard statements
- `examples/test_guard.flow` - Simple guard test case
- `examples/comprehensive_control_flow_demo.flow` - Multiple guard clauses
- Many other examples using guard syntax

**Technical Notes**:
- Parser needs to recognize `guard <condition> else { <block> }` syntax
- Should handle complex boolean expressions in guard conditions
- Must integrate with existing Result type error handling

---

## Parser Enhancement Needs

### Complex Nested Expressions
- **Issue**: Parser fails on complex nested if-else expressions
- **Priority**: Medium
- **Impact**: Limits expressiveness of conditional logic

### Error Reporting
- **Issue**: Parser errors could be more descriptive
- **Priority**: Low
- **Impact**: Developer experience

---

## Completion Status
- ✅ **Spec Blocks**: Fully implemented and working
- ✅ **Result Types**: Complete error handling support
- ✅ **Effect System**: Working with proper tracking
- ✅ **Basic Control Flow**: If statements, function declarations
- ❌ **Guard Statements**: Not implemented
- ❌ **Complex Expressions**: Limited nested expression support