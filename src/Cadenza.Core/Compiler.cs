// Cadenza Core Compiler - Compilation and Generation Classes
// Extracted from cadenzac-core.cs

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cadenza.Core;

// =============================================================================
// C# CODE GENERATOR
// =============================================================================

public class CSharpGenerator
{
    private readonly HashSet<string> _generatedNamespaces = new();
    private readonly List<string> _usingStatements = new();
    private readonly Dictionary<string, string> _importedSymbols = new(); // Track imported symbols
    private readonly Dictionary<string, string> _moduleNamespaces = new(); // Track module name to namespace mapping
    private string? _currentFunctionReturnType; // Track current function's return type for Result type inference
    
    public SyntaxTree GenerateFromAST(ProgramNode program)
    {
        var namespaceMembers = new Dictionary<string, List<MemberDeclarationSyntax>>();
        var globalMembers = new List<MemberDeclarationSyntax>();
        
        // Add Result struct and helper class if any function uses Result types
        var resultTypes = GenerateResultTypes();
        globalMembers.AddRange(resultTypes);
        
        // Add Option struct and helper class if any function uses Option types
        var optionTypes = GenerateOptionTypes();
        globalMembers.AddRange(optionTypes);
        
        // First pass: Process imports and modules to build symbol mapping
        foreach (var statement in program.Statements)
        {
            if (statement is ImportStatement import)
            {
                ProcessImportStatement(import);
            }
            else if (statement is ModuleDeclaration module)
            {
                // Register the module for qualified call resolution
                _moduleNamespaces[module.Name] = $"Cadenza.Modules.{module.Name}";
                
                // Add using statement for the module namespace
                _usingStatements.Add($"Cadenza.Modules.{module.Name}");
                
            }
        }
        
        // Second pass: Generate actual C# code
        var standaloneMembers = new List<MemberDeclarationSyntax>();
        
        foreach (var statement in program.Statements)
        {
            var member = GenerateStatement(statement);
            if (member != null)
            {
                if (statement is ModuleDeclaration module)
                {
                    var namespaceName = $"Cadenza.Modules.{module.Name}";
                    if (!namespaceMembers.ContainsKey(namespaceName))
                    {
                        namespaceMembers[namespaceName] = new List<MemberDeclarationSyntax>();
                    }
                    namespaceMembers[namespaceName].AddRange(member as IEnumerable<MemberDeclarationSyntax> ?? new[] { member });
                }
                else if (statement is FunctionDeclaration)
                {
                    // Collect standalone functions to wrap in a class
                    standaloneMembers.Add(member);
                }
                else
                {
                    globalMembers.Add(member);
                }
            }
        }
        
        // Wrap standalone functions in a Program class
        if (standaloneMembers.Count > 0)
        {
            var programClass = ClassDeclaration("CadenzaProgram")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(standaloneMembers.ToArray());
            globalMembers.Add(programClass);
        }
        
        // Create compilation unit
        var compilationUnit = CompilationUnit();
        
        // Add using statements
        _usingStatements.Add("System");
        _usingStatements.Add("System.Collections.Generic");
        _usingStatements.Add("System.Threading.Tasks");
        // _usingStatements.Add("Cadenza.Runtime"); // Only add if runtime features are used
        
        foreach (var usingStmt in _usingStatements.Distinct())
        {
            compilationUnit = compilationUnit.AddUsings(UsingDirective(ParseName(usingStmt)));
        }
        
        // Check for main function and generate entry point
        bool hasMainFunction = false;
        string mainNamespace = "";
        bool isStandaloneMain = false;
        
        // Look for main function in modules
        foreach (var statement in program.Statements)
        {
            if (statement is ModuleDeclaration module)
            {
                foreach (var stmt in module.Body)
                {
                    if (stmt is FunctionDeclaration func && func.Name == "main")
                    {
                        hasMainFunction = true;
                        mainNamespace = $"Cadenza.Modules.{module.Name}";
                        break;
                    }
                }
            }
            else if (statement is FunctionDeclaration func && func.Name == "main")
            {
                hasMainFunction = true;
                isStandaloneMain = true;
                mainNamespace = "CadenzaProgram";
                break;
            }
        }
        
        // Add top-level statement FIRST if main function exists
        if (hasMainFunction)
        {
            var entryPoint = GenerateTopLevelStatement(mainNamespace);
            compilationUnit = compilationUnit.AddMembers(entryPoint);
        }
        
        // Add namespace members
        foreach (var (namespaceName, members) in namespaceMembers)
        {
            var namespaceDecl = NamespaceDeclaration(ParseName(namespaceName))
                .AddMembers(members.ToArray());
            compilationUnit = compilationUnit.AddMembers(namespaceDecl);
        }
        
        // Add global members (structs and classes)
        compilationUnit = compilationUnit.AddMembers(globalMembers.ToArray());
        
        return CSharpSyntaxTree.Create(compilationUnit);
    }
    
    private GlobalStatementSyntax GenerateTopLevelStatement(string mainNamespace)
    {
        ExpressionSyntax mainCall;
        
        if (mainNamespace == "CadenzaProgram")
        {
            // For standalone main function: CadenzaProgram.main()
            mainCall = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("CadenzaProgram"),
                    IdentifierName("main")
                )
            );
        }
        else
        {
            // Generate: MainNamespace.ClassName.main(); 
            // Extract module name from namespace
            var moduleName = mainNamespace.Replace("Cadenza.Modules.", "");
            var fullPath = $"{mainNamespace}.{moduleName}";
            
            mainCall = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseName(mainNamespace),
                        IdentifierName(moduleName)
                    ),
                    IdentifierName("main")
                )
            );
        }
        
        var statement = ExpressionStatement(mainCall);
        return GlobalStatement(statement);
    }
    
    private MemberDeclarationSyntax[] GenerateResultTypes()
    {
        // Generate Result<T,E> class
        var resultStruct = ClassDeclaration("Result")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddTypeParameterListParameters(
                TypeParameter("T"),
                TypeParameter("E"))
            .AddMembers(
                FieldDeclaration(
                    VariableDeclaration(ParseTypeName("bool"))
                        .AddVariables(VariableDeclarator("IsSuccess")))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
                FieldDeclaration(
                    VariableDeclaration(ParseTypeName("T"))
                        .AddVariables(VariableDeclarator("Value")))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
                FieldDeclaration(
                    VariableDeclaration(ParseTypeName("E"))
                        .AddVariables(VariableDeclarator("Error")))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
                PropertyDeclaration(ParseTypeName("bool"), "IsError")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithBody(Block(
                                ReturnStatement(
                                    PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, IdentifierName("IsSuccess"))
                                )
                            ))
                    ),
                ConstructorDeclaration("Result")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("isSuccess")).WithType(ParseTypeName("bool")),
                        Parameter(Identifier("value")).WithType(ParseTypeName("T")),
                        Parameter(Identifier("error")).WithType(ParseTypeName("E")))
                    .WithBody(Block(
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("IsSuccess"), IdentifierName("isSuccess"))),
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("Value"), IdentifierName("value"))),
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("Error"), IdentifierName("error"))))));

        // Generate Result helper class
        var resultClass = GenerateResultClass();

        return new MemberDeclarationSyntax[] { resultStruct, resultClass };
    }
    
    private ClassDeclarationSyntax GenerateResultClass()
    {
        return ClassDeclaration("Result")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddMembers(
                // Generic OK method with explicit type parameters
                MethodDeclaration(ParseTypeName("Result<T, E>"), "Ok")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddTypeParameterListParameters(
                        TypeParameter("T"),
                        TypeParameter("E"))
                    .AddParameterListParameters(Parameter(Identifier("value")).WithType(ParseTypeName("T")))
                    .WithBody(Block(
                        ReturnStatement(
                            ObjectCreationExpression(ParseTypeName("Result<T, E>"))
                                .AddArgumentListArguments(
                                    Argument(LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                                    Argument(IdentifierName("value")),
                                    Argument(LiteralExpression(SyntaxKind.DefaultLiteralExpression)))))),
                
                // Generic Error method with explicit type parameters
                MethodDeclaration(ParseTypeName("Result<T, E>"), "Error")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddTypeParameterListParameters(
                        TypeParameter("T"),
                        TypeParameter("E"))
                    .AddParameterListParameters(Parameter(Identifier("error")).WithType(ParseTypeName("E")))
                    .WithBody(Block(
                        ReturnStatement(
                            ObjectCreationExpression(ParseTypeName("Result<T, E>"))
                                .AddArgumentListArguments(
                                    Argument(LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                                    Argument(LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                    Argument(IdentifierName("error"))))))
            );
    }
    
    private MemberDeclarationSyntax[] GenerateOptionTypes()
    {
        // Generate Option<T> struct
        var optionStruct = StructDeclaration("Option")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddTypeParameterListParameters(TypeParameter("T"))
            .AddMembers(
                FieldDeclaration(
                    VariableDeclaration(ParseTypeName("bool"))
                        .AddVariables(VariableDeclarator("HasValue")))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
                FieldDeclaration(
                    VariableDeclaration(ParseTypeName("T"))
                        .AddVariables(VariableDeclarator("Value")))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
                ConstructorDeclaration("Option")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("hasValue")).WithType(ParseTypeName("bool")),
                        Parameter(Identifier("value")).WithType(ParseTypeName("T")))
                    .WithBody(Block(
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("HasValue"), IdentifierName("hasValue"))),
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("Value"), IdentifierName("value"))))));

        // Generate Option helper class
        var optionClass = GenerateOptionClass();

        return new MemberDeclarationSyntax[] { optionStruct, optionClass };
    }
    
    private ClassDeclarationSyntax GenerateOptionClass()
    {
        return ClassDeclaration("Option")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddMembers(
                MethodDeclaration(ParseTypeName("Option<T>"), "Some")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddTypeParameterListParameters(TypeParameter("T"))
                    .AddParameterListParameters(Parameter(Identifier("value")).WithType(ParseTypeName("T")))
                    .WithBody(Block(
                        ReturnStatement(
                            ObjectCreationExpression(ParseTypeName("Option<T>"))
                                .AddArgumentListArguments(
                                    Argument(LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                                    Argument(IdentifierName("value")))))),
                
                MethodDeclaration(ParseTypeName("Option<T>"), "None")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddTypeParameterListParameters(TypeParameter("T"))
                    .WithBody(Block(
                        ReturnStatement(
                            ObjectCreationExpression(ParseTypeName("Option<T>"))
                                .AddArgumentListArguments(
                                    Argument(LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                                    Argument(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))))
            );
    }
    
    private MemberDeclarationSyntax? GenerateStatement(ASTNode statement)
    {
        return statement switch
        {
            FunctionDeclaration func => GenerateFunctionDeclaration(func),
            ModuleDeclaration module => GenerateModuleDeclaration(module),
            ComponentDeclaration component => GenerateComponentDeclaration(component),
            _ => null
        };
    }
    
    private MethodDeclarationSyntax GenerateFunctionDeclaration(FunctionDeclaration func)
    {
        // Set the current function return type for Result type inference
        _currentFunctionReturnType = func.ReturnType;
        
        var method = MethodDeclaration(
            ParseTypeName(MapCadenzaTypeToCSharp(func.ReturnType ?? "void")),
            func.Name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));
        
        // Add parameters
        foreach (var param in func.Parameters)
        {
            method = method.AddParameterListParameters(
                Parameter(Identifier(param.Name))
                    .WithType(ParseTypeName(MapCadenzaTypeToCSharp(param.Type)))
            );
        }
        
        // Generate XML documentation from specification block or effects
        var xmlDocumentation = GenerateXmlDocumentation(func);
        if (xmlDocumentation.Any())
        {
            method = method.WithLeadingTrivia(xmlDocumentation);
        }
        
        // Generate method body
        var statements = new List<StatementSyntax>();
        _errorPropagationContext = null; // Reset context for this function
        
        for (int i = 0; i < func.Body.Count; i++)
        {
            var stmt = func.Body[i];
            var isLastStatement = i == func.Body.Count - 1;
            
            // Generate the statement
            var generated = GenerateStatementSyntax(stmt);
            
            // Handle error propagation if it was encountered
            if (_errorPropagationContext != null)
            {
                var context = _errorPropagationContext;
                _errorPropagationContext = null; // Clear after use
                
                var tempVarName = $"{context.VariableName}_result";
                var resultExpr = GenerateExpression(context.Expression.Expression);
                
                // Add the error propagation statements
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(tempVarName)
                                .WithInitializer(EqualsValueClause(resultExpr))
                        )
                ));
                
                // Create a new Result object with the correct return type for the current function
                var (successType, errorType) = ParseResultType(_currentFunctionReturnType);
                var returnError = ReturnStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Result"),
                            GenericName("Error")
                                .WithTypeArgumentList(
                                    TypeArgumentList(SeparatedList<TypeSyntax>(new[]
                                    {
                                        ParseTypeName(successType),
                                        ParseTypeName(errorType)
                                    }))
                                )
                        )
                    ).AddArgumentListArguments(
                        Argument(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(tempVarName),
                                IdentifierName("Error")
                            )
                        )
                    )
                );

                statements.Add(IfStatement(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(tempVarName),
                        IdentifierName("IsError")
                    ),
                    returnError
                ));
                
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(context.VariableType)
                        .AddVariables(
                            VariableDeclarator(context.VariableName)
                                .WithInitializer(EqualsValueClause(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(tempVarName),
                                        IdentifierName("Value")
                                    )
                                ))
                        )
                ));
                
                continue; // Skip adding the null statement
            }
            
            // If this is the last statement and it's an expression (not a return statement or control flow statement),
            // wrap it in a return statement
            if (isLastStatement && stmt is not ReturnStatement && stmt is not IfStatement && stmt is not GuardStatement && func.ReturnType != null && func.ReturnType != "void")
            {
                var returnStmt = ReturnStatement(GenerateExpression(stmt));
                statements.Add(returnStmt);
            }
            else if (generated != null)
            {
                statements.Add(generated);
            }
        }
        
        method = method.WithBody(Block(statements));
        
        // Clear the current function return type
        _currentFunctionReturnType = null;
        
        return method;
    }
    
    private string MapCadenzaTypeToCSharp(string flowLangType)
    {
        return flowLangType switch
        {
            "string" => "string",
            "int" => "int", 
            "bool" => "bool",
            "Unit" => "void",
            _ => flowLangType
        };
    }
    
    private MemberDeclarationSyntax GenerateModuleDeclaration(ModuleDeclaration module)
    {
        var members = new List<MemberDeclarationSyntax>();
        
        foreach (var stmt in module.Body)
        {
            var member = GenerateStatement(stmt);
            if (member != null)
            {
                members.Add(member);
            }
        }
        
        return ClassDeclaration(module.Name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddMembers(members.ToArray());
    }
    
    private ClassDeclarationSyntax GenerateComponentDeclaration(ComponentDeclaration component)
    {
        var componentClass = ClassDeclaration($"{component.Name}Component")
            .AddModifiers(Token(SyntaxKind.PublicKeyword));
        
        // Add state properties
        if (component.State?.Any() == true)
        {
            foreach (var state in component.State)
            {
                var property = PropertyDeclaration(ParseTypeName(state.Type), state.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    );
                
                componentClass = componentClass.AddMembers(property);
            }
        }
        
        // Add render method
        var renderMethod = MethodDeclaration(ParseTypeName("string"), "Render")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithBody(Block(
                ReturnStatement(
                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("<!-- Component render output -->"))
                )
            ));
        
        componentClass = componentClass.AddMembers(renderMethod);
        
        return componentClass;
    }
    
    private StatementSyntax? GenerateStatementSyntax(ASTNode statement)
    {
        return statement switch
        {
            ReturnStatement ret => GenerateReturnStatement(ret),
            LetStatement let => GenerateLetStatement(let),
            IfStatement ifStmt => GenerateIfStatement(ifStmt),
            GuardStatement guard => GenerateGuardStatement(guard),
            MatchExpression match => ExpressionStatement(GenerateMatchExpression(match)),
            CallExpression call => ExpressionStatement(GenerateCallExpression(call)),
            MethodCallExpression methodCall => ExpressionStatement(GenerateMethodCallExpression(methodCall)),
            _ => ExpressionStatement(GenerateExpression(statement))
        };
    }
    
    private ReturnStatementSyntax GenerateReturnStatement(ReturnStatement ret)
    {
        if (ret.Expression != null)
        {
            // Skip generating return for Unit literal in void functions
            if (ret.Expression is Identifier id && id.Name == "Unit")
            {
                return ReturnStatement();
            }
            return ReturnStatement(GenerateExpression(ret.Expression));
        }
        return ReturnStatement();
    }
    
    private StatementSyntax GenerateLetStatement(LetStatement let)
    {
        var variableType = let.Type != null ? ParseTypeName(MapCadenzaTypeToCSharp(let.Type)) : IdentifierName("var");
        
        // Handle error propagation specially - store context for later expansion
        if (let.Expression is ErrorPropagation errorProp)
        {
            // Store the error propagation context for function-level processing
            _errorPropagationContext = new ErrorPropagationContext 
            {
                VariableName = let.Name, 
                Expression = errorProp, 
                VariableType = variableType 
            };
            
            // Return null to signal that this should be handled at function level
            return null;
        }
        
        return LocalDeclarationStatement(
            VariableDeclaration(variableType)
                .AddVariables(
                    VariableDeclarator(let.Name)
                        .WithInitializer(EqualsValueClause(GenerateExpression(let.Expression)))
                )
        );
    }
    
    private ErrorPropagationContext _errorPropagationContext;
    
    private class ErrorPropagationContext
    {
        public string VariableName { get; set; }
        public ErrorPropagation Expression { get; set; }
        public TypeSyntax VariableType { get; set; }
    }
    
    private IfStatementSyntax GenerateIfStatement(IfStatement ifStmt)
    {
        var ifSyntax = IfStatement(
            GenerateExpression(ifStmt.Condition),
            Block(ifStmt.ThenBody.Select(GenerateStatementSyntax).Where(s => s != null).Cast<StatementSyntax>())
        );
        
        if (ifStmt.ElseBody?.Any() == true)
        {
            ifSyntax = ifSyntax.WithElse(
                ElseClause(
                    Block(ifStmt.ElseBody.Select(GenerateStatementSyntax).Where(s => s != null).Cast<StatementSyntax>())
                )
            );
        }
        
        return ifSyntax;
    }
    
    private StatementSyntax GenerateGuardStatement(GuardStatement guard)
    {
        // Generate: if (!(condition)) { else_block }
        var negatedCondition = PrefixUnaryExpression(
            SyntaxKind.LogicalNotExpression, 
            ParenthesizedExpression(GenerateExpression(guard.Condition))
        );
        
        var elseBlock = guard.ElseBody?.Any() == true
            ? Block(guard.ElseBody.Select(GenerateStatementSyntax).Where(s => s != null).Cast<StatementSyntax>())
            : Block(); // Empty block if no else body
            
        return IfStatement(negatedCondition, elseBlock);
    }
    
    private ExpressionSyntax GenerateExpression(ASTNode expression)
    {
        return GenerateExpression(expression, null);
    }
    
    private ExpressionSyntax GenerateExpression(ASTNode expression, string? expectedType)
    {
        
        return expression switch
        {
            BinaryExpression binary => GenerateBinaryExpression(binary),
            CallExpression call => GenerateCallExpression(call),
            MethodCallExpression methodCall => GenerateMethodCallExpression(methodCall),
            Identifier id => IdentifierName(id.Name),
            NumberLiteral num => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(num.Value)),
            StringLiteral str => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str.Value)),
            BooleanLiteral boolean => LiteralExpression(boolean.Value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
            ResultExpression result => GenerateResultExpression(result, expectedType),
            ErrorPropagation error => GenerateErrorPropagation(error),
            MemberAccessExpression member => GenerateMemberAccess(member),
            StringInterpolation interpolation => GenerateStringInterpolation(interpolation),
            TernaryExpression ternary => GenerateTernaryExpression(ternary),
            LogicalExpression logical => GenerateLogicalExpression(logical),
            ComparisonExpression comparison => GenerateComparisonExpression(comparison),
            ArithmeticExpression arithmetic => GenerateArithmeticExpression(arithmetic),
            UnaryExpression unary => GenerateUnaryExpression(unary),
            ListExpression list => GenerateListExpression(list),
            ListAccessExpression listAccess => GenerateListAccessExpression(listAccess),
            OptionExpression option => GenerateOptionExpression(option),
            MatchExpression match => GenerateMatchExpression(match),
            _ => IdentifierName("null")
        };
    }
    
    private BinaryExpressionSyntax GenerateBinaryExpression(BinaryExpression binary)
    {
        var left = GenerateExpression(binary.Left);
        var right = GenerateExpression(binary.Right);
        
        // Add parentheses around arithmetic expressions when used in comparison or logical operations
        if (IsComparisonOrLogicalOperator(binary.Operator))
        {
            if (binary.Left is BinaryExpression leftBinary && (IsArithmeticOperator(leftBinary.Operator) || ContainsArithmetic(leftBinary)))
            {
                left = ParenthesizedExpression(left);
            }
            if (binary.Right is BinaryExpression rightBinary && (IsArithmeticOperator(rightBinary.Operator) || ContainsArithmetic(rightBinary)))
            {
                right = ParenthesizedExpression(right);
            }
        }
        
        var kind = binary.Operator switch
        {
            "+" => SyntaxKind.AddExpression,
            "-" => SyntaxKind.SubtractExpression,
            "*" => SyntaxKind.MultiplyExpression,
            "/" => SyntaxKind.DivideExpression,
            "==" => SyntaxKind.EqualsExpression,
            "!=" => SyntaxKind.NotEqualsExpression,
            "<" => SyntaxKind.LessThanExpression,
            ">" => SyntaxKind.GreaterThanExpression,
            "<=" => SyntaxKind.LessThanOrEqualExpression,
            ">=" => SyntaxKind.GreaterThanOrEqualExpression,
            "&&" => SyntaxKind.LogicalAndExpression,
            "||" => SyntaxKind.LogicalOrExpression,
            _ => SyntaxKind.AddExpression
        };
        
        return BinaryExpression(kind, left, right);
    }
    
    private static bool IsArithmeticOperator(string op)
    {
        return op is "+" or "-" or "*" or "/";
    }
    
    private static bool IsComparisonOrLogicalOperator(string op)
    {
        return op is ">" or "<" or ">=" or "<=" or "==" or "!=" or "&&" or "||";
    }
    
    private static bool ContainsArithmetic(BinaryExpression expr)
    {
        if (IsArithmeticOperator(expr.Operator))
            return true;
            
        if (expr.Left is BinaryExpression leftBinary && ContainsArithmetic(leftBinary))
            return true;
            
        if (expr.Right is BinaryExpression rightBinary && ContainsArithmetic(rightBinary))
            return true;
            
        return false;
    }
    
    private static bool IsArithmeticExpression(ASTNode expr)
    {
        return expr switch
        {
            ArithmeticExpression => true,
            BinaryExpression binary => IsArithmeticOperator(binary.Operator) || ContainsArithmetic(binary),
            _ => false
        };
    }

    private InvocationExpressionSyntax GenerateMethodCallExpression(MethodCallExpression methodCall)
    {
        ExpressionSyntax expression;
        
        // Check if this is a module-qualified call (like Math.add)
        if (methodCall.Object is Identifier identifier && _moduleNamespaces.ContainsKey(identifier.Name))
        {
            
            // Generate fully qualified call: Cadenza.Modules.Math.Math.add
            var moduleName = identifier.Name;
            expression = IdentifierName("Cadenza");
            expression = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                IdentifierName("Modules")
            );
            expression = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                IdentifierName(moduleName)
            );
            expression = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                IdentifierName(moduleName) // Class name same as module name
            );
            expression = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                IdentifierName(methodCall.Method)
            );
        }
        else
        {
            // Generate the object expression first
            var objectExpression = GenerateExpression(methodCall.Object);
            
            // Create member access expression: object.method
            expression = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                objectExpression,
                IdentifierName(methodCall.Method)
            );
        }
        
        // Generate arguments
        var args = methodCall.Arguments.Select(arg => Argument(GenerateExpression(arg))).ToArray();
        
        return InvocationExpression(expression)
            .AddArgumentListArguments(args);
    }
    
    private InvocationExpressionSyntax GenerateCallExpression(CallExpression call)
    {
        ExpressionSyntax expression;
        
        // Handle member access calls like Console.WriteLine and module-qualified calls like Math.add
        if (call.Name.Contains('.'))
        {
            var parts = call.Name.Split('.');
            var moduleName = parts[0];
            var functionName = parts[1];
            
            // Check if this is a module-qualified call (like Math.add)
            if (_moduleNamespaces.ContainsKey(moduleName))
            {
                // Generate fully qualified call: Cadenza.Modules.Math.Math.add
                var moduleNamespace = _moduleNamespaces[moduleName];
                expression = IdentifierName("Cadenza");
                expression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expression,
                    IdentifierName("Modules")
                );
                expression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expression,
                    IdentifierName(moduleName)
                );
                expression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expression,
                    IdentifierName(moduleName) // Class name same as module name
                );
                expression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expression,
                    IdentifierName(functionName)
                );
            }
            else
            {
                // Regular member access like Console.WriteLine
                expression = IdentifierName(parts[0]);
                for (int i = 1; i < parts.Length; i++)
                {
                    expression = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        expression,
                        IdentifierName(parts[i])
                    );
                }
            }
        }
        else
        {
            // Check if this is an imported symbol that needs qualified name
            if (_importedSymbols.ContainsKey(call.Name))
            {
                // Generate qualified call: Cadenza.Modules.Math.Math.add
                var qualifiedName = _importedSymbols[call.Name];
                var parts = qualifiedName.Split('.');
                expression = IdentifierName(parts[0]);
                for (int i = 1; i < parts.Length; i++)
                {
                    expression = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        expression,
                        IdentifierName(parts[i])
                    );
                }
            }
            else
            {
                // Regular function call - use simple name
                expression = IdentifierName(call.Name);
            }
        }
        
        var args = call.Arguments.Select(arg => Argument(GenerateExpression(arg))).ToArray();
        
        return InvocationExpression(expression)
            .AddArgumentListArguments(args);
    }
    
    private InvocationExpressionSyntax GenerateResultExpression(ResultExpression result, string? expectedType = null)
    {
        var methodName = result.Type == "Ok" ? "Ok" : "Error";
        
        // Use expectedType if provided, otherwise fall back to function return type
        var typeToUse = expectedType ?? _currentFunctionReturnType;
        var (successType, errorType) = ParseResultType(typeToUse);

        var methodAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("Result"),
            GenericName(methodName)
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SeparatedList<TypeSyntax>(new[]
                        {
                            ParseTypeName(successType),
                            ParseTypeName(errorType)
                        })
                    )
                )
        );

        // For nested Results, pass the success type as the expected type for the inner expression
        ExpressionSyntax valueExpression;
        if (result.Value is ResultExpression && successType != "object")
        {
            // If the value is a Result and we have a specific success type, use that as expected type
            valueExpression = GenerateExpression(result.Value, successType);
        }
        else
        {
            valueExpression = GenerateExpression(result.Value);
        }

        return InvocationExpression(methodAccess)
            .AddArgumentListArguments(Argument(valueExpression));
    }

    /// <summary>
    /// Parse Result<T, E> type to extract T and E types
    /// </summary>
    private (string successType, string errorType) ParseResultType(string? returnType)
    {
        if (string.IsNullOrEmpty(returnType))
        {
            return ("object", "string"); // Default fallback
        }

        // Handle Result<T, E> pattern
        if (returnType.StartsWith("Result<") && returnType.EndsWith(">"))
        {
            var genericPart = returnType.Substring(7, returnType.Length - 8); // Remove "Result<" and ">"
            
            int bracketCount = 0;
            int commaIndex = -1;
            for(int i = 0; i < genericPart.Length; i++)
            {
                if(genericPart[i] == '<') bracketCount++;
                if(genericPart[i] == '>') bracketCount--;
                if(genericPart[i] == ',' && bracketCount == 0)
                {
                    commaIndex = i;
                    break;
                }
            }

            if (commaIndex != -1)
            {
                var successType = genericPart.Substring(0, commaIndex).Trim();
                var errorType = genericPart.Substring(commaIndex + 1).Trim();
                
                // Map Cadenza types to C# types
                successType = MapCadenzaTypeToCSharp(successType);
                errorType = MapCadenzaTypeToCSharp(errorType);
                
                return (successType, errorType);
            }
        }
        
        // Default fallback if parsing fails
        return ("object", "string");
    }
    
    private ExpressionSyntax GenerateErrorPropagation(ErrorPropagation error)
    {
        // Generate the expression that returns a Result
        var resultExpr = GenerateExpression(error.Expression);
        
        // For error propagation, we need to:
        // 1. Check if the result is an error
        // 2. If error, return it
        // 3. If success, extract the value
        
        // Generate: !resultExpr.IsSuccess ? throw new InvalidOperationException(resultExpr.Error) : resultExpr.Value
        // But we need to handle the early return case properly
        
        // For now, create a conditional expression that extracts the value
        // In a full implementation, this would generate proper statement-level handling
        return ConditionalExpression(
            PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    resultExpr,
                    IdentifierName("IsSuccess")
                )
            ),
            // If error, we should return (but can't in expression context)
            // So we'll throw for now - this needs proper statement-level handling
            ThrowExpression(
                ObjectCreationExpression(
                    IdentifierName("InvalidOperationException")
                ).WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    resultExpr,
                                    IdentifierName("Error")
                                )
                            )
                        )
                    )
                )
            ),
            // If success, extract the value
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                resultExpr,
                IdentifierName("Value")
            )
        );
    }
    
    private MemberAccessExpressionSyntax GenerateMemberAccess(MemberAccessExpression member)
    {
        // Handle Cadenza to C# property mapping
        var memberName = member.Member;
        if (memberName == "length")
        {
            memberName = "Count"; // List.length -> List.Count
        }
        
        return MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            GenerateExpression(member.Object),
            IdentifierName(memberName)
        );
    }
    
    private ExpressionSyntax GenerateStringInterpolation(StringInterpolation interpolation)
    {
        // Generate proper C# string interpolation
        var interpolationParts = new List<InterpolatedStringContentSyntax>();
        
        foreach (var part in interpolation.Parts)
        {
            if (part is StringLiteral str)
            {
                if (!string.IsNullOrEmpty(str.Value))
                {
                    interpolationParts.Add(InterpolatedStringText(Token(TriviaList(), SyntaxKind.InterpolatedStringTextToken, str.Value, str.Value, TriviaList())));
                }
            }
            else
            {
                var expr = GenerateExpression(part);
                interpolationParts.Add(Interpolation(expr));
            }
        }
        
        return InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
            .WithContents(List(interpolationParts));
    }
    
    private List<SyntaxTrivia> GenerateXmlDocumentation(FunctionDeclaration func)
    {
        var trivia = new List<SyntaxTrivia>();
        bool summaryGenerated = false;

        if (func.Specification != null)
        {
            summaryGenerated = true;
            // Generate rich XML documentation from specification block
            trivia.Add(Comment("/// <summary>"));
            trivia.Add(EndOfLine("\n"));
            
            // Add intent
            trivia.Add(Comment($"/// {func.Specification.Intent}"));
            trivia.Add(EndOfLine("\n"));
            
            // Add business rules if present
            if (func.Specification.Rules?.Any() == true)
            {
                trivia.Add(Comment("/// "));
                trivia.Add(EndOfLine("\n"));
                trivia.Add(Comment("/// Business Rules:"));
                trivia.Add(EndOfLine("\n"));
                foreach (var rule in func.Specification.Rules)
                {
                    trivia.Add(Comment($"/// - {rule}"));
                    trivia.Add(EndOfLine("\n"));
                }
            }
            
            // Add postconditions if present
            if (func.Specification.Postconditions?.Any() == true)
            {
                trivia.Add(Comment("/// "));
                trivia.Add(EndOfLine("\n"));
                trivia.Add(Comment("/// Expected Outcomes:"));
                trivia.Add(EndOfLine("\n"));
                foreach (var postcondition in func.Specification.Postconditions)
                {
                    trivia.Add(Comment($"/// - {postcondition}"));
                    trivia.Add(EndOfLine("\n"));
                }
            }
            
            // Add source document reference if present
            if (!string.IsNullOrEmpty(func.Specification.SourceDoc))
            {
                trivia.Add(Comment("/// "));
                trivia.Add(EndOfLine("\n"));
                trivia.Add(Comment($"/// Source: {func.Specification.SourceDoc}"));
                trivia.Add(EndOfLine("\n"));
            }
            
            trivia.Add(Comment("/// </summary>"));
            trivia.Add(EndOfLine("\n"));
        }
        else if (func.Effects?.Any() == true)
        {
            summaryGenerated = true;
            // Fallback to basic effects documentation
            trivia.Add(Comment("/// <summary>"));
            trivia.Add(EndOfLine("\n"));
            
            if (func.IsPure)
            {
                trivia.Add(Comment("/// Pure function - no side effects"));
            }
            else
            {
                trivia.Add(Comment($"/// Effects: {string.Join(", ", func.Effects)}"));
            }
            
            trivia.Add(EndOfLine("\n"));
            trivia.Add(Comment("/// </summary>"));
            trivia.Add(EndOfLine("\n"));
        }
        else if (func.IsPure)
        {
            summaryGenerated = true;
            // Pure functions without specifications get basic pure function documentation
            trivia.Add(Comment("/// <summary>"));
            trivia.Add(EndOfLine("\n"));
            trivia.Add(Comment("/// Pure function - no side effects"));
            trivia.Add(EndOfLine("\n"));
            trivia.Add(Comment("/// </summary>"));
            trivia.Add(EndOfLine("\n"));
        }

        if (!summaryGenerated)
        {
            trivia.Add(Comment("/// <summary>"));
            trivia.Add(EndOfLine("\n"));
            trivia.Add(Comment("/// "));
            trivia.Add(EndOfLine("\n"));
            trivia.Add(Comment("/// </summary>"));
            trivia.Add(EndOfLine("\n"));
        }
        
        // Add parameter documentation
        foreach (var param in func.Parameters)
        {
            trivia.Add(Comment($"/// <param name=\"{param.Name}\">Parameter of type {MapCadenzaTypeToCSharp(param.Type)}</param>"));
            trivia.Add(EndOfLine("\n"));
        }
        
        // Add return documentation
        if (!string.IsNullOrEmpty(func.ReturnType))
        {
            trivia.Add(Comment($"/// <returns>Returns {MapCadenzaTypeToCSharp(func.ReturnType)}</returns>"));
            trivia.Add(EndOfLine("\n"));
        }
        
        return trivia;
    }

    private ConditionalExpressionSyntax GenerateTernaryExpression(TernaryExpression ternary)
    {
        return ConditionalExpression(
            GenerateExpression(ternary.Condition),
            GenerateExpression(ternary.ThenExpr),
            GenerateExpression(ternary.ElseExpr)
        );
    }
    
    private BinaryExpressionSyntax GenerateLogicalExpression(LogicalExpression logical)
    {
        var left = GenerateExpression(logical.Left);
        var right = GenerateExpression(logical.Right);
        
        // Add parentheses around arithmetic expressions when used in logical operations
        if (IsArithmeticExpression(logical.Left))
        {
            left = ParenthesizedExpression(left);
        }
        if (IsArithmeticExpression(logical.Right))
        {
            right = ParenthesizedExpression(right);
        }
        
        var kind = logical.Operator switch
        {
            "&&" => SyntaxKind.LogicalAndExpression,
            "||" => SyntaxKind.LogicalOrExpression,
            _ => SyntaxKind.LogicalAndExpression
        };
        
        return BinaryExpression(kind, left, right);
    }
    
    private BinaryExpressionSyntax GenerateComparisonExpression(ComparisonExpression comparison)
    {
        var left = GenerateExpression(comparison.Left);
        var right = GenerateExpression(comparison.Right);
        
        // Add parentheses around arithmetic expressions when used in comparison operations
        if (IsArithmeticExpression(comparison.Left))
        {
            left = ParenthesizedExpression(left);
        }
        if (IsArithmeticExpression(comparison.Right))
        {
            right = ParenthesizedExpression(right);
        }
        
        var kind = comparison.Operator switch
        {
            "==" => SyntaxKind.EqualsExpression,
            "!=" => SyntaxKind.NotEqualsExpression,
            "<" => SyntaxKind.LessThanExpression,
            ">" => SyntaxKind.GreaterThanExpression,
            "<=" => SyntaxKind.LessThanOrEqualExpression,
            ">=" => SyntaxKind.GreaterThanOrEqualExpression,
            _ => SyntaxKind.EqualsExpression
        };
        
        return BinaryExpression(kind, left, right);
    }
    
    private BinaryExpressionSyntax GenerateArithmeticExpression(ArithmeticExpression arithmetic)
    {
        var kind = arithmetic.Operator switch
        {
            "+" => SyntaxKind.AddExpression,
            "-" => SyntaxKind.SubtractExpression,
            "*" => SyntaxKind.MultiplyExpression,
            "/" => SyntaxKind.DivideExpression,
            "%" => SyntaxKind.ModuloExpression,
            _ => SyntaxKind.AddExpression
        };
        
        return BinaryExpression(
            kind,
            GenerateExpression(arithmetic.Left),
            GenerateExpression(arithmetic.Right)
        );
    }
    
    private PrefixUnaryExpressionSyntax GenerateUnaryExpression(UnaryExpression unary)
    {
        var kind = unary.Operator switch
        {
            "!" => SyntaxKind.LogicalNotExpression,
            "-" => SyntaxKind.UnaryMinusExpression,
            _ => SyntaxKind.LogicalNotExpression
        };
        
        return PrefixUnaryExpression(kind, GenerateExpression(unary.Operand));
    }
    
    private ExpressionSyntax GenerateListExpression(ListExpression list)
    {
        // Generate: new List<T> { element1, element2, ... }
        var elements = list.Elements.Select(GenerateExpression).ToArray();
        
        return ObjectCreationExpression(
            IdentifierName("List<int>") // For now, assume int lists
        ).WithInitializer(
            InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                SeparatedList<ExpressionSyntax>(elements)
            )
        );
    }
    
    private ExpressionSyntax GenerateListAccessExpression(ListAccessExpression listAccess)
    {
        // Generate: list[index]
        return ElementAccessExpression(
            GenerateExpression(listAccess.List)
        ).WithArgumentList(
            BracketedArgumentList(
                SingletonSeparatedList(
                    Argument(GenerateExpression(listAccess.Index))
                )
            )
        );
    }
    
    private ExpressionSyntax GenerateOptionExpression(OptionExpression option)
    {
        if (option.Type == "Some" && option.Value != null)
        {
            // Generate: Option<T>.Some(value)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Option<object>"), // For now, use object type
                    IdentifierName("Some")
                )
            ).AddArgumentListArguments(Argument(GenerateExpression(option.Value)));
        }
        else
        {
            // Generate: Option<T>.None<T>()
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Option<object>"),
                    IdentifierName("None")
                )
            );
        }
    }
    
    private ExpressionSyntax GenerateMatchExpression(MatchExpression match)
    {
        // Generate proper match expression with variable binding
        var valueExpr = GenerateExpression(match.Value);
        
        if (match.Cases.Count == 2)
        {
            var okCase = match.Cases.FirstOrDefault(c => c.Pattern == "Ok");
            var errorCase = match.Cases.FirstOrDefault(c => c.Pattern == "Error");
            
            if (okCase != null && errorCase != null)
            {
                // Generate: result.IsSuccess ? (var okVar = result.Value; okExpression) : (var errorVar = result.Error; errorExpression)
                var condition = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    valueExpr,
                    IdentifierName("IsSuccess")
                );
                
                // Generate the then expression with variable binding
                ExpressionSyntax thenExpr;
                if (okCase.Variable != null && okCase.Body.Count > 0)
                {
                    // Create a lambda that binds the variable and executes the body
                    var valueAccess = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        valueExpr,
                        IdentifierName("Value")
                    );
                    
                    // For now, just substitute the variable name directly in the body
                    thenExpr = GenerateExpressionWithVariableSubstitution(okCase.Body[0], okCase.Variable, valueAccess);
                }
                else
                {
                    thenExpr = okCase.Body.Count > 0 ? GenerateExpression(okCase.Body[0]) : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("ok"));
                }
                
                // Generate the else expression with variable binding
                ExpressionSyntax elseExpr;
                if (errorCase.Variable != null && errorCase.Body.Count > 0)
                {
                    // Create a lambda that binds the variable and executes the body
                    var errorAccess = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        valueExpr,
                        IdentifierName("Error")
                    );
                    
                    // For now, just substitute the variable name directly in the body
                    elseExpr = GenerateExpressionWithVariableSubstitution(errorCase.Body[0], errorCase.Variable, errorAccess);
                }
                else
                {
                    elseExpr = errorCase.Body.Count > 0 ? GenerateExpression(errorCase.Body[0]) : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("error"));
                }
                
                return ConditionalExpression(condition, thenExpr, elseExpr);
            }
        }
        
        // Fallback: just return the first case body
        if (match.Cases.Count > 0 && match.Cases[0].Body.Count > 0)
        {
            return GenerateExpression(match.Cases[0].Body[0]);
        }
        
        return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("match"));
    }
    
    private ExpressionSyntax GenerateExpressionWithVariableSubstitution(ASTNode expression, string variableName, ExpressionSyntax valueExpression)
    {
        // Simple variable substitution - replace identifiers matching the variable name
        return expression switch
        {
            Identifier id when id.Name == variableName => valueExpression,
            BinaryExpression binary => BinaryExpression(
                binary.Operator switch
                {
                    "+" => SyntaxKind.AddExpression,
                    "-" => SyntaxKind.SubtractExpression,
                    "*" => SyntaxKind.MultiplyExpression,
                    "/" => SyntaxKind.DivideExpression,
                    "==" => SyntaxKind.EqualsExpression,
                    "!=" => SyntaxKind.NotEqualsExpression,
                    "<" => SyntaxKind.LessThanExpression,
                    ">" => SyntaxKind.GreaterThanExpression,
                    "<=" => SyntaxKind.LessThanOrEqualExpression,
                    ">=" => SyntaxKind.GreaterThanOrEqualExpression,
                    _ => SyntaxKind.AddExpression
                },
                GenerateExpressionWithVariableSubstitution(binary.Left, variableName, valueExpression),
                GenerateExpressionWithVariableSubstitution(binary.Right, variableName, valueExpression)
            ),
            MemberAccessExpression member => MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                GenerateExpressionWithVariableSubstitution(member.Object, variableName, valueExpression),
                IdentifierName(member.Member == "length" ? "Count" : member.Member)
            ),
            CallExpression call => InvocationExpression(
                call.Name.Contains('.') ? 
                    ParseExpression(call.Name) : 
                    IdentifierName(call.Name)
            ).AddArgumentListArguments(
                call.Arguments.Select(arg => Argument(GenerateExpressionWithVariableSubstitution(arg, variableName, valueExpression))).ToArray()
            ),
            _ => GenerateExpression(expression)
        };
    }
    
    private void ProcessImportStatement(ImportStatement import)
    {
        // Handle specific imports like: import Math.{add, multiply}
        if (import.SpecificImports != null)
        {
            var moduleNamespace = $"Cadenza.Modules.{import.ModuleName}.{import.ModuleName}";
            
            foreach (var symbol in import.SpecificImports)
            {
                // Map imported symbol to fully qualified C# name
                _importedSymbols[symbol] = $"{moduleNamespace}.{symbol}";
            }
        }
        // Handle wildcard imports like: import Math.*
        else if (import.IsWildcard)
        {
            // Add the module namespace to using statements so all symbols are available
            var moduleNamespace = $"Cadenza.Modules.{import.ModuleName}";
            if (!_usingStatements.Contains(moduleNamespace))
            {
                _usingStatements.Add(moduleNamespace);
            }
        }
    }
}

// =============================================================================
// COMPILATION RECORDS
// =============================================================================

/// <summary>
/// Result of a direct compilation operation
/// </summary>
public record CompilationResult(
    bool Success,
    IEnumerable<Diagnostic> Diagnostics,
    string? AssemblyPath = null,
    TimeSpan? CompilationTime = null
);

/// <summary>
/// Options for direct compilation
/// </summary>
public record CompilationOptions(
    string SourceFile,
    string OutputPath,
    OutputKind OutputKind = OutputKind.ConsoleApplication,
    bool OptimizeCode = true,
    bool IncludeDebugSymbols = false,
    string[]? AdditionalReferences = null
);

// =============================================================================
// DIRECT COMPILER
// =============================================================================

/// <summary>
/// Direct compiler that generates assemblies from Cadenza source using Roslyn
/// </summary>
public class DirectCompiler
{
    private readonly CadenzaTranspiler _transpiler;
    private readonly CompilationCache _cache;

    public DirectCompiler()
    {
        _transpiler = new CadenzaTranspiler();
        _cache = new CompilationCache();
    }

    /// <summary>
    /// Compiles Cadenza source directly to an assembly
    /// </summary>
    public async Task<CompilationResult> CompileToAssemblyAsync(CompilationOptions options)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Step 1: Parse Cadenza source to AST
            var source = await File.ReadAllTextAsync(options.SourceFile);
            var lexer = new CadenzaLexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();

            // Step 2: Generate C# syntax tree
            var generator = new CSharpGenerator();
            var syntaxTree = generator.GenerateFromAST(ast);

            // Step 3: Create compilation with references
            var compilation = CreateCompilation(syntaxTree, options);

            // Step 4: Emit assembly
            var emitResult = compilation.Emit(options.OutputPath);

            var compilationTime = DateTime.UtcNow - startTime;

            return new CompilationResult(
                Success: emitResult.Success,
                Diagnostics: emitResult.Diagnostics,
                AssemblyPath: emitResult.Success ? options.OutputPath : null,
                CompilationTime: compilationTime
            );
        }
        catch (Exception ex)
        {
            var compilationTime = DateTime.UtcNow - startTime;
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FL0001",
                    "Compilation Error",
                    ex.Message,
                    "Compiler",
                    DiagnosticSeverity.Error,
                    true
                ),
                Location.None
            );

            return new CompilationResult(
                Success: false,
                Diagnostics: new[] { diagnostic },
                CompilationTime: compilationTime
            );
        }
    }

    /// <summary>
    /// Compiles and immediately executes the Cadenza program
    /// </summary>
    public async Task<(CompilationResult CompilationResult, int? ExitCode)> CompileAndRunAsync(CompilationOptions options)
    {
        var compilationResult = await CompileToAssemblyAsync(options);
        
        if (!compilationResult.Success || compilationResult.AssemblyPath == null)
        {
            return (compilationResult, null);
        }

        try
        {
            // Load and execute the assembly
            var assembly = Assembly.LoadFrom(compilationResult.AssemblyPath);
            var entryPoint = assembly.EntryPoint;

            if (entryPoint == null)
            {
                return (compilationResult, null);
            }

            var result = entryPoint.Invoke(null, new object[] { Array.Empty<string>() });
            var exitCode = result is int code ? code : 0;

            return (compilationResult, exitCode);
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FL0002",
                    "Execution Error",
                    ex.Message,
                    "Runtime",
                    DiagnosticSeverity.Error,
                    true
                ),
                Location.None
            );

            var updatedResult = compilationResult with
            {
                Success = false,
                Diagnostics = compilationResult.Diagnostics.Append(diagnostic)
            };

            return (updatedResult, null);
        }
    }

    /// <summary>
    /// Creates a CSharpCompilation with proper references and options
    /// </summary>
    private CSharpCompilation CreateCompilation(SyntaxTree syntaxTree, CompilationOptions options)
    {
        var references = GetDefaultReferences();
        
        if (options.AdditionalReferences != null)
        {
            var additionalRefs = options.AdditionalReferences
                .Select(path => MetadataReference.CreateFromFile(path));
            references = references.Concat(additionalRefs).ToArray();
        }

        var compilationOptions = new CSharpCompilationOptions(
            outputKind: options.OutputKind,
            optimizationLevel: options.OptimizeCode ? OptimizationLevel.Release : OptimizationLevel.Debug,
            allowUnsafe: false,
            platform: Platform.AnyCpu
        );

        var assemblyName = Path.GetFileNameWithoutExtension(options.OutputPath);
        
        return CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: compilationOptions
        );
    }

    /// <summary>
    /// Gets the default .NET references needed for Cadenza programs
    /// </summary>
    private MetadataReference[] GetDefaultReferences()
    {
        var references = new List<MetadataReference>
        {
            // Core .NET references
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location),
            
            // System.Runtime reference (needed for modern .NET)
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            
            // System.Collections reference
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            
            // System.Text reference (for string operations)
            MetadataReference.CreateFromFile(Assembly.Load("System.Text.RegularExpressions").Location)
        };
        
        // Add Cadenza.Runtime reference if it exists in the same assembly
        try
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            references.Add(MetadataReference.CreateFromFile(currentAssembly.Location));
        }
        catch (Exception)
        {
            // If we can't load the runtime, continue without it
        }
        
        return references.ToArray();
    }
}

// =============================================================================
// COMPILATION CACHE
// =============================================================================

/// <summary>
/// Caches compilation objects for performance optimization
/// </summary>
public class CompilationCache
{
    private readonly Dictionary<string, CSharpCompilation> _compilationCache = new();
    private readonly Dictionary<string, DateTime> _lastModified = new();

    public bool TryGetCachedCompilation(string sourceFile, out CSharpCompilation? compilation)
    {
        compilation = null;

        if (!_compilationCache.ContainsKey(sourceFile))
            return false;

        var fileInfo = new FileInfo(sourceFile);
        if (!fileInfo.Exists)
            return false;

        if (_lastModified.ContainsKey(sourceFile) && 
            _lastModified[sourceFile] >= fileInfo.LastWriteTime)
        {
            compilation = _compilationCache[sourceFile];
            return true;
        }

        // File has been modified, remove from cache
        _compilationCache.Remove(sourceFile);
        _lastModified.Remove(sourceFile);
        return false;
    }

    public void CacheCompilation(string sourceFile, CSharpCompilation compilation)
    {
        var fileInfo = new FileInfo(sourceFile);
        if (!fileInfo.Exists) return;

        _compilationCache[sourceFile] = compilation;
        _lastModified[sourceFile] = fileInfo.LastWriteTime;
    }

    public void ClearCache()
    {
        _compilationCache.Clear();
        _lastModified.Clear();
    }
}

// =============================================================================
// DIRECT COMPILER CLI
// =============================================================================

/// <summary>
/// Enhanced CLI program with direct compilation support
/// </summary>
public class DirectCompilerCLI
{
    private readonly DirectCompiler _compiler;

    public DirectCompilerCLI()
    {
        _compiler = new DirectCompiler();
    }

    /// <summary>
    /// Enhanced main method with direct compilation support
    /// </summary>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = ParseArguments(args);
            
            if (options == null)
            {
                ShowHelp();
                return 1;
            }

            if (options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            if (options.ShowVersion)
            {
                Console.WriteLine("Cadenza Direct Compiler v1.0.0");
                return 0;
            }

            if (options.CompileMode)
            {
                return await HandleCompileMode(options);
            }
            else
            {
                return await HandleTranspileMode(options);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> HandleCompileMode(CLIOptions options)
    {
        var compilationOptions = new CompilationOptions(
            SourceFile: options.InputFile!,
            OutputPath: options.OutputFile ?? GetDefaultOutputPath(options.InputFile!, options.Library),
            OutputKind: options.Library ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication,
            OptimizeCode: !options.Debug,
            IncludeDebugSymbols: options.Debug
        );

        if (options.Run)
        {
            var (result, exitCode) = await _compiler.CompileAndRunAsync(compilationOptions);
            
            if (!result.Success)
            {
                DisplayCompilationErrors(result.Diagnostics);
                return 1;
            }

            Console.WriteLine($"Compilation successful: {compilationOptions.OutputPath}");
            Console.WriteLine($"Compilation time: {result.CompilationTime?.TotalMilliseconds:F2}ms");
            
            if (exitCode.HasValue)
            {
                Console.WriteLine($"Program exited with code: {exitCode.Value}");
                return exitCode.Value;
            }
            
            return 0;
        }
        else
        {
            var result = await _compiler.CompileToAssemblyAsync(compilationOptions);
            
            if (!result.Success)
            {
                DisplayCompilationErrors(result.Diagnostics);
                return 1;
            }

            Console.WriteLine($"Compilation successful: {compilationOptions.OutputPath}");
            Console.WriteLine($"Compilation time: {result.CompilationTime?.TotalMilliseconds:F2}ms");
            return 0;
        }
    }

    private async Task<int> HandleTranspileMode(CLIOptions options)
    {
        // Use existing transpiler for backward compatibility
        var transpiler = new CadenzaTranspiler();
        
        switch (options.Target?.ToLowerInvariant())
        {
            case "csharp":
            case "cs":
            case null:
                await transpiler.TranspileAsync(options.InputFile!, options.OutputFile);
                Console.WriteLine($"Successfully transpiled {options.InputFile} -> {options.OutputFile}");
                break;
            
            case "javascript":
            case "js":
                await transpiler.TranspileToJavaScriptAsync(options.InputFile!, options.OutputFile);
                Console.WriteLine($"Successfully transpiled {options.InputFile} -> {options.OutputFile} (JavaScript)");
                break;
            
            case "blazor":
            case "razor":
                await transpiler.TranspileToBlazorAsync(options.InputFile!, options.OutputFile);
                Console.WriteLine($"Successfully transpiled {options.InputFile} -> {options.OutputFile} (Blazor)");
                break;
            
            default:
                Console.Error.WriteLine($"Error: Unsupported target '{options.Target}'. Supported targets: csharp, javascript, blazor");
                return 1;
        }
        
        return 0;
    }

    private void DisplayCompilationErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
        
        foreach (var error in errors)
        {
            Console.Error.WriteLine($"Error: {error.GetMessage()}");
            
            if (error.Location != Location.None)
            {
                var lineSpan = error.Location.GetLineSpan();
                Console.Error.WriteLine($"  at line {lineSpan.StartLinePosition.Line + 1}, column {lineSpan.StartLinePosition.Character + 1}");
            }
        }
    }

    private string GetDefaultOutputPath(string inputFile, bool isLibrary)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
        var extension = isLibrary ? ".dll" : ".exe";
        return nameWithoutExtension + extension;
    }

    private CLIOptions? ParseArguments(string[] args)
    {
        if (args.Length == 0)
            return new CLIOptions { ShowHelp = true };

        var options = new CLIOptions();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    return options;
                
                case "--version":
                case "-v":
                    options.ShowVersion = true;
                    return options;
                
                case "--compile":
                case "-c":
                    options.CompileMode = true;
                    break;
                
                case "--run":
                case "-r":
                    options.Run = true;
                    options.CompileMode = true; // --run implies --compile
                    break;
                
                case "--library":
                case "-l":
                    options.Library = true;
                    break;
                
                case "--debug":
                case "-d":
                    options.Debug = true;
                    break;
                
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFile = args[++i];
                    }
                    break;
                
                case "--target":
                case "-t":
                    if (i + 1 < args.Length)
                    {
                        options.Target = args[++i];
                    }
                    break;
                
                default:
                    if (!args[i].StartsWith('-'))
                    {
                        if (options.InputFile == null)
                        {
                            options.InputFile = args[i];
                        }
                        else if (options.OutputFile == null && !options.CompileMode)
                        {
                            options.OutputFile = args[i];
                        }
                    }
                    break;
            }
        }

        // Validation
        if (options.InputFile == null && !options.ShowHelp && !options.ShowVersion)
        {
            Console.Error.WriteLine("Error: Input file required");
            return null;
        }

        // Set default output file for transpile mode
        if (!options.CompileMode && options.OutputFile == null && options.InputFile != null)
        {
            var extension = options.Target?.ToLowerInvariant() switch
            {
                "javascript" or "js" => ".js",
                "blazor" or "razor" => ".razor",
                _ => ".cs"
            };
            options.OutputFile = Path.ChangeExtension(options.InputFile, extension);
        }

        return options;
    }

    private void ShowHelp()
    {
        Console.WriteLine("Cadenza Direct Compiler - Transpilation and Direct Compilation");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Transpile (default):");
        Console.WriteLine("    cadenzac-core <input.cdz> [<output.cs>] [--target csharp|javascript|blazor]");
        Console.WriteLine();
        Console.WriteLine("  Direct compilation:");
        Console.WriteLine("    cadenzac-core --compile <input.cdz> [--output <output.exe>]");
        Console.WriteLine("    cadenzac-core --run <input.cdz>");
        Console.WriteLine("    cadenzac-core --library <input.cdz> [--output <output.dll>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --compile, -c   Compile directly to assembly (default: transpile)");
        Console.WriteLine("  --run, -r       Compile and run immediately");
        Console.WriteLine("  --library, -l   Generate library (.dll) instead of executable");
        Console.WriteLine("  --debug, -d     Include debug symbols and disable optimizations");
        Console.WriteLine("  --output, -o    Specify output file path");
        Console.WriteLine("  --target, -t    Target language for transpilation (csharp, javascript, blazor)");
        Console.WriteLine("  --help, -h      Show this help message");
        Console.WriteLine("  --version, -v   Show version information");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Transpile to C#");
        Console.WriteLine("  cadenzac-core hello.cdz hello.cs");
        Console.WriteLine();
        Console.WriteLine("  # Direct compilation");
        Console.WriteLine("  cadenzac-core --compile hello.cdz");
        Console.WriteLine("  cadenzac-core --compile hello.cdz --output hello.exe");
        Console.WriteLine();
        Console.WriteLine("  # Compile and run");
        Console.WriteLine("  cadenzac-core --run hello.cdz");
        Console.WriteLine();
        Console.WriteLine("  # Generate library");
        Console.WriteLine("  cadenzac-core --library math.cdz --output math.dll");
    }
}

// =============================================================================
// CLI OPTIONS
// =============================================================================

/// <summary>
/// CLI options parsed from command line arguments
/// </summary>
public class CLIOptions
{
    public string? InputFile { get; set; }
    public string? OutputFile { get; set; }
    public bool CompileMode { get; set; }
    public bool Run { get; set; }
    public bool Library { get; set; }
    public bool Debug { get; set; }
    public string? Target { get; set; }
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
}

// =============================================================================
// BLAZOR GENERATOR
// =============================================================================

public class BlazorGenerator
{
    public string GenerateBlazorComponent(ComponentDeclaration component)
    {
        var razor = new System.Text.StringBuilder();
        
        // Add page directive if this is a page component
        if (component.Parameters.Any())
        {
            razor.AppendLine($"@page \"/{component.Name.ToLowerInvariant()}\"");
        }
        
        // Add using statements
        razor.AppendLine("@using Microsoft.AspNetCore.Components");
        razor.AppendLine("@using Microsoft.AspNetCore.Components.Web");
        razor.AppendLine();
        
        // Generate render block (HTML markup)
        razor.AppendLine(GenerateRenderBlock(component.RenderBlock));
        razor.AppendLine();
        
        // Generate code block
        razor.AppendLine("@code {");
        
        // Add parameters
        foreach (var param in component.Parameters)
        {
            razor.AppendLine($"    [Parameter] public {MapCadenzaTypeToCSharp(param.Type)} {param.Name} {{ get; set; }} = default({MapCadenzaTypeToCSharp(param.Type)});");
        }
        
        if (component.Parameters.Any())
        {
            razor.AppendLine();
        }
        
        // Add state variables
        if (component.State?.Any() == true)
        {
            foreach (var state in component.State)
            {
                var defaultValue = state.InitialValue != null ? GenerateBlazorExpression(state.InitialValue) : GetDefaultValue(state.Type);
                razor.AppendLine($"    private {MapCadenzaTypeToCSharp(state.Type)} {state.Name} = {defaultValue};");
            }
            razor.AppendLine();
        }
        
        // Add OnInitialized method if component has OnMount
        if (component.OnMount != null)
        {
            razor.AppendLine("    protected override void OnInitialized()");
            razor.AppendLine("    {");
            razor.AppendLine("        base.OnInitialized();");
            razor.AppendLine(GenerateBlazorStatements(component.OnMount));
            razor.AppendLine("    }");
            razor.AppendLine();
        }
        
        // Add event handlers
        if (component.Events?.Any() == true)
        {
            foreach (var eventHandler in component.Events)
            {
                razor.AppendLine($"    private void {ToPascalCase(eventHandler.Name)}({GenerateParameterList(eventHandler.Parameters)})");
                razor.AppendLine("    {");
                
                foreach (var statement in eventHandler.Body)
                {
                    razor.AppendLine($"        {GenerateBlazorStatements(statement)}");
                }
                
                razor.AppendLine("    }");
                razor.AppendLine();
            }
        }
        
        razor.AppendLine("}");
        
        return razor.ToString();
    }
    
    private string GenerateRenderBlock(ASTNode renderBlock)
    {
        return renderBlock switch
        {
            UIElement element => GenerateUIElement(element),
            ComponentInstance component => GenerateComponentInstance(component),
            ConditionalRender conditional => GenerateConditionalRender(conditional),
            IterativeRender iterative => GenerateIterativeRender(iterative),
            _ => GenerateBlazorExpression(renderBlock)
        };
    }
    
    private string GenerateUIElement(UIElement element)
    {
        var html = new System.Text.StringBuilder();
        
        // Map Cadenza element names to HTML tags
        var htmlTag = MapCadenzaElementToHtml(element.Tag);
        
        html.Append($"<{htmlTag}");
        
        // Add attributes
        foreach (var attr in element.Attributes)
        {
            var attrName = MapCadenzaAttributeToHtml(attr.Name);
            var attrValue = GenerateBlazorExpression(attr.Value);
            
            if (attr.Name.StartsWith("on_"))
            {
                // Event handler
                var eventName = attr.Name.Replace("on_", "@on");
                html.Append($" {eventName}=\"{attrValue}\"");
            }
            else if (attr.Name == "text")
            {
                // Text content - handle specially
                html.Append($">{attrValue}</{htmlTag}>");
                return html.ToString();
            }
            else
            {
                html.Append($" {attrName}=\"{attrValue}\"");
            }
        }
        
        if (element.Children.Any())
        {
            html.Append(">");
            
            foreach (var child in element.Children)
            {
                html.Append(GenerateRenderBlock(child));
            }
            
            html.Append($"</{htmlTag}>");
        }
        else
        {
            html.Append(" />");
        }
        
        return html.ToString();
    }
    
    private string GenerateComponentInstance(ComponentInstance component)
    {
        var html = new System.Text.StringBuilder();
        
        html.Append($"<{component.Name}");
        
        // Add props
        foreach (var prop in component.Props)
        {
            var propValue = GenerateBlazorExpression(prop.Value);
            html.Append($" {prop.Name}=\"{propValue}\"");
        }
        
        if (component.Children?.Any() == true)
        {
            html.Append(">");
            
            foreach (var child in component.Children)
            {
                html.Append(GenerateRenderBlock(child));
            }
            
            html.Append($"</{component.Name}>");
        }
        else
        {
            html.Append(" />");
        }
        
        return html.ToString();
    }
    
    private string GenerateConditionalRender(ConditionalRender conditional)
    {
        var html = new System.Text.StringBuilder();
        
        html.AppendLine($"@if ({GenerateBlazorExpression(conditional.Condition)})");
        html.AppendLine("{");
        
        foreach (var item in conditional.ThenBody)
        {
            html.AppendLine($"    {GenerateRenderBlock(item)}");
        }
        
        html.AppendLine("}");
        
        if (conditional.ElseBody?.Any() == true)
        {
            html.AppendLine("else");
            html.AppendLine("{");
            
            foreach (var item in conditional.ElseBody)
            {
                html.AppendLine($"    {GenerateRenderBlock(item)}");
            }
            
            html.AppendLine("}");
        }
        
        return html.ToString();
    }
    
    private string GenerateIterativeRender(IterativeRender iterative)
    {
        var html = new System.Text.StringBuilder();
        
        var condition = iterative.Condition != null ? $" where {GenerateBlazorExpression(iterative.Condition)}" : "";
        html.AppendLine($"@foreach (var {iterative.Variable} in {GenerateBlazorExpression(iterative.Collection)}{condition})");
        html.AppendLine("{");
        
        foreach (var item in iterative.Body)
        {
            html.AppendLine($"    {GenerateRenderBlock(item)}");
        }
        
        html.AppendLine("}");
        
        return html.ToString();
    }
    
    private string GenerateBlazorExpression(ASTNode expression)
    {
        return expression switch
        {
            Identifier id => id.Name,
            NumberLiteral num => num.Value.ToString(),
            StringLiteral str => $"\"{str.Value}\"",
            BooleanLiteral boolLit => boolLit.Value.ToString().ToLower(),
            StringInterpolation interpolation => GenerateStringInterpolation(interpolation),
            BinaryExpression binary => $"{GenerateBlazorExpression(binary.Left)} {binary.Operator} {GenerateBlazorExpression(binary.Right)}",
            CallExpression call => $"{call.Name}({string.Join(", ", call.Arguments.Select(GenerateBlazorExpression))})",
            MethodCallExpression method => $"{GenerateBlazorExpression(method.Object)}.{method.Method}({string.Join(", ", method.Arguments.Select(GenerateBlazorExpression))})",
            _ => expression.ToString() ?? ""
        };
    }
    
    private string GenerateStringInterpolation(StringInterpolation interpolation)
    {
        var result = new System.Text.StringBuilder();
        result.Append("$\"");
        
        foreach (var part in interpolation.Parts)
        {
            if (part is StringLiteral str)
            {
                result.Append(str.Value);
            }
            else
            {
                result.Append("{");
                result.Append(GenerateBlazorExpression(part));
                result.Append("}");
            }
        }
        
        result.Append("\"");
        return result.ToString();
    }
    
    private string GenerateBlazorStatements(ASTNode statement)
    {
        return statement switch
        {
            CallExpression call when call.Name == "set_state" => GenerateSetStateCall(call),
            CallExpression call => $"{GenerateBlazorExpression(call)};",
            _ => $"{GenerateBlazorExpression(statement)};"
        };
    }
    
    private string GenerateSetStateCall(CallExpression call)
    {
        if (call.Arguments.Count >= 2)
        {
            var stateName = GenerateBlazorExpression(call.Arguments[0]);
            var stateValue = GenerateBlazorExpression(call.Arguments[1]);
            return $"{stateName} = {stateValue};";
        }
        return "";
    }
    
    private string MapCadenzaElementToHtml(string elementName)
    {
        return elementName switch
        {
            "container" => "div",
            "heading" => "h1",
            "button" => "button",
            "text_input" => "input",
            "select_dropdown" => "select",
            _ => elementName
        };
    }
    
    private string MapCadenzaAttributeToHtml(string attributeName)
    {
        return attributeName switch
        {
            "class" => "class",
            "text" => "text",
            "level" => "level",
            "disabled" => "disabled",
            _ => attributeName
        };
    }
    
    private string MapCadenzaTypeToCSharp(string flowLangType)
    {
        return flowLangType switch
        {
            "string" => "string",
            "int" => "int",
            "bool" => "bool",
            "float" => "float",
            "double" => "double",
            "Option<string>" => "string?",
            "Option<int>" => "int?",
            "Option<bool>" => "bool?",
            "List<string>" => "List<string>",
            "List<int>" => "List<int>",
            _ => flowLangType
        };
    }
    
    private string GetDefaultValue(string type)
    {
        return type switch
        {
            "string" => "\"\"",
            "int" => "0",
            "bool" => "false",
            "float" => "0.0f",
            "double" => "0.0",
            _ when type.StartsWith("Option<") => "null",
            _ when type.StartsWith("List<") => $"new {MapCadenzaTypeToCSharp(type)}()",
            _ => "default"
        };
    }
    
    private string GenerateParameterList(List<Parameter> parameters)
    {
        return string.Join(", ", parameters.Select(p => $"{MapCadenzaTypeToCSharp(p.Type)} {p.Name}"));
    }
    
    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}

// =============================================================================
// FLOW CORE PROGRAM
// =============================================================================

public class FlowCoreProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        var cli = new DirectCompilerCLI();
        return await cli.RunAsync(args);
    }
}