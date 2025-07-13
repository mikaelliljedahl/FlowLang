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
        ["for"] = TokenType.For,
        ["in"] = TokenType.In,
        ["where"] = TokenType.Where,
        ["effects"] = TokenType.Effects,
        ["pure"] = TokenType.Pure,
        ["int"] = TokenType.Int,
        ["string"] = TokenType.String_Type,
        ["bool"] = TokenType.Bool,
        
        // Saga keywords
        ["saga"] = TokenType.Saga,
        ["step"] = TokenType.Step,
        ["compensate"] = TokenType.Compensate,
        ["transaction"] = TokenType.Transaction,
        
        // UI Component keywords
        ["component"] = TokenType.Component,
        ["state"] = TokenType.State,
        ["events"] = TokenType.Events,
        ["render"] = TokenType.Render,
        ["on_mount"] = TokenType.OnMount,
        ["on_unmount"] = TokenType.OnUnmount,
        ["on_update"] = TokenType.OnUpdate,
        ["event_handler"] = TokenType.EventHandler,
        ["declare_state"] = TokenType.DeclareState,
        ["set_state"] = TokenType.SetState,
        ["container"] = TokenType.Container,
        
        // State management keywords
        ["app_state"] = TokenType.AppState,
        ["action"] = TokenType.Action,
        ["updates"] = TokenType.Updates,
        
        // API client keywords
        ["api_client"] = TokenType.ApiClient,
        ["from"] = TokenType.From,
        ["endpoint"] = TokenType.Endpoint,
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
            '+' => new Token(TokenType.Plus, "+", startLine, startColumn),
            '-' => Match('>') ? new Token(TokenType.Arrow, "->", startLine, startColumn) : new Token(TokenType.Minus, "-", startLine, startColumn),
            '*' => new Token(TokenType.Multiply, "*", startLine, startColumn),
            '/' => new Token(TokenType.Divide, "/", startLine, startColumn),
            '%' => new Token(TokenType.Modulo, "%", startLine, startColumn),
            '>' => Match('=') ? new Token(TokenType.GreaterEqual, ">=", startLine, startColumn) : new Token(TokenType.Greater, ">", startLine, startColumn),
            '<' => Match('=') ? new Token(TokenType.LessEqual, "<=", startLine, startColumn) : new Token(TokenType.Less, "<", startLine, startColumn),
            '!' => Match('=') ? new Token(TokenType.NotEqual, "!=", startLine, startColumn) : new Token(TokenType.LogicalNot, "!", startLine, startColumn),
            '&' => Match('&') ? new Token(TokenType.LogicalAnd, "&&", startLine, startColumn) : throw new Exception($"Unexpected character '&' at line {startLine}, column {startColumn}"),
            '|' => Match('|') ? new Token(TokenType.LogicalOr, "||", startLine, startColumn) : throw new Exception($"Unexpected character '|' at line {startLine}, column {startColumn}"),
            '?' => new Token(TokenType.Question, "?", startLine, startColumn),
            '.' => new Token(TokenType.Dot, ".", startLine, startColumn),
            '\n' => new Token(TokenType.Newline, "\n", startLine, startColumn),
            '"' => ReadStringLiteral(startLine, startColumn),
            '$' => ReadInterpolatedString(startLine, startColumn),
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

    private Token ReadStringLiteral(int line, int column)
    {
        var value = "";
        
        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Advance(); // Skip backslash
                if (!IsAtEnd())
                {
                    var escaped = Advance();
                    value += escaped switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '"' => '"',
                        _ => escaped
                    };
                }
            }
            else
            {
                value += Advance();
            }
        }
        
        if (IsAtEnd())
        {
            throw new Exception($"Unterminated string literal at line {line}, column {column}");
        }
        
        Advance(); // Consume closing quote
        return new Token(TokenType.String, value, line, column);
    }

    private Token ReadInterpolatedString(int line, int column)
    {
        if (!Match('"'))
        {
            throw new Exception($"Expected '\"' after '$' at line {line}, column {column}");
        }
        
        var value = "";
        
        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Advance(); // Skip backslash
                if (!IsAtEnd())
                {
                    var escaped = Advance();
                    value += escaped switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '"' => '"',
                        _ => escaped
                    };
                }
            }
            else
            {
                value += Advance();
            }
        }
        
        if (IsAtEnd())
        {
            throw new Exception($"Unterminated interpolated string literal at line {line}, column {column}");
        }
        
        Advance(); // Consume closing quote
        return new Token(TokenType.InterpolatedString, value, line, column);
    }
}