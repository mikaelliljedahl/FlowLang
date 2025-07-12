using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FlowLang.Compiler;

// C# Code Generator using Roslyn
public class CSharpGenerator
{
    public SyntaxTree GenerateFromAST(Program program)
    {
        var members = new List<MemberDeclarationSyntax>();
        
        foreach (var statement in program.Statements)
        {
            if (statement is FunctionDeclaration func)
            {
                members.Add(GenerateMethod(func));
            }
        }
        
        var compilationUnit = CompilationUnit()
            .WithMembers(List(members));
        
        return CSharpSyntaxTree.Create(compilationUnit);
    }

    private MethodDeclarationSyntax GenerateMethod(FunctionDeclaration func)
    {
        var parameters = func.Parameters.Select(p => 
            Parameter(Identifier(p.Name))
                .WithType(ParseTypeName(p.Type))
        ).ToArray();

        var body = func.Body.Select(GenerateStatement).ToArray();
        
        return MethodDeclaration(ParseTypeName(func.ReturnType), func.Name)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithBody(Block(body));
    }

    private StatementSyntax GenerateStatement(ASTNode node)
    {
        return node switch
        {
            ReturnStatement ret => ReturnStatement(GenerateExpression(ret.Expression)),
            _ => throw new NotImplementedException($"Statement type {node.GetType().Name} not implemented")
        };
    }

    private ExpressionSyntax GenerateExpression(ASTNode node)
    {
        return node switch
        {
            BinaryExpression bin => BinaryExpression(
                GetBinaryOperator(bin.Operator),
                GenerateExpression(bin.Left),
                GenerateExpression(bin.Right)
            ),
            Identifier id => IdentifierName(id.Name),
            NumberLiteral num => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(num.Value)),
            _ => throw new NotImplementedException($"Expression type {node.GetType().Name} not implemented")
        };
    }

    private SyntaxKind GetBinaryOperator(string op) => op switch
    {
        "+" => SyntaxKind.AddExpression,
        "-" => SyntaxKind.SubtractExpression,
        "*" => SyntaxKind.MultiplyExpression,
        "/" => SyntaxKind.DivideExpression,
        ">" => SyntaxKind.GreaterThanExpression,
        "<" => SyntaxKind.LessThanExpression,
        ">=" => SyntaxKind.GreaterThanOrEqualExpression,
        "<=" => SyntaxKind.LessThanOrEqualExpression,
        _ => throw new NotImplementedException($"Operator {op} not implemented")
    };

    private TypeSyntax ParseTypeName(string typeName) => typeName switch
    {
        "int" => PredefinedType(Token(SyntaxKind.IntKeyword)),
        "string" => PredefinedType(Token(SyntaxKind.StringKeyword)),
        "bool" => PredefinedType(Token(SyntaxKind.BoolKeyword)),
        _ => IdentifierName(typeName)
    };
}