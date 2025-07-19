The C# compiler is struggling with type inference for the `Select` method in these specific contexts. Even after explicitly specifying `TSource` and `TResult` (e.g., `Select<ASTNode, StatementSyntax?>`), the compiler still tries to match it to the `Select` overload that includes an `int` index parameter (`Func<TSource, int, TResult>`), which is not what was intended. This suggests:
*   **Ambiguity in Lambda Signature:** Despite attempts to clarify, the lambda's signature might still be ambiguous to the compiler in combination with the `Select` overloads.
*   **Roslyn Compiler Behavior:** There might be a subtle interaction with Roslyn's type inference engine that makes this particular LINQ pattern problematic in this codebase's setup.

## Ideas for Future development of type analysis

### 1. Comprehensive Type Analysis Phase

*   **Suggestion:** Implement a dedicated type analysis or type checking phase *before* C# code generation. This phase would traverse the entire Cadenza AST and resolve the precise .NET type for every expression and variable. This resolved type information (e.g., `System.Collections.Generic.List<System.Int32>`, `Cadenza.Core.Result<System.Int32, System.String>`) would then be explicitly attached to the AST nodes.
*   **Benefit:** The `CSharpGenerator` would no longer need to perform type inference on the fly or rely on string parsing (`ParseResultType`). It would simply read the pre-resolved, fully qualified .NET types from the AST nodes, eliminating ambiguity and making code generation much more robust, especially for complex generic types like nested `Result`s.

### 2. Refine `GenerateResultExpression` and `ParseResultType`

*   **Suggestion:** If a full type analysis phase is not immediately feasible, focus on making `ParseResultType` more robust for nested generics. The current implementation's logic for finding the "top-level" comma might be flawed for complex nesting. A recursive parsing approach for `ParseResultType` might be necessary, or a more sophisticated string manipulation technique that correctly identifies the outer generic arguments.
*   **Suggestion:** Ensure `GenerateResultExpression` always receives and correctly utilizes the *exact* expected `Result` type for the current level of nesting. When generating an inner `Result.Ok` or `Result.Error`, the `successType` of the *outer* `Result` should be passed as the `expectedType` for the *inner* call.

### 3. Address `CS0411` in LINQ `Select` Calls

*   **Suggestion:** Replace the problematic `Select().OfType()` patterns with explicit `foreach` loops and `if` statements for filtering and casting. While less "functional," this will bypass the compiler's type inference issues and ensure the code compiles.
    ```csharp
    // Instead of:
    // Block(ifStmt.ThenBody.Select<ASTNode, StatementSyntax?>(stmt => GenerateStatementSyntax(stmt, expectedType)).OfType<StatementSyntax>())
    
    // Use:
    var thenStatements = new List<StatementSyntax>();
    foreach (var node in ifStmt.ThenBody)
    {
        var generatedStmt = GenerateStatementSyntax(node, expectedType);
        if (generatedStmt is StatementSyntax actualStmt)
        {
            thenStatements.Add(actualStmt);
        }
    }
    Block(thenStatements)
    ```
*   **Alternative:** If LINQ is strongly preferred, investigate if there are specific Roslyn APIs or patterns for generating LINQ expressions that avoid this type inference ambiguity. This might involve using `Expression.Lambda` or other Roslyn factories to build the lambda expression more explicitly.

