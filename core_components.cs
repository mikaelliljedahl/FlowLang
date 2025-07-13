using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

// =============================================================================
// TOKEN DEFINITIONS
// =============================================================================

// Token types for FlowLang
public enum TokenType
{
    // Literals
    Identifier,
    Number,
    String,
    StringInterpolation,
    
    // Keywords
    Function,
    Return,
    If,
    Else,
    Effects,
    Pure,
    Uses,
    Result,
    Ok,
    Error,
    Let,
    
    // Effect names
    Database,
    Network,
    Logging,
    FileSystem,
    Memory,
    IO,
    
    // Types
    Int,
    String_Type,
    Bool,
    
    // Symbols
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Semicolon,
    Colon,
    Arrow,
    Assign,
    
    // Operators
    Plus,
    Minus,
    Multiply,
    Divide,
    Greater,
    Less,
    GreaterEqual,
    LessEqual,
    Equal,
    NotEqual,
    Question, // Error propagation operator ?
    
    // Logical operators
    And,      // &&
    Or,       // ||
    Not,      // !
    
    // Control flow keywords
    Guard,    // guard keyword
    
    // Module system keywords
    Module,   // module keyword
    Import,   // import keyword
    Export,   // export keyword
    From,     // from keyword
    Dot,      // . for qualified names and imports
    
    // Special
    EOF,
    Newline
}

public record Token(TokenType Type, string Value, int Line, int Column);

// =============================================================================
// AST DEFINITIONS
// =============================================================================

// AST Node definitions
public abstract record ASTNode;
public record Program(List<ASTNode> Statements) : ASTNode;

// Module system AST nodes
public record ModuleDeclaration(string Name, List<ASTNode> Body, List<string>? Exports = null) : ASTNode;
public record ImportStatement(string ModuleName, List<string>? SpecificImports = null, bool IsWildcard = false) : ASTNode;
public record ExportStatement(List<string> ExportedNames) : ASTNode;
public record QualifiedName(string ModuleName, string Name) : ASTNode;
public record FunctionDeclaration(string Name, List<Parameter> Parameters, string ReturnType, List<ASTNode> Body, EffectAnnotation? Effects = null, bool IsPure = false) : ASTNode;
public record Parameter(string Name, string Type);
public record EffectAnnotation(List<string> Effects) : ASTNode;
public record ReturnStatement(ASTNode Expression) : ASTNode;
public record LetStatement(string Name, ASTNode Expression) : ASTNode;
public record IfStatement(ASTNode Condition, List<ASTNode> ThenBody, List<ASTNode>? ElseBody = null) : ASTNode;
public record GuardStatement(ASTNode Condition, List<ASTNode> ElseBody) : ASTNode;
public record BinaryExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record UnaryExpression(string Operator, ASTNode Operand) : ASTNode;
public record Identifier(string Name) : ASTNode;
public record NumberLiteral(int Value) : ASTNode;
public record StringLiteral(string Value) : ASTNode;
public record StringInterpolation(List<ASTNode> Parts) : ASTNode; // Contains string literals and expressions

// Result type AST nodes
public record ResultType(string OkType, string ErrorType) : ASTNode;
public record OkExpression(ASTNode Value) : ASTNode;
public record ErrorExpression(ASTNode Value) : ASTNode;
public record ErrorPropagationExpression(ASTNode Expression) : ASTNode;
public record FunctionCall(string Name, List<ASTNode> Arguments) : ASTNode;

// Effect validation helper
public static class EffectValidator
{
    private static readonly HashSet<string> KnownEffects = new()
    {
        "Database", "Network", "Logging", "FileSystem", "Memory", "IO"
    };
    
    public static bool IsValidEffect(string effect)
    {
        return KnownEffects.Contains(effect);
    }
    
    public static void ValidateEffects(List<string> effects)
    {
        foreach (var effect in effects)
        {
            if (!IsValidEffect(effect))
            {
                throw new Exception($"Unknown effect: {effect}. Valid effects are: {string.Join(", ", KnownEffects)}");
            }
        }
    }
}

// =============================================================================
// LEXER
// =============================================================================

public class FlowLangLexer
{
    private readonly string _source;
    private int _position = 0;
    private int _line = 1;
    private int _column = 1;

    private readonly Dictionary<string, TokenType> _keywords = new()
    {
        ["function"] = TokenType.Function,
        ["return"] = TokenType.Return,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["effects"] = TokenType.Effects,
        ["pure"] = TokenType.Pure,
        ["uses"] = TokenType.Uses,
        ["int"] = TokenType.Int,
        ["string"] = TokenType.String_Type,
        ["bool"] = TokenType.Bool,
        ["Result"] = TokenType.Result,
        ["Ok"] = TokenType.Ok,
        ["Error"] = TokenType.Error,
        ["let"] = TokenType.Let,
        ["guard"] = TokenType.Guard,
        ["module"] = TokenType.Module,
        ["import"] = TokenType.Import,
        ["export"] = TokenType.Export,
        ["from"] = TokenType.From,
        // Effect names
        ["Database"] = TokenType.Database,
        ["Network"] = TokenType.Network,
        ["Logging"] = TokenType.Logging,
        ["FileSystem"] = TokenType.FileSystem,
        ["Memory"] = TokenType.Memory,
        ["IO"] = TokenType.IO,
    };

    public FlowLangLexer(string source)
    {
        _source = source;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        
        while (!IsAtEnd())
        {
            var token = NextToken();
            if (token != null)
            {
                tokens.Add(token);
            }
        }
        
        tokens.Add(new Token(TokenType.EOF, "", _line, _column));
        return tokens;
    }

    private Token? NextToken()
    {
        SkipWhitespace();
        
        if (IsAtEnd()) return null;
        
        var startLine = _line;
        var startColumn = _column;
        var c = Advance();

        return c switch
        {
            '(' => new Token(TokenType.LeftParen, "(", startLine, startColumn),
            ')' => new Token(TokenType.RightParen, ")", startLine, startColumn),
            '{' => new Token(TokenType.LeftBrace, "{", startLine, startColumn),
            '}' => new Token(TokenType.RightBrace, "}", startLine, startColumn),
            '[' => new Token(TokenType.LeftBracket, "[", startLine, startColumn),
            ']' => new Token(TokenType.RightBracket, "]", startLine, startColumn),
            ',' => new Token(TokenType.Comma, ",", startLine, startColumn),
            ';' => new Token(TokenType.Semicolon, ";", startLine, startColumn),
            ':' => new Token(TokenType.Colon, ":", startLine, startColumn),
            '=' => Match('=') ? new Token(TokenType.Equal, "==", startLine, startColumn) : new Token(TokenType.Assign, "=", startLine, startColumn),
            '!' => Match('=') ? new Token(TokenType.NotEqual, "!=", startLine, startColumn) : new Token(TokenType.Not, "!", startLine, startColumn),
            '&' => Match('&') ? new Token(TokenType.And, "&&", startLine, startColumn) : throw new Exception($"Unexpected character '&' at line {startLine}, column {startColumn}"),
            '|' => Match('|') ? new Token(TokenType.Or, "||", startLine, startColumn) : throw new Exception($"Unexpected character '|' at line {startLine}, column {startColumn}"),
            '+' => new Token(TokenType.Plus, "+", startLine, startColumn),
            '-' => Match('>') ? new Token(TokenType.Arrow, "->", startLine, startColumn) : new Token(TokenType.Minus, "-", startLine, startColumn),
            '.' => new Token(TokenType.Dot, ".", startLine, startColumn),
            '*' => new Token(TokenType.Multiply, "*", startLine, startColumn),
            '/' => Match('/') ? SkipComment(startLine, startColumn) : new Token(TokenType.Divide, "/", startLine, startColumn),
            '>' => Match('=') ? new Token(TokenType.GreaterEqual, ">=", startLine, startColumn) : new Token(TokenType.Greater, ">", startLine, startColumn),
            '<' => Match('=') ? new Token(TokenType.LessEqual, "<=", startLine, startColumn) : new Token(TokenType.Less, "<", startLine, startColumn),
            '?' => new Token(TokenType.Question, "?", startLine, startColumn),
            '$' => Match('"') ? ReadStringInterpolation(startLine, startColumn) : throw new Exception($"Unexpected character '{c}' at line {startLine}, column {startColumn}"),
            '"' => ReadString(startLine, startColumn),
            '\n' => new Token(TokenType.Newline, "\n", startLine, startColumn),
            _ when char.IsDigit(c) => ReadNumber(c, startLine, startColumn),
            _ when char.IsLetter(c) || c == '_' => ReadIdentifier(c, startLine, startColumn),
            _ => throw new Exception($"Unexpected character '{c}' at line {startLine}, column {startColumn}")
        };
    }

    private Token ReadNumber(char firstChar, int line, int column)
    {
        var value = firstChar.ToString();
        
        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            value += Advance();
        }
        
        return new Token(TokenType.Number, value, line, column);
    }

    private Token ReadString(int line, int column)
    {
        var value = "";
        
        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\n')
            {
                throw new Exception($"Unterminated string at line {line}, column {column}");
            }
            
            if (Peek() == '\\')
            {
                // Handle escape sequences
                Advance(); // consume the backslash
                if (IsAtEnd())
                {
                    throw new Exception($"Unterminated escape sequence in string at line {line}, column {column}");
                }
                
                var escaped = Advance();
                value += escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => escaped // For other characters, just include them as-is
                };
            }
            else
            {
                value += Advance();
            }
        }
        
        if (IsAtEnd())
        {
            throw new Exception($"Unterminated string at line {line}, column {column}");
        }
        
        // Consume closing quote
        Advance();
        
        return new Token(TokenType.String, value, line, column);
    }
    
    private Token ReadStringInterpolation(int line, int column)
    {
        var value = "";
        
        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\n')
            {
                throw new Exception($"Unterminated string interpolation at line {line}, column {column}");
            }
            value += Advance();
        }
        
        if (IsAtEnd())
        {
            throw new Exception($"Unterminated string interpolation at line {line}, column {column}");
        }
        
        // Consume closing quote
        Advance();
        
        return new Token(TokenType.StringInterpolation, value, line, column);
    }

    private Token ReadIdentifier(char firstChar, int line, int column)
    {
        var value = firstChar.ToString();
        
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            value += Advance();
        }
        
        var tokenType = _keywords.TryGetValue(value, out var keyword) ? keyword : TokenType.Identifier;
        return new Token(tokenType, value, line, column);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()) && Peek() != '\n')
        {
            Advance();
        }
    }

    private char Advance()
    {
        if (IsAtEnd()) return '\0';
        
        var c = _source[_position++];
        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
        return c;
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_position];

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_position] != expected) return false;
        Advance();
        return true;
    }

    private bool IsAtEnd() => _position >= _source.Length;
    
    private Token? SkipComment(int line, int column)
    {
        // Skip until end of line
        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
        return null; // Don't return a token for comments
    }
}