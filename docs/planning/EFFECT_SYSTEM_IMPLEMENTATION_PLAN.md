# Implementation Plan: Cadenza Effect System

This document outlines the steps required to fully implement the effect system (`pure` functions and `uses` annotations) in the Cadenza compiler.

## 1. Current Status Analysis

My analysis of the compiler source code in `src/Cadenza.Core` reveals that a significant portion of the groundwork for this feature is already in place.

### What is Working:

*   **Lexer (`Lexer.cs`):** The lexer correctly recognizes the `pure` and `uses` keywords, as well as the individual effect types (`Database`, `Network`, etc.), and assigns them the correct `TokenType`.
*   **AST (`Ast.cs`):** The Abstract Syntax Tree is properly designed to handle effects. The `FunctionDeclaration` record already contains the necessary properties:
    *   `bool IsPure`
    *   `List<string>? Effects`
*   **Parser (`Parser.cs`):** The parser correctly handles the grammar for `pure` functions and `uses` clauses. It successfully populates the `IsPure` and `Effects` properties of the `FunctionDeclaration` AST node.

### What is Missing:

The implementation gap is in the **transpilation and semantic analysis** phases.

1.  **Transpiler (`Transpiler.cs`):** The `CSharpGenerator` class currently **ignores** the `IsPure` and `Effects` properties on the `FunctionDeclaration` node. The generated C# code is the same whether a function is declared `pure` or has a `uses` clause.
2.  **Semantic Analysis (Non-existent):** There is no validation step that enforces the rules of the effect system. For example, the compiler does not currently check if a `pure` function calls an impure one, or if a function uses an effect that it did not declare.

The foundation is solid. The task is to make the compiler *act* on the information that the parser is already gathering.

## 2. Implementation Steps

The implementation can be broken down into two major phases:
1.  **Phase 1: Transpiler Modifications** - Make the generated C# code reflect the effects.
2.  **Phase 2: Semantic Analysis** - Enforce the correctness of the effects.

---

### Phase 1: Modify the C# Generator for `async`/`await` and Attributes

The goal here is to translate Cadenza's effect annotations into corresponding C# attributes and to correctly handle asynchronous operations.

**File to Modify:** `src/Cadenza.Core/Transpiler.cs`
**Method to Modify:** `GenerateFunction(FunctionDeclaration func)`

#### Step 2.1: Implement `isPure` Transpilation

When `func.IsPure` is `true`, the generated C# method should be decorated with the `[System.Diagnostics.Contracts.Pure]` attribute.

```csharp
// In CSharpGenerator.cs, inside GenerateFunction

private MemberDeclarationSyntax GenerateFunction(FunctionDeclaration func)
{
    // ... (existing code for parameters, returnType, body)

    var method = MethodDeclaration(returnType, Identifier(func.Name))
        .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
        .AddParameterListParameters(parameters)
        .WithBody(body);

    // *** NEW LOGIC START ***
    if (func.IsPure)
    {
        var pureAttribute = AttributeList(
            SingletonSeparatedList(
                Attribute(IdentifierName("System.Diagnostics.Contracts.Pure"))
            )
        );
        method = method.AddAttributeLists(pureAttribute);
    }
    // *** NEW LOGIC END ***

    // ... (existing code for specification trivia)

    return method;
}
```

#### Step 2.2: Implement `uses` Transpilation and `async`/`await` Handling

When `func.Effects` is not null, the generated C# method should be decorated with a custom `[Effect("...")]` attribute for each declared effect. Additionally, functions with `Network` or `Database` effects should be made `async` and return `Task` or `Task<T>`.

First, define the `EffectAttribute` in the runtime library.

**File to Create/Modify:** `src/Cadenza.Core/CadenzaRuntime.cs` (or a new file in that project)

```csharp
// Add this class to the Cadenza.Runtime namespace
namespace Cadenza.Runtime
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EffectAttribute : Attribute
    {
        public string Name { get; }

        public EffectAttribute(string name)
        {
            Name = name;
        }
    }
}
```

Next, update the transpiler to add these attributes and handle `async`/`await`.

**File to Modify:** `src/Cadenza.Core/Transpiler.cs`
**Method to Modify:** `GenerateFunction(FunctionDeclaration func)`

```csharp
// In CSharpGenerator.cs, inside GenerateFunction

private MemberDeclarationSyntax GenerateFunction(FunctionDeclaration func)
{
    // Determine if the function needs to be async based on its effects
    bool isAsync = func.Effects != null && 
                   (func.Effects.Contains("Network") || func.Effects.Contains("Database"));

    // Adjust return type for async methods
    TypeSyntax returnType;
    if (isAsync)
    {
        if (func.ReturnType == "void" || func.ReturnType == null) // Assuming 'void' or no return type for Cadenza functions
        {
            returnType = ParseTypeName("Task");
        }
        else
        {
            returnType = GenericName(Identifier("Task"))
                .AddTypeArgumentListArguments(ParseTypeName(func.ReturnType));
        }
    }
    else
    {
        returnType = ParseTypeName(func.ReturnType ?? "void");
    }

    var body = Block(
        // Pass isAsync to GenerateStatement to handle await expressions within the body
        func.Body.Select(s => GenerateStatement(s, isAsync))
    );
    
    var method = MethodDeclaration(returnType, Identifier(func.Name))
        .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));
    
    if (isAsync)
    {
        method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword)); // Add async modifier
    }

    method = method.AddParameterListParameters(parameters)
        .WithBody(body);

    // Handle isPure (from previous step)
    if (func.IsPure)
    {
        // ...
    }

    // *** NEW LOGIC FOR EFFECTS ATTRIBUTES ***
    if (func.Effects != null && func.Effects.Any())
    {
        var attributes = func.Effects.Select(effect =>
            Attribute(IdentifierName("Effect"))
                .WithArgumentList(
                    AttributeArgumentList(
                        SingletonSeparatedList(
                            AttributeArgument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(effect)
                                )
                            )
                        )
                    )
                )
        );

        var attributeList = AttributeList(SeparatedList(attributes));
        method = method.AddAttributeLists(attributeList);
    }
    // *** END NEW LOGIC FOR EFFECTS ATTRIBUTES ***

    // Handle specification trivia
    if (func.Specification != null)
    {
        // ...
    }

    return method;
}

// You will also need to update GenerateStatement and GenerateExpression
// to conditionally add 'await' to CallExpression nodes if the called function
// is asynchronous. This will require the CSharpGenerator to have access to
// the semantic analysis results (the symbol table of function effects) or
// to perform a simplified check.

// Example (conceptual, requires symbol table lookup):
// private ExpressionSyntax GenerateCallExpression(CallExpression callExpr, bool isCallerAsync)
// {
//     // ... (existing code)
//     if (isCallerAsync && IsFunctionAsync(callExpr.Name)) // IsFunctionAsync would query the symbol table
//     {
//         return AwaitExpression(invocation); // Wrap in AwaitExpression
//     }
//     return invocation;
// }
```

---

### Phase 2: Implement Semantic Analysis

This is the most critical part, as it enforces the language rules. This requires creating a new **semantic analysis pass** that runs after parsing and before transpilation.

This pass will traverse the AST, build a symbol table containing function signatures (including effects), and validate the rules.

#### Step 2.3: Create a `SemanticAnalyzer` Class

This new class will have a method like `Analyze(ProgramNode ast)` which will:
1.  Traverse the AST to build a symbol table of all `FunctionDeclaration` nodes, storing their names, parameters, and effect information (`IsPure` and `Effects`).
2.  **Crucially, it should also determine and store whether each function is `async` (i.e., has `Network` or `Database` effects) in the symbol table.**
3.  Traverse the AST a second time, this time analyzing the *body* of each function.

#### Step 2.4: Implement Effect Rule Validation and `async` Propagation

Inside the `SemanticAnalyzer`, when analyzing a function's body, for every `CallExpression` found:

1.  **Look up the callee:** Find the `FunctionDeclaration` of the function being called in the symbol table.
2.  **Check for Purity Violations:**
    *   If the **current function** is `pure`, check if the **callee** is also `pure`.
    *   If the callee is *not* pure (i.e., it has effects or is not marked `pure`), throw a `SemanticErrorException` (e.g., "Pure function 'caller' cannot call impure function 'callee'.").
3.  **Check for Undeclared Effects:**
    *   If the **current function** is *not* pure, get the list of its declared `Effects`.
    *   Get the list of the **callee's** `Effects`.
    *   For each effect in the callee's list, check if it is present in the current function's declared effects list.
    *   If a callee's effect is not declared by the caller, throw a `SemanticErrorException` (e.g., "Function 'caller' uses undeclared effect 'Network' from calling 'callee'.").
4.  **`async` Propagation (Semantic Check):**
    *   If the **callee** is marked as `async` (due to `Network` or `Database` effects), ensure that the **current function** (the caller) is also marked as `async` (i.e., it must also declare `Network` or `Database` effects, or be explicitly marked `async` if such a concept is introduced in Cadenza).
    *   This ensures that the `async` nature propagates up the call stack, preventing synchronous calls to asynchronous operations.

This analysis ensures the integrity of the effect system and the correctness of `async`/`await` propagation.

## Summary of Plan

1.  **Update `Transpiler.cs`:** Modify `GenerateFunction` to:
    *   Add `[Pure]` attributes.
    *   Add `[Effect("...")]` attributes.
    *   Conditionally add `async` modifier and `Task`/`Task<T>` return types for functions with `Network` or `Database` effects.
    *   Update `GenerateStatement` and `GenerateExpression` to add `await` to calls to functions that are determined to be asynchronous (this determination will come from the `SemanticAnalyzer`).
2.  **Update `CadenzaRuntime.cs`:** Add the definition for the `public class EffectAttribute`.
3.  **Create `SemanticAnalyzer.cs`:** Implement a new analysis pass that:
    *   Builds a symbol table of all functions and their effects.
    *   **Determines and stores whether each function is `async` based on its effects.**
    *   Validates that `pure` functions only call other `pure` functions.
    *   Validates that any effects used by called functions are explicitly declared in the caller's `uses` clause.
    *   **Validates that `async` functions propagate their `async` nature up the call stack.**
4.  **Integrate:** The main compiler logic in `Compiler.cs` should be updated to run the `SemanticAnalyzer` after parsing and before transpiling.