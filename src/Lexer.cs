using System;
using System.Collections.Generic;

namespace FlowLang.Compiler;

// Basic FlowLang Lexer
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
        ["int"] = TokenType.Int,
        ["string"] = TokenType.String_Type,
        ["bool"] = TokenType.Bool,
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
            '=' => new Token(TokenType.Assign, "=", startLine, startColumn),
            '+' => new Token(TokenType.Plus, "+", startLine, startColumn),
            '-' => Match('>') ? new Token(TokenType.Arrow, "->", startLine, startColumn) : new Token(TokenType.Minus, "-", startLine, startColumn),
            '*' => new Token(TokenType.Multiply, "*", startLine, startColumn),
            '/' => new Token(TokenType.Divide, "/", startLine, startColumn),
            '>' => Match('=') ? new Token(TokenType.GreaterEqual, ">=", startLine, startColumn) : new Token(TokenType.Greater, ">", startLine, startColumn),
            '<' => Match('=') ? new Token(TokenType.LessEqual, "<=", startLine, startColumn) : new Token(TokenType.Less, "<", startLine, startColumn),
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
}