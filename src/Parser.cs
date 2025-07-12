using System;
using System.Collections.Generic;

namespace FlowLang.Compiler;

// Basic FlowLang Parser
public class FlowLangParser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    public FlowLangParser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public Program Parse()
    {
        var statements = new List<ASTNode>();
        
        while (!IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }
        
        return new Program(statements);
    }

    private ASTNode? ParseStatement()
    {
        if (Match(TokenType.Function))
        {
            return ParseFunctionDeclaration();
        }
        
        return null;
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected function name").Value;
        
        Consume(TokenType.LeftParen, "Expected '(' after function name");
        var parameters = new List<Parameter>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Value;
                Consume(TokenType.Colon, "Expected ':' after parameter name");
                var paramType = ConsumeType("Expected parameter type").Value;
                parameters.Add(new Parameter(paramName, paramType));
            }
            while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        Consume(TokenType.Arrow, "Expected '->' after parameters");
        var returnType = ConsumeType("Expected return type").Value;
        
        Consume(TokenType.LeftBrace, "Expected '{' before function body");
        var body = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.Return))
            {
                var expr = ParseExpression();
                body.Add(new ReturnStatement(expr));
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after function body");
        
        return new FunctionDeclaration(name, parameters, returnType, body);
    }

    private ASTNode ParseExpression()
    {
        return ParseComparison();
    }

    private ASTNode ParseAddition()
    {
        var expr = ParseMultiplication();
        
        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var op = Previous().Value;
            var right = ParseMultiplication();
            expr = new BinaryExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseComparison()
    {
        var expr = ParseAddition();
        
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous().Value;
            var right = ParseAddition();
            expr = new BinaryExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseMultiplication()
    {
        var expr = ParsePrimary();
        
        while (Match(TokenType.Multiply, TokenType.Divide))
        {
            var op = Previous().Value;
            var right = ParsePrimary();
            expr = new BinaryExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParsePrimary()
    {
        if (Match(TokenType.Number))
        {
            return new NumberLiteral(int.Parse(Previous().Value));
        }
        
        if (Match(TokenType.Identifier))
        {
            return new Identifier(Previous().Value);
        }
        
        if (Match(TokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return expr;
        }
        
        throw new Exception($"Unexpected token: {Peek().Value}");
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;
    private Token Advance() => IsAtEnd() ? Previous() : _tokens[_current++];
    private bool IsAtEnd() => _current >= _tokens.Count || Peek().Type == TokenType.EOF;
    private Token Peek() => _tokens[_current];
    private Token Previous() => _tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new Exception($"{message}. Got: {Peek().Value}");
    }

    private Token ConsumeType(string message)
    {
        if (Check(TokenType.Int) || Check(TokenType.String_Type) || Check(TokenType.Bool) || Check(TokenType.Identifier))
        {
            return Advance();
        }
        throw new Exception($"{message}. Got: {Peek().Type} '{Peek().Value}'");
    }
}