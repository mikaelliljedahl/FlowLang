# Module System Implementation

## Overview
Add basic module system to Cadenza with imports, exports, and namespace generation.

## Goals
- Add module declarations and imports
- Support explicit exports from modules
- Generate C# namespaces from modules
- Basic dependency resolution

## Technical Requirements

### 1. Lexer Changes
- Add `module` keyword token
- Add `import` keyword token
- Add `export` keyword token
- Add `from` keyword token

### 2. Parser Changes
- Parse module declarations: `module MyModule { ... }`
- Parse import statements: `import MyModule.*` or `import MyModule.{function1, function2}`
- Parse export statements: `export { function1, function2 }`
- Handle module-qualified names: `MyModule.function1()`

### 3. AST Changes
- Add `ModuleDeclaration` AST node
- Add `ImportStatement` AST node
- Add `ExportStatement` AST node
- Add `QualifiedName` AST node

### 4. Code Generator Changes
- Generate C# namespaces from modules
- Generate using statements from imports
- Handle module-qualified function calls
- Generate proper C# class structure

## Example Cadenza Code
```cadenza
// math.cdz
module Math {
    export function add(a: int, b: int) -> int {
        return a + b
    }
    
    export function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    function internal_helper(x: int) -> int {
        return x * 2
    }
}

// main.cdz
import Math.{add, multiply}
import Utils.*

function main() -> int {
    let result = add(5, 3)
    let product = multiply(result, 2)
    return product
}
```

## Expected C# Output
```csharp
// Math.cs
namespace Math
{
    public static class MathModule
    {
        public static int add(int a, int b)
        {
            return a + b;
        }
        
        public static int multiply(int a, int b)
        {
            return a * b;
        }
        
        private static int internal_helper(int x)
        {
            return x * 2;
        }
    }
}

// Program.cs
using Math;
using Utils;

public static class Program
{
    public static int main()
    {
        var result = MathModule.add(5, 3);
        var product = MathModule.multiply(result, 2);
        return product;
    }
}
```

## Implementation Tasks
1. Add module keywords to lexer
2. Add import/export keywords to lexer
3. Add module declaration parsing
4. Add import statement parsing
5. Add export statement parsing
6. Add qualified name parsing
7. Create module AST nodes
8. Generate C# namespaces
9. Generate using statements
10. Handle module-qualified calls
11. Add module resolution logic
12. Test with multi-module examples

## Success Criteria
- Module declarations parse correctly
- Import/export statements work properly
- Generated C# uses proper namespaces
- Module-qualified calls work correctly
- Multi-file projects build successfully

## Dependencies
- Current lexer/parser/codegen infrastructure
- Enhanced CLI (for multi-file builds)
- All previous language features