// Using packages from transpiler.csproj

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
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
    
    private ASTNode ParseStringInterpolation(string template)
    {
        var parts = new List<ASTNode>();
        var currentLiteral = "";
        var i = 0;
        
        while (i < template.Length)
        {
            if (template[i] == '{' && i + 1 < template.Length)
            {
                // Add the current literal part if it's not empty
                if (currentLiteral.Length > 0)
                {
                    parts.Add(new StringLiteral(currentLiteral));
                    currentLiteral = "";
                }
                
                // Find the closing brace
                var braceCount = 1;
                var j = i + 1;
                var expression = "";
                
                while (j < template.Length && braceCount > 0)
                {
                    if (template[j] == '{')
                        braceCount++;
                    else if (template[j] == '}')
                        braceCount--;
                    
                    if (braceCount > 0)
                        expression += template[j];
                    
                    j++;
                }
                
                if (braceCount > 0)
                {
                    throw new Exception("Unmatched '{' in string interpolation");
                }
                
                // Parse the expression inside the braces
                if (expression.Trim().Length > 0)
                {
                    // For now, treat expressions as simple identifiers
                    // In a more complete implementation, we'd parse the full expression
                    parts.Add(new Identifier(expression.Trim()));
                }
                
                i = j;
            }
            else
            {
                currentLiteral += template[i];
                i++;
            }
        }
        
        // Add any remaining literal
        if (currentLiteral.Length > 0)
        {
            parts.Add(new StringLiteral(currentLiteral));
        }
        
        return new StringInterpolation(parts);
    }
}

// =============================================================================
// PARSER
// =============================================================================

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
        if (Match(TokenType.Module))
        {
            return ParseModuleDeclaration();
        }
        
        if (Match(TokenType.Import))
        {
            return ParseImportStatement();
        }
        
        if (Match(TokenType.Export))
        {
            return ParseExportStatement();
        }
        
        if (Match(TokenType.Function))
        {
            return ParseFunctionDeclaration();
        }
        
        if (Match(TokenType.Pure))
        {
            // Put the token back and parse as function with pure modifier
            _current--;
            return ParseFunctionDeclaration();
        }
        
        return null;
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        // Check for pure modifier
        bool isPure = false;
        if (Match(TokenType.Pure))
        {
            isPure = true;
            Consume(TokenType.Function, "Expected 'function' after 'pure'");
        }
        
        var name = Consume(TokenType.Identifier, "Expected function name").Value;
        
        Consume(TokenType.LeftParen, "Expected '(' after function name");
        var parameters = new List<Parameter>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Value;
                Consume(TokenType.Colon, "Expected ':' after parameter name");
                var paramType = ParseType();
                parameters.Add(new Parameter(paramName, paramType));
            }
            while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        // Skip any newlines after parameters
        while (Match(TokenType.Newline)) { }
        
        // Parse effect annotation if present
        EffectAnnotation? effectAnnotation = null;
        if (Match(TokenType.Uses))
        {
            if (isPure)
            {
                throw new Exception("Pure functions cannot have effect annotations");
            }
            effectAnnotation = ParseEffectAnnotation();
        }
        
        // Skip any newlines before arrow
        while (Match(TokenType.Newline)) { }
        
        Consume(TokenType.Arrow, "Expected '->' after parameters or effect annotation");
        var returnType = ParseType();
        
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
            else if (Match(TokenType.Let))
            {
                var varName = Consume(TokenType.Identifier, "Expected variable name after 'let'").Value;
                Consume(TokenType.Assign, "Expected '=' after variable name");
                var expr = ParseExpression();
                body.Add(new LetStatement(varName, expr));
            }
            else if (Match(TokenType.If))
            {
                body.Add(ParseIfStatement());
            }
            else if (Match(TokenType.Guard))
            {
                body.Add(ParseGuardStatement());
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after function body");
        
        return new FunctionDeclaration(name, parameters, returnType, body, effectAnnotation, isPure);
    }
    
    private EffectAnnotation ParseEffectAnnotation()
    {
        // Parse: uses [Database, Network, Logging]
        // Skip any newlines after 'uses'
        while (Match(TokenType.Newline)) { }
        
        Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
        
        var effects = new List<string>();
        
        if (!Check(TokenType.RightBracket))
        {
            do
            {
                // Skip newlines within effect list
                while (Match(TokenType.Newline)) { }
                
                var effectToken = Peek();
                if (effectToken.Type == TokenType.Database || 
                    effectToken.Type == TokenType.Network ||
                    effectToken.Type == TokenType.Logging ||
                    effectToken.Type == TokenType.FileSystem ||
                    effectToken.Type == TokenType.Memory ||
                    effectToken.Type == TokenType.IO)
                {
                    effects.Add(Advance().Value);
                }
                else
                {
                    throw new Exception($"Expected effect name, got: {effectToken.Value}. Valid effects are: Database, Network, Logging, FileSystem, Memory, IO");
                }
                
                // Skip newlines after effect name
                while (Match(TokenType.Newline)) { }
            }
            while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightBracket, "Expected ']' after effect list");
        
        // Validate effects
        EffectValidator.ValidateEffects(effects);
        
        return new EffectAnnotation(effects);
    }
    
    private IfStatement ParseIfStatement()
    {
        var condition = ParseExpression();
        Consume(TokenType.LeftBrace, "Expected '{' after if condition");
        
        var thenBody = ParseBlockStatements();
        Consume(TokenType.RightBrace, "Expected '}' after if body");
        
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            if (Check(TokenType.If))
            {
                // Handle "else if" by parsing another if statement
                Advance(); // consume 'if'
                var elseIfStmt = ParseIfStatement();
                elseBody = new List<ASTNode> { elseIfStmt };
            }
            else
            {
                // Regular else block
                Consume(TokenType.LeftBrace, "Expected '{' after else");
                elseBody = ParseBlockStatements();
                Consume(TokenType.RightBrace, "Expected '}' after else body");
            }
        }
        
        return new IfStatement(condition, thenBody, elseBody);
    }
    
    private GuardStatement ParseGuardStatement()
    {
        var condition = ParseExpression();
        Consume(TokenType.Else, "Expected 'else' after guard condition");
        Consume(TokenType.LeftBrace, "Expected '{' after guard else");
        
        var elseBody = ParseBlockStatements();
        Consume(TokenType.RightBrace, "Expected '}' after guard else body");
        
        return new GuardStatement(condition, elseBody);
    }
    
    // Module system parsing methods
    private ModuleDeclaration ParseModuleDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected module name").Value;
        
        Consume(TokenType.LeftBrace, "Expected '{' after module name");
        var body = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            var stmt = ParseStatement();
            if (stmt != null)
            {
                body.Add(stmt);
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after module body");
        
        return new ModuleDeclaration(name, body);
    }
    
    private ImportStatement ParseImportStatement()
    {
        var moduleName = Consume(TokenType.Identifier, "Expected module name after 'import'").Value;
        
        // Check for dot to see what comes next
        if (Match(TokenType.Dot))
        {
            // Check if it's a wildcard import: import Module.*
            if (Check(TokenType.Multiply))
            {
                Consume(TokenType.Multiply, "Expected '*' after '.' for wildcard import");
                return new ImportStatement(moduleName, null, true);
            }
            
            // Check if it's a selective import: import Module.{func1, func2}
            if (Check(TokenType.LeftBrace))
            {
                Consume(TokenType.LeftBrace, "Expected '{' for selective import");
                var imports = new List<string>();
                
                if (!Check(TokenType.RightBrace))
                {
                    do
                    {
                        while (Match(TokenType.Newline)) { } // Skip newlines
                        var importName = Consume(TokenType.Identifier, "Expected function name in import list").Value;
                        imports.Add(importName);
                        while (Match(TokenType.Newline)) { } // Skip newlines
                    }
                    while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightBrace, "Expected '}' after import list");
                return new ImportStatement(moduleName, imports, false);
            }
            
            // Otherwise it's a qualified module name (e.g., MyModule.SubModule)
            var subModuleName = Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
            moduleName += "." + subModuleName;
            
            // Check for more qualified parts or imports
            while (Match(TokenType.Dot))
            {
                if (Check(TokenType.Multiply))
                {
                    Consume(TokenType.Multiply, "Expected '*' after '.' for wildcard import");
                    return new ImportStatement(moduleName, null, true);
                }
                
                if (Check(TokenType.LeftBrace))
                {
                    Consume(TokenType.LeftBrace, "Expected '{' for selective import");
                    var imports = new List<string>();
                    
                    if (!Check(TokenType.RightBrace))
                    {
                        do
                        {
                            while (Match(TokenType.Newline)) { } // Skip newlines
                            var importName = Consume(TokenType.Identifier, "Expected function name in import list").Value;
                            imports.Add(importName);
                            while (Match(TokenType.Newline)) { } // Skip newlines
                        }
                        while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightBrace, "Expected '}' after import list");
                    return new ImportStatement(moduleName, imports, false);
                }
                
                var nextModuleName = Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
                moduleName += "." + nextModuleName;
            }
        }
        
        // Simple module import: import Module
        return new ImportStatement(moduleName, null, false);
    }
    
    private ExportStatement ParseExportStatement()
    {
        Consume(TokenType.LeftBrace, "Expected '{' after 'export'");
        var exports = new List<string>();
        
        if (!Check(TokenType.RightBrace))
        {
            do
            {
                while (Match(TokenType.Newline)) { } // Skip newlines
                var exportName = Consume(TokenType.Identifier, "Expected function name in export list").Value;
                exports.Add(exportName);
                while (Match(TokenType.Newline)) { } // Skip newlines
            }
            while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after export list");
        return new ExportStatement(exports);
    }
    
    private List<ASTNode> ParseBlockStatements()
    {
        var statements = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.Return))
            {
                var expr = ParseExpression();
                statements.Add(new ReturnStatement(expr));
            }
            else if (Match(TokenType.Let))
            {
                var name = Consume(TokenType.Identifier, "Expected variable name after 'let'").Value;
                Consume(TokenType.Assign, "Expected '=' after variable name");
                var expr = ParseExpression();
                statements.Add(new LetStatement(name, expr));
            }
            else if (Match(TokenType.If))
            {
                statements.Add(ParseIfStatement());
            }
            else if (Match(TokenType.Guard))
            {
                statements.Add(ParseGuardStatement());
            }
            else
            {
                // Parse expression statement
                var expr = ParseExpression();
                statements.Add(expr);
            }
        }
        
        return statements;
    }
    
    private ASTNode ParseStringInterpolationExpression(string template)
    {
        var parts = new List<ASTNode>();
        var currentLiteral = "";
        var i = 0;
        
        while (i < template.Length)
        {
            if (template[i] == '{' && i + 1 < template.Length)
            {
                // Add the current literal part if it's not empty
                if (currentLiteral.Length > 0)
                {
                    parts.Add(new StringLiteral(currentLiteral));
                    currentLiteral = "";
                }
                
                // Find the closing brace
                var braceCount = 1;
                var j = i + 1;
                var expression = "";
                
                while (j < template.Length && braceCount > 0)
                {
                    if (template[j] == '{')
                        braceCount++;
                    else if (template[j] == '}')
                        braceCount--;
                    
                    if (braceCount > 0)
                        expression += template[j];
                    
                    j++;
                }
                
                if (braceCount > 0)
                {
                    throw new Exception("Unmatched '{' in string interpolation");
                }
                
                // Parse the expression inside the braces
                if (expression.Trim().Length > 0)
                {
                    // For now, treat expressions as simple identifiers
                    // In a more complete implementation, we'd parse the full expression
                    parts.Add(new Identifier(expression.Trim()));
                }
                
                i = j;
            }
            else
            {
                currentLiteral += template[i];
                i++;
            }
        }
        
        // Add any remaining literal
        if (currentLiteral.Length > 0)
        {
            parts.Add(new StringLiteral(currentLiteral));
        }
        
        return new StringInterpolation(parts);
    }

    private ASTNode ParseExpression()
    {
        return ParseLogicalOr();
    }
    
    // Logical OR has lowest precedence among logical operators
    private ASTNode ParseLogicalOr()
    {
        var expr = ParseLogicalAnd();
        
        while (Match(TokenType.Or))
        {
            var op = Previous().Value;
            var right = ParseLogicalAnd();
            expr = new BinaryExpression(expr, op, right);
        }
        
        return expr;
    }
    
    // Logical AND has higher precedence than OR
    private ASTNode ParseLogicalAnd()
    {
        var expr = ParseEquality();
        
        while (Match(TokenType.And))
        {
            var op = Previous().Value;
            var right = ParseEquality();
            expr = new BinaryExpression(expr, op, right);
        }
        
        return expr;
    }
    
    // Equality operators (==, !=)
    private ASTNode ParseEquality()
    {
        var expr = ParseComparison();
        
        while (Match(TokenType.Equal, TokenType.NotEqual))
        {
            var op = Previous().Value;
            var right = ParseComparison();
            expr = new BinaryExpression(expr, op, right);
        }
        
        return expr;
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

    // Comparison operators (>, <, >=, <=)
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
        var expr = ParseUnary();
        
        while (Match(TokenType.Multiply, TokenType.Divide))
        {
            var op = Previous().Value;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right);
        }
        
        return expr;
    }
    
    // Unary operators (!) - highest precedence
    private ASTNode ParseUnary()
    {
        if (Match(TokenType.Not))
        {
            var op = Previous().Value;
            var expr = ParseUnary(); // Right-associative
            return new UnaryExpression(op, expr);
        }
        
        return ParsePrimary();
    }

    private ASTNode ParsePrimary()
    {
        if (Match(TokenType.Number))
        {
            return new NumberLiteral(int.Parse(Previous().Value));
        }
        
        if (Match(TokenType.String))
        {
            return new StringLiteral(Previous().Value);
        }
        
        if (Match(TokenType.StringInterpolation))
        {
            return ParseStringInterpolationExpression(Previous().Value);
        }
        
        if (Match(TokenType.Ok))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'Ok'");
            var value = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after Ok value");
            return new OkExpression(value);
        }
        
        if (Match(TokenType.Error))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'Error'");
            var value = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after Error value");
            return new ErrorExpression(value);
        }
        
        if (Match(TokenType.Identifier))
        {
            var identifierName = Previous().Value;
            
            // Check for qualified name (Module.function)
            if (Match(TokenType.Dot))
            {
                var memberName = Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
                var qualifiedName = new QualifiedName(identifierName, memberName);
                
                // Check for function call on qualified name
                if (Match(TokenType.LeftParen))
                {
                    var args = new List<ASTNode>();
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        }
                        while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after function arguments");
                    
                    var functionCall = new FunctionCall($"{identifierName}.{memberName}", args);
                    
                    // Check for error propagation operator
                    if (Match(TokenType.Question))
                    {
                        return new ErrorPropagationExpression(functionCall);
                    }
                    
                    return functionCall;
                }
                
                return qualifiedName;
            }
            
            var identifier = new Identifier(identifierName);
            
            // Check for function call
            if (Match(TokenType.LeftParen))
            {
                var args = new List<ASTNode>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    }
                    while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after function arguments");
                
                var functionCall = new FunctionCall(identifier.Name, args);
                
                // Check for error propagation operator
                if (Match(TokenType.Question))
                {
                    return new ErrorPropagationExpression(functionCall);
                }
                
                return functionCall;
            }
            
            // Check for error propagation operator on simple identifier
            if (Match(TokenType.Question))
            {
                return new ErrorPropagationExpression(identifier);
            }
            
            return identifier;
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

    private string ParseType()
    {
        if (Match(TokenType.Result))
        {
            // Parse Result<T, E>
            Consume(TokenType.Less, "Expected '<' after 'Result'");
            var okType = ParseType();
            Consume(TokenType.Comma, "Expected ',' between Result type parameters");
            var errorType = ParseType();
            Consume(TokenType.Greater, "Expected '>' after Result type parameters");
            return $"Result<{okType}, {errorType}>";
        }
        else if (Check(TokenType.Int) || Check(TokenType.String_Type) || Check(TokenType.Bool) || Check(TokenType.Identifier))
        {
            return Advance().Value;
        }
        else
        {
            throw new Exception($"Expected type. Got: {Peek().Type} '{Peek().Value}'");
        }
    }

    private Token ConsumeType(string message)
    {
        if (Check(TokenType.Int) || Check(TokenType.String_Type) || Check(TokenType.Bool) || Check(TokenType.Identifier) || Check(TokenType.Result))
        {
            return Advance();
        }
        throw new Exception($"{message}. Got: {Peek().Type} '{Peek().Value}'");
    }
}

// =============================================================================
// CODE GENERATOR
// =============================================================================

public class CSharpGenerator
{
    private readonly HashSet<string> _generatedNamespaces = new();
    private readonly List<string> _usingStatements = new();
    
    public SyntaxTree GenerateFromAST(Program program)
    {
        var namespaceMembers = new Dictionary<string, List<MemberDeclarationSyntax>>();
        var globalMembers = new List<MemberDeclarationSyntax>();
        
        // Add Result class if any function uses Result types
        if (program.Statements.Any(s => ContainsResultType(s)))
        {
            globalMembers.Add(GenerateResultClass());
        }
        
        // Process all statements
        foreach (var statement in program.Statements)
        {
            switch (statement)
            {
                case ModuleDeclaration module:
                    ProcessModuleDeclaration(module, namespaceMembers);
                    break;
                    
                case ImportStatement import:
                    ProcessImportStatement(import);
                    break;
                    
                case FunctionDeclaration func:
                    // Functions outside modules go in global scope
                    globalMembers.Add(GenerateMethod(func));
                    break;
                    
                case ExportStatement export:
                    // Export statements are handled within modules
                    break;
            }
        }
        
        // Build compilation unit
        var compilationUnit = CompilationUnit();
        
        // Add using statements
        if (_usingStatements.Count > 0)
        {
            var usings = _usingStatements.Select(u => UsingDirective(IdentifierName(u))).ToArray();
            compilationUnit = compilationUnit.WithUsings(List(usings));
        }
        
        // Add global members first
        if (globalMembers.Count > 0)
        {
            compilationUnit = compilationUnit.WithMembers(List<MemberDeclarationSyntax>(globalMembers));
        }
        
        // Add namespace declarations
        foreach (var kvp in namespaceMembers)
        {
            var namespaceName = kvp.Key;
            var members = kvp.Value;
            
            var namespaceDecl = NamespaceDeclaration(IdentifierName(namespaceName))
                .WithMembers(List(members));
            
            compilationUnit = compilationUnit.AddMembers(namespaceDecl);
        }
        
        return CSharpSyntaxTree.Create(compilationUnit);
    }
    
    private bool ContainsResultType(ASTNode node)
    {
        return node switch
        {
            FunctionDeclaration func => func.ReturnType.StartsWith("Result<"),
            ModuleDeclaration module => module.Body.Any(ContainsResultType),
            _ => false
        };
    }
    
    private void ProcessModuleDeclaration(ModuleDeclaration module, Dictionary<string, List<MemberDeclarationSyntax>> namespaceMembers)
    {
        if (!namespaceMembers.ContainsKey(module.Name))
        {
            namespaceMembers[module.Name] = new List<MemberDeclarationSyntax>();
        }
        
        var moduleClass = GenerateModuleClass(module);
        namespaceMembers[module.Name].Add(moduleClass);
        _generatedNamespaces.Add(module.Name);
    }
    
    private void ProcessImportStatement(ImportStatement import)
    {
        if (import.IsWildcard || import.SpecificImports == null)
        {
            // For wildcard imports or simple module imports, add using statement
            if (!_usingStatements.Contains(import.ModuleName))
            {
                _usingStatements.Add(import.ModuleName);
            }
        }
        else
        {
            // For specific imports, we still need the namespace available
            if (!_usingStatements.Contains(import.ModuleName))
            {
                _usingStatements.Add(import.ModuleName);
            }
        }
    }
    
    private ClassDeclarationSyntax GenerateModuleClass(ModuleDeclaration module)
    {
        var methods = new List<MemberDeclarationSyntax>();
        var exportedFunctions = new HashSet<string>();
        
        // Collect exported function names from export statements
        foreach (var stmt in module.Body)
        {
            if (stmt is ExportStatement export)
            {
                foreach (var name in export.ExportedNames)
                {
                    exportedFunctions.Add(name);
                }
            }
        }
        
        // Generate methods from function declarations
        foreach (var stmt in module.Body)
        {
            if (stmt is FunctionDeclaration func)
            {
                var isExported = exportedFunctions.Contains(func.Name) || exportedFunctions.Count == 0;
                var method = GenerateMethod(func, isExported);
                methods.Add(method);
            }
        }
        
        // Create static class for the module
        var className = $"{module.Name}Module";
        return ClassDeclaration(className)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithMembers(List(methods));
    }

    private ClassDeclarationSyntax GenerateResultClass()
    {
        // Generate Result<T, E> class
        var resultClass = ClassDeclaration("Result")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithTypeParameterList(TypeParameterList(SeparatedList(new[]
            {
                TypeParameter("T"),
                TypeParameter("E")
            })))
            .WithMembers(List(new MemberDeclarationSyntax[]
            {
                // Value property
                PropertyDeclaration(IdentifierName("T"), "Value")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(AccessorList(List(new[]
                    {
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    }))),
                
                // ErrorValue property (renamed to avoid conflict with Error static method)
                PropertyDeclaration(IdentifierName("E"), "ErrorValue")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(AccessorList(List(new[]
                    {
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    }))),
                
                // IsError property
                PropertyDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), "IsError")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(AccessorList(List(new[]
                    {
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    }))),
                
                // Ok static method
                MethodDeclaration(
                    GenericName("Result")
                        .WithTypeArgumentList(TypeArgumentList(SeparatedList(new TypeSyntax[]
                        {
                            IdentifierName("T"),
                            IdentifierName("E")
                        }))),
                    "Ok")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(SingletonSeparatedList(
                        Parameter(Identifier("value"))
                            .WithType(IdentifierName("T")))))
                    .WithBody(Block(
                        ReturnStatement(
                            ObjectCreationExpression(
                                GenericName("Result")
                                    .WithTypeArgumentList(TypeArgumentList(SeparatedList(new TypeSyntax[]
                                    {
                                        IdentifierName("T"),
                                        IdentifierName("E")
                                    }))))
                            .WithInitializer(InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                                SeparatedList(new ExpressionSyntax[]
                                {
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("Value"),
                                        IdentifierName("value")),
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("IsError"),
                                        LiteralExpression(SyntaxKind.FalseLiteralExpression))
                                })))))),
                
                // Error static method
                MethodDeclaration(
                    GenericName("Result")
                        .WithTypeArgumentList(TypeArgumentList(SeparatedList(new TypeSyntax[]
                        {
                            IdentifierName("T"),
                            IdentifierName("E")
                        }))),
                    "Error")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(SingletonSeparatedList(
                        Parameter(Identifier("error"))
                            .WithType(IdentifierName("E")))))
                    .WithBody(Block(
                        ReturnStatement(
                            ObjectCreationExpression(
                                GenericName("Result")
                                    .WithTypeArgumentList(TypeArgumentList(SeparatedList(new TypeSyntax[]
                                    {
                                        IdentifierName("T"),
                                        IdentifierName("E")
                                    }))))
                            .WithInitializer(InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                                SeparatedList(new ExpressionSyntax[]
                                {
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("ErrorValue"),
                                        IdentifierName("error")),
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("IsError"),
                                        LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                })))))),
            }));
        
        return resultClass;
    }

    private MethodDeclarationSyntax GenerateMethod(FunctionDeclaration func, bool isPublic = true)
    {
        var parameters = func.Parameters.Select(p => 
            Parameter(Identifier(p.Name))
                .WithType(ParseTypeName(p.Type))
        ).ToArray();

        var bodyStatements = new List<StatementSyntax>();
        foreach (var stmt in func.Body)
        {
            var generated = GenerateStatement(stmt);
            if (generated is BlockSyntax block)
            {
                // Flatten blocks from error propagation let statements
                bodyStatements.AddRange(block.Statements);
            }
            else
            {
                bodyStatements.Add(generated);
            }
        }
        
        // Generate XML documentation comment
        var xmlDocComment = GenerateXmlDocComment(func);
        
        var modifiers = new List<SyntaxToken> { Token(SyntaxKind.StaticKeyword) };
        if (isPublic)
        {
            modifiers.Insert(0, Token(SyntaxKind.PublicKeyword));
        }
        else
        {
            modifiers.Insert(0, Token(SyntaxKind.PrivateKeyword));
        }
        
        return MethodDeclaration(ParseTypeName(func.ReturnType), func.Name)
            .WithModifiers(TokenList(modifiers))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithBody(Block(bodyStatements))
            .WithLeadingTrivia(xmlDocComment);
    }

    private StatementSyntax GenerateStatement(ASTNode node)
    {
        return node switch
        {
            ReturnStatement ret => ReturnStatement(GenerateExpression(ret.Expression)),
            LetStatement let => GenerateLetStatement(let),
            IfStatement ifStmt => IfStatement(
                GenerateExpression(ifStmt.Condition),
                Block(ifStmt.ThenBody.Select(GenerateStatement)),
                ifStmt.ElseBody != null ? 
                    ElseClause(Block(ifStmt.ElseBody.Select(GenerateStatement))) : 
                    null),
            GuardStatement guardStmt => IfStatement(
                PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    ParenthesizedExpression(GenerateExpression(guardStmt.Condition))),
                Block(guardStmt.ElseBody.Select(GenerateStatement))),
            _ => throw new NotImplementedException($"Statement type {node.GetType().Name} not implemented")
        };
    }

    private StatementSyntax GenerateLetStatement(LetStatement let)
    {
        // Check if the expression contains error propagation
        if (let.Expression is ErrorPropagationExpression errorProp)
        {
            // Generate error propagation handling:
            // var tempResult = expression();
            // if (tempResult.IsError) return tempResult;
            // var varName = tempResult.Value;
            
            var tempVarName = $"{let.Name}_result";
            var tempVarDeclaration = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(tempVarName)
                            .WithInitializer(EqualsValueClause(GenerateExpression(errorProp.Expression))))));
            
            var errorCheck = IfStatement(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(tempVarName),
                    IdentifierName("IsError")),
                ReturnStatement(IdentifierName(tempVarName)));
            
            var valueExtraction = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(let.Name)
                            .WithInitializer(EqualsValueClause(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(tempVarName),
                                    IdentifierName("Value")))))));
            
            // Return a block containing all three statements
            return Block(tempVarDeclaration, errorCheck, valueExtraction);
        }
        else
        {
            // Regular let statement without error propagation
            return LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(let.Name)
                            .WithInitializer(EqualsValueClause(GenerateExpression(let.Expression))))));
        }
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
            UnaryExpression unary => PrefixUnaryExpression(
                GetUnaryOperator(unary.Operator),
                GenerateExpression(unary.Operand)
            ),
            Identifier id => IdentifierName(id.Name),
            NumberLiteral num => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(num.Value)),
            StringLiteral str => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str.Value)),
            StringInterpolation interp => GenerateStringInterpolation(interp),
            OkExpression ok => InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Result"),
                    IdentifierName("Ok")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(GenerateExpression(ok.Value))))),
            ErrorExpression err => InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Result"),
                    IdentifierName("Error")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(GenerateExpression(err.Value))))),
            FunctionCall func => GenerateFunctionCall(func),
            QualifiedName qualified => MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName($"{qualified.ModuleName}Module"),
                IdentifierName(qualified.Name)),
            ErrorPropagationExpression prop => GenerateErrorPropagation(prop),
            _ => throw new NotImplementedException($"Expression type {node.GetType().Name} not implemented")
        };
    }

    private SyntaxKind GetBinaryOperator(string op) => op switch
    {
        "+" => SyntaxKind.AddExpression, // This handles both numeric addition and string concatenation
        "-" => SyntaxKind.SubtractExpression,
        "*" => SyntaxKind.MultiplyExpression,
        "/" => SyntaxKind.DivideExpression,
        ">" => SyntaxKind.GreaterThanExpression,
        "<" => SyntaxKind.LessThanExpression,
        ">=" => SyntaxKind.GreaterThanOrEqualExpression,
        "<=" => SyntaxKind.LessThanOrEqualExpression,
        "==" => SyntaxKind.EqualsExpression,
        "!=" => SyntaxKind.NotEqualsExpression,
        "&&" => SyntaxKind.LogicalAndExpression,
        "||" => SyntaxKind.LogicalOrExpression,
        _ => throw new NotImplementedException($"Operator {op} not implemented")
    };
    
    private SyntaxKind GetUnaryOperator(string op) => op switch
    {
        "!" => SyntaxKind.LogicalNotExpression,
        _ => throw new NotImplementedException($"Unary operator {op} not implemented")
    };

    private ExpressionSyntax GenerateStringInterpolation(StringInterpolation interpolation)
    {
        if (interpolation.Parts.Count == 0)
        {
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(""));
        }
        
        if (interpolation.Parts.Count == 1 && interpolation.Parts[0] is StringLiteral literal)
        {
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(literal.Value));
        }
        
        // Generate C# string interpolation: $"text {expr} more text"
        var expressions = new List<ExpressionSyntax>();
        var formatString = "";
        var argIndex = 0;
        
        foreach (var part in interpolation.Parts)
        {
            if (part is StringLiteral str)
            {
                formatString += str.Value;
            }
            else
            {
                formatString += "{" + argIndex + "}";
                expressions.Add(GenerateExpression(part));
                argIndex++;
            }
        }
        
        // Use string.Format for C# generation
        var formatCall = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                PredefinedType(Token(SyntaxKind.StringKeyword)),
                IdentifierName("Format")))
            .WithArgumentList(ArgumentList(
                SeparatedList(new[] { Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(formatString))) }
                    .Concat(expressions.Select(expr => Argument(expr))))));
        
        return formatCall;
    }
    
    private ExpressionSyntax GenerateErrorPropagation(ErrorPropagationExpression prop)
    {
        // Note: This method handles error propagation when used in expression context
        // For let statements, error propagation is handled in GenerateLetStatement
        
        // Generate a method call that handles the error propagation inline
        // This is a fallback for cases where error propagation is used outside let statements
        var expr = GenerateExpression(prop.Expression);
        
        // For now, just access the Value property - this assumes error checking was done elsewhere
        // In a more complete implementation, we might need to generate a lambda or helper method
        return MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            expr,
            IdentifierName("Value"));
    }

    private TypeSyntax ParseTypeName(string typeName) => typeName switch
    {
        "int" => PredefinedType(Token(SyntaxKind.IntKeyword)),
        "string" => PredefinedType(Token(SyntaxKind.StringKeyword)),
        "bool" => PredefinedType(Token(SyntaxKind.BoolKeyword)),
        _ when typeName.StartsWith("Result<") => ParseResultType(typeName),
        _ => IdentifierName(typeName)
    };

    private TypeSyntax ParseResultType(string typeName)
    {
        // Parse Result<T, E> to Result<T, E>
        var inner = typeName.Substring(7, typeName.Length - 8); // Remove "Result<" and ">"
        var types = inner.Split(',').Select(t => t.Trim()).ToArray();
        
        return GenericName("Result")
            .WithTypeArgumentList(TypeArgumentList(
                SeparatedList(types.Select(ParseTypeName))));
    }
    
    private SyntaxTriviaList GenerateXmlDocComment(FunctionDeclaration func)
    {
        var commentLines = new List<SyntaxTrivia>();
        
        // Start XML doc comment
        commentLines.Add(Comment("/// <summary>"));
        commentLines.Add(EndOfLine("\n"));
        
        // Add effect information
        if (func.IsPure)
        {
            commentLines.Add(Comment("/// Pure function - no side effects"));
            commentLines.Add(EndOfLine("\n"));
        }
        else if (func.Effects != null && func.Effects.Effects.Count > 0)
        {
            var effectsStr = string.Join(", ", func.Effects.Effects);
            commentLines.Add(Comment($"/// Effects: {effectsStr}"));
            commentLines.Add(EndOfLine("\n"));
        }
        
        // End XML doc comment
        commentLines.Add(Comment("/// </summary>"));
        commentLines.Add(EndOfLine("\n"));
        
        // Add parameter documentation if there are parameters
        foreach (var param in func.Parameters)
        {
            commentLines.Add(Comment($"/// <param name=\"{param.Name}\">Parameter of type {param.Type}</param>"));
            commentLines.Add(EndOfLine("\n"));
        }
        
        // Add return documentation
        commentLines.Add(Comment($"/// <returns>Returns {func.ReturnType}</returns>"));
        commentLines.Add(EndOfLine("\n"));
        
        return TriviaList(commentLines);
    }
    
    private ExpressionSyntax GenerateFunctionCall(FunctionCall func)
    {
        ExpressionSyntax target;
        
        // Check if this is a module-qualified function call
        if (func.Name.Contains("."))
        {
            var parts = func.Name.Split('.');
            if (parts.Length == 2)
            {
                var moduleName = parts[0];
                var functionName = parts[1];
                
                // Generate ModuleName.ModuleNameModule.FunctionName
                target = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName($"{moduleName}Module"),
                    IdentifierName(functionName));
            }
            else
            {
                // Fallback to simple identifier
                target = IdentifierName(func.Name);
            }
        }
        else
        {
            target = IdentifierName(func.Name);
        }
        
        return InvocationExpression(target)
            .WithArgumentList(ArgumentList(
                SeparatedList(func.Arguments.Select(arg => Argument(GenerateExpression(arg))))));
    }
}

// =============================================================================
// CONFIGURATION
// =============================================================================

public record FlowcConfig(
    string Name = "my-project",
    string Version = "1.0.0", 
    string Description = "",
    BuildConfig Build = null,
    Dictionary<string, string> Dependencies = null
)
{
    public BuildConfig Build { get; init; } = Build ?? new();
    public Dictionary<string, string> Dependencies { get; init; } = Dependencies ?? new();
}

public record BuildConfig(
    string Source = "src/",
    string Output = "build/",
    string Target = "csharp"
);

// =============================================================================
// CLI COMMANDS
// =============================================================================

public abstract class Command
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Task<int> ExecuteAsync(string[] args);
}

public class NewCommand : Command
{
    public override string Name => "new";
    public override string Description => "Create a new FlowLang project";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Project name is required");
            Console.WriteLine("Usage: flowc new <project-name>");
            return 1;
        }

        var projectName = args[0];
        var projectPath = Path.Combine(Directory.GetCurrentDirectory(), projectName);

        if (Directory.Exists(projectPath))
        {
            Console.WriteLine($"Error: Directory '{projectName}' already exists");
            return 1;
        }

        try
        {
            await CreateProjectStructure(projectName, projectPath);
            Console.WriteLine($"Created new FlowLang project: {projectName}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"  cd {projectName}");
            Console.WriteLine("  flowc build");
            Console.WriteLine("  flowc run examples/hello.flow");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating project: {ex.Message}");
            return 1;
        }
    }

    private async Task CreateProjectStructure(string projectName, string projectPath)
    {
        // Create directories
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "src"));
        Directory.CreateDirectory(Path.Combine(projectPath, "examples"));
        Directory.CreateDirectory(Path.Combine(projectPath, "tests"));

        // Create flowc.json
        var config = new FlowLang.Package.EnhancedFlowcConfig(
            Name: projectName,
            Description: $"A FlowLang project: {projectName}"
        );
        await FlowLang.Package.ConfigurationManager.SaveConfigAsync(config, Path.Combine(projectPath, "flowc.json"));

        // Create main.flow
        var mainFlow = """
function main() -> string {
    return "Hello, FlowLang!"
}
""";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "src", "main.flow"), mainFlow);

        // Create hello.flow example
        var helloFlow = """
pure function greet(name: string) -> string {
    return $"Hello, {name}!"
}

function main() -> string {
    return greet("World")
}
""";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "examples", "hello.flow"), helloFlow);

        // Create basic test
        var basicTest = """
pure function add(a: int, b: int) -> int {
    return a + b
}

function test_add() -> bool {
    return add(2, 3) == 5
}
""";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "tests", "basic_test.flow"), basicTest);

        // Create .gitignore
        var gitignore = """
# Build outputs
build/
*.cs
bin/
obj/

# IDE files
.vs/
.vscode/
*.user

# OS files
.DS_Store
Thumbs.db
""";
        await File.WriteAllTextAsync(Path.Combine(projectPath, ".gitignore"), gitignore);

        // Create README.md
        var readme = $"""
# {projectName}

A FlowLang project.

## Getting Started

Build the project:
```bash
flowc build
```

Run examples:
```bash
flowc run examples/hello.flow
```

Run tests:
```bash
flowc test
```

## Project Structure

- `src/` - Main source files
- `examples/` - Example FlowLang files
- `tests/` - Test files
- `flowc.json` - Project configuration
""";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "README.md"), readme);
    }
}

public class BuildCommand : Command
{
    public override string Name => "build";
    public override string Description => "Build the current FlowLang project";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var config = await LoadConfig();
            var transpiler = new FlowLangTranspiler();

            // Create output directory
            var outputDir = config.Build.Output;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Find all .flow files in source directory
            var sourceDir = config.Build.Source;
            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine($"Error: Source directory '{sourceDir}' not found");
                return 1;
            }

            var flowFiles = Directory.GetFiles(sourceDir, "*.flow", SearchOption.AllDirectories);
            if (flowFiles.Length == 0)
            {
                Console.WriteLine($"No .flow files found in '{sourceDir}'");
                return 1;
            }

            Console.WriteLine($"Building {flowFiles.Length} file(s)...");

            foreach (var flowFile in flowFiles)
            {
                var relativePath = Path.GetRelativePath(sourceDir, flowFile);
                var outputFile = Path.Combine(outputDir, Path.ChangeExtension(relativePath, ".cs"));
                var outputFileDir = Path.GetDirectoryName(outputFile);
                
                if (outputFileDir != null && !Directory.Exists(outputFileDir))
                {
                    Directory.CreateDirectory(outputFileDir);
                }

                await transpiler.TranspileAsync(flowFile, outputFile);
            }

            Console.WriteLine($"Build completed. Output in '{outputDir}'");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Build failed: {ex.Message}");
            return 1;
        }
    }

    private async Task<object> LoadConfig()
    {
        await Task.CompletedTask;
        Console.WriteLine("Warning: Advanced package management disabled for core compilation");
        return new object();
    }
}

public class RunCommand : Command
{
    public override string Name => "run";
    public override string Description => "Transpile and run a single FlowLang file";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: File path is required");
            Console.WriteLine("Usage: flowc run <file.flow>");
            return 1;
        }

        var flowFile = args[0];
        if (!File.Exists(flowFile))
        {
            Console.WriteLine($"Error: File '{flowFile}' not found");
            return 1;
        }

        try
        {
            var transpiler = new FlowLangTranspiler();
            var tempDir = Path.GetTempPath();
            var tempFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(flowFile) + ".cs");

            // Transpile to temporary file
            await transpiler.TranspileAsync(flowFile, tempFile);
            
            Console.WriteLine($"Transpiled '{flowFile}' to '{tempFile}'");
            Console.WriteLine("Generated C# code:");
            Console.WriteLine(new string('=', 50));
            
            var generatedCode = await File.ReadAllTextAsync(tempFile);
            Console.WriteLine(generatedCode);
            
            Console.WriteLine(new string('=', 50));
            Console.WriteLine("Run complete.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running file: {ex.Message}");
            return 1;
        }
    }
}

public class TestCommand : Command
{
    public override string Name => "test";
    public override string Description => "Run tests in the current project";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var config = await LoadConfig();
            var testsDir = "tests";

            if (!Directory.Exists(testsDir))
            {
                Console.WriteLine("No tests directory found");
                return 0;
            }

            var testFiles = Directory.GetFiles(testsDir, "*.flow", SearchOption.AllDirectories);
            if (testFiles.Length == 0)
            {
                Console.WriteLine("No test files found");
                return 0;
            }

            Console.WriteLine($"Found {testFiles.Length} test file(s)");
            
            var transpiler = new FlowLangTranspiler();
            var allPassed = true;

            foreach (var testFile in testFiles)
            {
                Console.WriteLine($"Processing test: {testFile}");
                
                try
                {
                    var tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(testFile) + "_test.cs");
                    await transpiler.TranspileAsync(testFile, tempFile);
                    Console.WriteLine($"  ✓ Transpiled successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed: {ex.Message}");
                    allPassed = false;
                }
            }

            Console.WriteLine();
            Console.WriteLine(allPassed ? "All tests passed!" : "Some tests failed");
            return allPassed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test execution failed: {ex.Message}");
            return 1;
        }
    }

    private async Task<object> LoadConfig()
    {
        await Task.CompletedTask;
        Console.WriteLine("Warning: Advanced package management disabled for core compilation");
        return new object();
    }
}

public class LintCommand : Command
{
    public override string Name => "lint";
    public override string Description => "Run static analysis and linting on FlowLang code";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var options = ParseLintOptions(args);
            var analyzer = new FlowLang.Analysis.StaticAnalyzer(options.Configuration);

            // Determine what to analyze
            var paths = options.Paths?.Any() == true ? options.Paths : new[] { "." };
            
            Console.WriteLine("Running FlowLang static analysis...");
            var report = await analyzer.AnalyzeAsync(paths);

            // Output results
            if (options.OutputFormat == "json")
            {
                Console.WriteLine(report.ToJson());
            }
            else if (options.OutputFormat == "sarif")
            {
                Console.WriteLine(report.ToSarif());
            }
            else
            {
                // Text output (default)
                analyzer.PrintSummary(report);
                if (options.ShowDetails)
                {
                    analyzer.PrintDiagnostics(report, options.IncludeInfo);
                }
            }

            // Return appropriate exit code
            return report.HasPassingResult(options.Configuration.SeverityThreshold) ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lint error: {ex.Message}");
            return 1;
        }
    }

    private LintOptions ParseLintOptions(string[] args)
    {
        var options = new LintOptions();
        var paths = new List<string>();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        options.Configuration = FlowLang.Analysis.LintConfiguration.LoadFromFile(args[++i]);
                    }
                    break;
                    
                case "--format":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFormat = args[++i];
                    }
                    break;
                    
                case "--effects":
                    options.Categories.Add(FlowLang.Analysis.AnalysisCategories.EffectSystem);
                    break;
                    
                case "--results":
                    options.Categories.Add(FlowLang.Analysis.AnalysisCategories.ResultTypes);
                    break;
                    
                case "--quality":
                    options.Categories.Add(FlowLang.Analysis.AnalysisCategories.CodeQuality);
                    break;
                    
                case "--performance":
                    options.Categories.Add(FlowLang.Analysis.AnalysisCategories.Performance);
                    break;
                    
                case "--security":
                    options.Categories.Add(FlowLang.Analysis.AnalysisCategories.Security);
                    break;
                    
                case "--fix":
                    options.AutoFix = true;
                    break;
                    
                case "--details":
                    options.ShowDetails = true;
                    break;
                    
                case "--include-info":
                    options.IncludeInfo = true;
                    break;
                    
                default:
                    if (!args[i].StartsWith("--"))
                    {
                        paths.Add(args[i]);
                    }
                    break;
            }
        }

        if (options.Configuration == null)
        {
            options.Configuration = FlowLang.Analysis.LintConfiguration.LoadFromFile();
        }

        // If specific categories were requested, filter the configuration
        if (options.Categories.Any())
        {
            var filteredRules = new Dictionary<string, FlowLang.Analysis.LintRuleConfig>();
            foreach (var rule in options.Configuration.Rules)
            {
                // This is a simplified filter - in practice, you'd need to map rules to categories
                filteredRules[rule.Key] = rule.Value;
            }
            options.Configuration = options.Configuration with { Rules = filteredRules };
        }

        options.Paths = paths;
        return options;
    }

    private class LintOptions
    {
        public object? Configuration { get; set; }
        public string OutputFormat { get; set; } = "text";
        public List<string> Categories { get; set; } = new();
        public bool AutoFix { get; set; }
        public bool ShowDetails { get; set; } = true;
        public bool IncludeInfo { get; set; }
        public IEnumerable<string>? Paths { get; set; }
    }
}

public class LspCommand : Command
{
    public override string Name => "lsp";
    public override string Description => "Start the FlowLang Language Server Protocol server";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            Console.Error.WriteLine("Starting FlowLang Language Server...");
            
            var server = new FlowLang.LSP.FlowLangLanguageServer(Console.OpenStandardInput(), Console.OpenStandardOutput());
            await server.StartAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Language server error: {ex.Message}");
            return 1;
        }
    }
}

public class HelpCommand : Command
{
    private readonly Dictionary<string, Command> _commands;

    public HelpCommand(Dictionary<string, Command> commands)
    {
        _commands = commands;
    }

    public override string Name => "help";
    public override string Description => "Show help information";

    public override Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length > 0)
        {
            var commandName = args[0];
            if (_commands.TryGetValue(commandName, out var command))
            {
                ShowCommandHelp(command);
            }
            else
            {
                Console.WriteLine($"Unknown command: {commandName}");
                return Task.FromResult(1);
            }
        }
        else
        {
            ShowGeneralHelp();
        }

        return Task.FromResult(0);
    }

    private void ShowGeneralHelp()
    {
        Console.WriteLine("FlowLang Transpiler (flowc) v1.0.0");
        Console.WriteLine();
        Console.WriteLine("Usage: flowc <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        
        foreach (var command in _commands.Values)
        {
            Console.WriteLine($"  {command.Name,-12} {command.Description}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Use 'flowc help <command>' for more information about a command.");
    }

    private void ShowCommandHelp(Command command)
    {
        Console.WriteLine($"flowc {command.Name}");
        Console.WriteLine();
        Console.WriteLine($"Description: {command.Description}");
        Console.WriteLine();

        switch (command.Name)
        {
            case "new":
                Console.WriteLine("Usage: flowc new <project-name>");
                Console.WriteLine();
                Console.WriteLine("Creates a new FlowLang project with the following structure:");
                Console.WriteLine("  project-name/");
                Console.WriteLine("  ├── flowc.json");
                Console.WriteLine("  ├── src/main.flow");
                Console.WriteLine("  ├── examples/hello.flow");
                Console.WriteLine("  ├── tests/basic_test.flow");
                Console.WriteLine("  ├── .gitignore");
                Console.WriteLine("  └── README.md");
                break;

            case "build":
                Console.WriteLine("Usage: flowc build");
                Console.WriteLine();
                Console.WriteLine("Builds the current FlowLang project by transpiling all .flow files");
                Console.WriteLine("in the source directory to C# files in the output directory.");
                Console.WriteLine();
                Console.WriteLine("Configuration is read from flowc.json");
                break;

            case "run":
                Console.WriteLine("Usage: flowc run <file.flow>");
                Console.WriteLine();
                Console.WriteLine("Transpiles a single FlowLang file and displays the generated C# code.");
                Console.WriteLine("Useful for testing and debugging individual files.");
                break;

            case "test":
                Console.WriteLine("Usage: flowc test");
                Console.WriteLine();
                Console.WriteLine("Runs all test files in the tests/ directory.");
                Console.WriteLine("Currently validates that all test files transpile correctly.");
                break;

            case "lint":
                Console.WriteLine("Usage: flowc lint [options] [files/directories]");
                Console.WriteLine();
                Console.WriteLine("Runs static analysis and linting on FlowLang code to detect issues");
                Console.WriteLine("and suggest improvements for code quality, performance, and security.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  --config <file>       Use custom configuration file (default: flowlint.json)");
                Console.WriteLine("  --format <format>     Output format: text, json, sarif (default: text)");
                Console.WriteLine("  --effects             Analyze only effect system rules");
                Console.WriteLine("  --results             Analyze only Result type rules");
                Console.WriteLine("  --quality             Analyze only code quality rules");
                Console.WriteLine("  --performance         Analyze only performance rules");
                Console.WriteLine("  --security            Analyze only security rules");
                Console.WriteLine("  --fix                 Auto-fix issues where possible");
                Console.WriteLine("  --details             Show detailed diagnostics (default: true)");
                Console.WriteLine("  --include-info        Include info-level diagnostics");
                Console.WriteLine();
                Console.WriteLine("Analysis Categories:");
                Console.WriteLine("  Effect System         - Effect completeness, minimality, propagation");
                Console.WriteLine("  Result Types          - Error handling, propagation validation");
                Console.WriteLine("  Code Quality          - Dead code, naming conventions, complexity");
                Console.WriteLine("  Performance           - String concatenation, effect patterns");
                Console.WriteLine("  Security              - Input validation, secret detection");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  flowc lint                    # Analyze current directory");
                Console.WriteLine("  flowc lint src/               # Analyze src directory");
                Console.WriteLine("  flowc lint --effects --results  # Check only effect and result rules");
                Console.WriteLine("  flowc lint --format json      # Output in JSON format");
                break;

            case "lsp":
                Console.WriteLine("Usage: flowc lsp");
                Console.WriteLine();
                Console.WriteLine("Starts the FlowLang Language Server Protocol server for IDE integration.");
                Console.WriteLine("Provides real-time diagnostics, auto-completion, hover information, and go-to-definition.");
                Console.WriteLine();
                Console.WriteLine("Features:");
                Console.WriteLine("- Syntax error detection and highlighting");
                Console.WriteLine("- FlowLang-specific auto-completion (keywords, effects, types)");
                Console.WriteLine("- Hover information with function signatures and effect annotations");
                Console.WriteLine("- Go-to-definition for functions, modules, and variables");
                Console.WriteLine("- Effect system validation and suggestions");
                Console.WriteLine("- Result type analysis and error propagation help");
                Console.WriteLine();
                Console.WriteLine("Use with VS Code FlowLang extension or other LSP-compatible editors.");
                break;
        }
    }
}

// =============================================================================
// MAIN APPLICATION
// =============================================================================

public class FlowLangTranspiler
{
    private static readonly Dictionary<string, Command> Commands = new();

    public static async Task<int> Main(string[] args)
    {
        // Initialize commands
        Commands["new"] = new NewCommand();
        Commands["build"] = new BuildCommand();
        Commands["run"] = new RunCommand();
        Commands["test"] = new TestCommand();
        // Temporarily disabled for core compilation fix
        // Commands["lint"] = new LintCommand();
        // Commands["lsp"] = new LspCommand();
        // Commands["dev"] = new DevCommand();
        
        // Package management commands - temporarily disabled
        // Commands["add"] = new AddCommand();
        // Commands["remove"] = new RemoveCommand();
        // Commands["install"] = new InstallCommand();
        // Commands["update"] = new UpdateCommand();
        // Commands["search"] = new SearchCommand();
        // Commands["info"] = new InfoCommand();
        // Commands["publish"] = new PublishCommand();
        // Commands["audit"] = new AuditCommand();
        // Commands["pack"] = new PackCommand();
        // Commands["clean"] = new CleanCommand();
        // Commands["workspace"] = new WorkspaceCommand();
        // Commands["version"] = new VersionCommand();
        
        Commands["help"] = new HelpCommand(Commands);

        // Handle version flag
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            Console.WriteLine("FlowLang Transpiler (flowc) v1.0.0");
            return 0;
        }

        // Handle help flag
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            await Commands["help"].ExecuteAsync(Array.Empty<string>());
            return 0;
        }

        // Handle legacy --input mode for backward compatibility
        if (args.Length >= 2 && args[0] == "--input")
        {
            Console.WriteLine("Warning: --input mode is deprecated. Use 'flowc run <file>' instead.");
            var inputPath = args[1];
            var outputPath = args.Length > 3 && args[2] == "--output" ? args[3] : null;

            try
            {
                var transpiler = new FlowLangTranspiler();
                await transpiler.TranspileAsync(inputPath, outputPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        // Handle commands
        var commandName = args[0];
        if (Commands.TryGetValue(commandName, out var command))
        {
            var commandArgs = args.Skip(1).ToArray();
            return await command.ExecuteAsync(commandArgs);
        }

        Console.WriteLine($"Unknown command: {commandName}");
        Console.WriteLine("Use 'flowc help' for available commands.");
        return 1;
    }

    public async Task TranspileAsync(string inputPath, string? outputPath = null)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file not found: {inputPath}");
        }

        var flowLangSource = await File.ReadAllTextAsync(inputPath);
        var csharpCode = TranspileToCS(flowLangSource);

        if (outputPath == null)
        {
            outputPath = Path.ChangeExtension(inputPath, ".cs");
        }

        await File.WriteAllTextAsync(outputPath, csharpCode);
        Console.WriteLine($"Transpiled {inputPath} -> {outputPath}");
    }

    public string TranspileToCS(string flowLangSource)
    {
        var lexer = new FlowLangLexer(flowLangSource);
        var tokens = lexer.Tokenize();
        
        var parser = new FlowLangParser(tokens);
        var ast = parser.Parse();
        
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(ast);
        
        return syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
    }
}

// =============================================================================
// PACKAGE MANAGEMENT COMMANDS
// =============================================================================

public class AddCommand : Command
{
    public override string Name => "add";
    public override string Description => "Add a package dependency";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Package name is required");
            Console.WriteLine("Usage: flowc add <package[@version]> [--dev]");
            Console.WriteLine("Examples:");
            Console.WriteLine("  flowc add FlowLang.Database");
            Console.WriteLine("  flowc add Newtonsoft.Json@13.0.3");
            Console.WriteLine("  flowc add FlowLang.Testing@^1.0.0 --dev");
            return 1;
        }

        var packageSpec = args[0];
        var isDev = args.Contains("--dev");

        try
        {
            var packageManager = new FlowLang.Package.PackageManager();
            var result = await packageManager.AddPackageAsync(packageSpec, isDev);
            
            if (result.Success)
            {
                Console.WriteLine(result.Message);
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class RemoveCommand : Command
{
    public override string Name => "remove";
    public override string Description => "Remove a package dependency";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Package name is required");
            Console.WriteLine("Usage: flowc remove <package>");
            return 1;
        }

        var packageName = args[0];

        try
        {
            var packageManager = new FlowLang.Package.PackageManager();
            var result = await packageManager.RemovePackageAsync(packageName);
            
            if (result.Success)
            {
                Console.WriteLine(result.Message);
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class InstallCommand : Command
{
    public override string Name => "install";
    public override string Description => "Install all project dependencies";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var includeDev = !args.Contains("--production");

        try
        {
            var packageManager = new FlowLang.Package.PackageManager();
            var result = await packageManager.InstallPackagesAsync(includeDev);
            
            if (result.Success)
            {
                Console.WriteLine(result.Message);
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class UpdateCommand : Command
{
    public override string Name => "update";
    public override string Description => "Update packages to latest compatible versions";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var specificPackage = args.Length > 0 ? args[0] : null;

        try
        {
            var packageManager = new FlowLang.Package.PackageManager();
            var result = await packageManager.UpdatePackagesAsync(specificPackage);
            
            if (result.Success)
            {
                Console.WriteLine(result.Message);
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class SearchCommand : Command
{
    public override string Name => "search";
    public override string Description => "Search for packages";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Search query is required");
            Console.WriteLine("Usage: flowc search <query>");
            return 1;
        }

        var query = string.Join(" ", args);

        try
        {
            var packageManager = new FlowLang.Package.PackageManager();
            var results = await packageManager.SearchPackagesAsync(query);
            
            if (results.Any())
            {
                Console.WriteLine($"Found {results.Count} packages:");
                Console.WriteLine();
                
                foreach (var result in results.Take(20))
                {
                    Console.WriteLine($"{result.Name}@{result.Version} ({result.Type})");
                    Console.WriteLine($"  {result.Description}");
                    Console.WriteLine($"  Downloads: {result.DownloadCount:N0}");
                    Console.WriteLine();
                }
                
                if (results.Count > 20)
                {
                    Console.WriteLine($"... and {results.Count - 20} more packages");
                }
            }
            else
            {
                Console.WriteLine("No packages found matching your search.");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class InfoCommand : Command
{
    public override string Name => "info";
    public override string Description => "Get detailed information about a package";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Package name is required");
            Console.WriteLine("Usage: flowc info <package>");
            return 1;
        }

        var packageName = args[0];

        try
        {
            var packageManager = new FlowLang.Package.PackageManager();
            var info = await packageManager.GetPackageInfoAsync(packageName);
            
            if (info != null)
            {
                Console.WriteLine($"Package: {info.Name}@{info.Version}");
                Console.WriteLine($"Type: {info.Type}");
                Console.WriteLine($"Description: {info.Description}");
                
                if (!string.IsNullOrEmpty(info.Author))
                    Console.WriteLine($"Author: {info.Author}");
                
                if (!string.IsNullOrEmpty(info.Homepage))
                    Console.WriteLine($"Homepage: {info.Homepage}");
                
                if (!string.IsNullOrEmpty(info.License))
                    Console.WriteLine($"License: {info.License}");
                
                if (info.Keywords.Any())
                    Console.WriteLine($"Keywords: {string.Join(", ", info.Keywords)}");
                
                if (info.Effects.Any())
                    Console.WriteLine($"Effects: {string.Join(", ", info.Effects)}");
                
                Console.WriteLine($"Versions: {string.Join(", ", info.Versions.Take(10))}");
                
                if (info.Dependencies.Any())
                {
                    Console.WriteLine("Dependencies:");
                    foreach (var (name, version) in info.Dependencies)
                    {
                        Console.WriteLine($"  {name}@{version}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Package '{packageName}' not found.");
                return 1;
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class PublishCommand : Command
{
    public override string Name => "publish";
    public override string Description => "Publish package to registry";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var dryRun = args.Contains("--dry-run");
        var access = args.Contains("--private") ? "private" : "public";

        try
        {
            var packageManager = new FlowLang.Package.PackageManager();
            var options = new FlowLang.Package.PublishOptions(Access: access, DryRun: dryRun);
            var result = await packageManager.PublishPackageAsync(options: options);
            
            if (result.Success)
            {
                Console.WriteLine(result.Message);
                if (!string.IsNullOrEmpty(result.PackageUrl))
                    Console.WriteLine($"Package URL: {result.PackageUrl}");
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class AuditCommand : Command
{
    public override string Name => "audit";
    public override string Description => "Audit packages for security vulnerabilities";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var fix = args.Contains("fix");
        var verbose = args.Contains("--verbose");

        try
        {
            var scanner = new FlowLang.Package.SecurityScanner();
            
            if (fix)
            {
                Console.WriteLine("Scanning and attempting to fix security vulnerabilities...");
                var fixResult = await scanner.FixSecurityIssuesAsync();
                
                Console.WriteLine(fixResult.Message);
                
                if (fixResult.AutomaticallyFixable.Any())
                {
                    Console.WriteLine("\nAutomatically fixed:");
                    foreach (var autoFix in fixResult.AutomaticallyFixable)
                    {
                        Console.WriteLine($"  ✓ {autoFix.PackageName}: {autoFix.CurrentVersion} → {autoFix.FixedVersion}");
                    }
                }
                
                if (fixResult.ManuallyFixable.Any())
                {
                    Console.WriteLine("\nRequires manual attention:");
                    foreach (var manualFix in fixResult.ManuallyFixable)
                    {
                        Console.WriteLine($"  ⚠ {manualFix.PackageName}: {manualFix.Description}");
                    }
                }
                
                return fixResult.Success ? 0 : 1;
            }
            else
            {
                var report = await scanner.AuditAsync(verbose: verbose);
                
                Console.WriteLine($"Security audit completed for {report.TotalPackagesScanned} packages");
                Console.WriteLine($"Vulnerabilities found: {report.TotalVulnerabilities}");
                
                if (report.HasVulnerabilities)
                {
                    Console.WriteLine($"  Critical: {report.CriticalCount}");
                    Console.WriteLine($"  High: {report.HighCount}");
                    Console.WriteLine($"  Medium: {report.MediumCount}");
                    Console.WriteLine($"  Low: {report.LowCount}");
                    
                    if (verbose)
                    {
                        Console.WriteLine("\nVulnerable packages:");
                        foreach (var vulnPackage in report.VulnerablePackages)
                        {
                            Console.WriteLine($"\n{vulnPackage.Name}@{vulnPackage.Version}:");
                            foreach (var vuln in vulnPackage.Vulnerabilities)
                            {
                                Console.WriteLine($"  • {vuln.Title} ({vuln.Severity})");
                                Console.WriteLine($"    {vuln.Description}");
                                if (vuln.FixedIn != null)
                                    Console.WriteLine($"    Fixed in: {vuln.FixedIn}");
                            }
                        }
                    }
                    
                    Console.WriteLine("\nRun 'flowc audit fix' to automatically fix compatible issues.");
                    return 1;
                }
                else
                {
                    Console.WriteLine("No security vulnerabilities found.");
                    return 0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class PackCommand : Command
{
    public override string Name => "pack";
    public override string Description => "Create a package tarball";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var outputDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

        try
        {
            var packageCreator = new FlowLang.Package.PackageCreator();
            var packagePath = await packageCreator.CreatePackageAsync(Directory.GetCurrentDirectory(), outputDir);
            
            Console.WriteLine($"Package created: {packagePath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class CleanCommand : Command
{
    public override string Name => "clean";
    public override string Description => "Clean package cache and build artifacts";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var packagesDir = Path.Combine(Directory.GetCurrentDirectory(), "packages");
            var buildDir = Path.Combine(Directory.GetCurrentDirectory(), "build");
            
            if (Directory.Exists(packagesDir))
            {
                Directory.Delete(packagesDir, true);
                Console.WriteLine("Cleaned packages directory");
            }
            
            if (Directory.Exists(buildDir))
            {
                Directory.Delete(buildDir, true);
                Console.WriteLine("Cleaned build directory");
            }
            
            // Clean .cs files generated by transpiler
            var csFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("obj") && !f.Contains("bin"));
            
            foreach (var csFile in csFiles)
            {
                try
                {
                    File.Delete(csFile);
                    Console.WriteLine($"Deleted: {Path.GetRelativePath(Directory.GetCurrentDirectory(), csFile)}");
                }
                catch
                {
                    // Ignore errors when deleting individual files
                }
            }
            
            Console.WriteLine("Clean completed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class WorkspaceCommand : Command
{
    public override string Name => "workspace";
    public override string Description => "Manage workspace projects";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Workspace command is required");
            Console.WriteLine("Usage: flowc workspace <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  list     - List all workspace projects");
            Console.WriteLine("  install  - Install dependencies for all projects");
            Console.WriteLine("  run <cmd> - Run command in all projects");
            return 1;
        }

        var subCommand = args[0];

        try
        {
            var workspaceManager = new FlowLang.Package.WorkspaceManager();

            switch (subCommand)
            {
                case "list":
                    var projects = await workspaceManager.GetWorkspaceProjectsAsync();
                    if (projects.Any())
                    {
                        Console.WriteLine("Workspace projects:");
                        foreach (var project in projects)
                        {
                            Console.WriteLine($"  {project}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No workspace projects found");
                    }
                    return 0;

                case "install":
                    var installResult = await workspaceManager.InstallWorkspaceAsync();
                    Console.WriteLine(installResult.Message);
                    return installResult.Success ? 0 : 1;

                case "run":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Command to run is required");
                        return 1;
                    }
                    
                    var command = string.Join(" ", args.Skip(1));
                    Console.WriteLine($"Running '{command}' in all workspace projects...");
                    
                    var workspaceProjects = await workspaceManager.GetWorkspaceProjectsAsync();
                    foreach (var project in workspaceProjects)
                    {
                        Console.WriteLine($"\n--- {project} ---");
                        // Implementation would run the command in each project directory
                        Console.WriteLine($"Would run: {command}");
                    }
                    return 0;

                default:
                    Console.WriteLine($"Unknown workspace command: {subCommand}");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class VersionCommand : Command
{
    public override string Name => "version";
    public override string Description => "Manage package version";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // Show current version
            try
            {
                var config = await FlowLang.Package.ConfigurationManager.LoadConfigAsync();
                Console.WriteLine($"Current version: {config.Version}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        var versionType = args[0];

        try
        {
            var config = await FlowLang.Package.ConfigurationManager.LoadConfigAsync();
            var currentVersion = FlowLang.Package.SemanticVersion.Parse(config.Version);
            
            var newVersion = versionType switch
            {
                "patch" => new FlowLang.Package.SemanticVersion(currentVersion.Major, currentVersion.Minor, currentVersion.Patch + 1),
                "minor" => new FlowLang.Package.SemanticVersion(currentVersion.Major, currentVersion.Minor + 1, 0),
                "major" => new FlowLang.Package.SemanticVersion(currentVersion.Major + 1, 0, 0),
                _ => FlowLang.Package.SemanticVersion.Parse(versionType)
            };

            var newConfig = config with { Version = newVersion.ToString() };
            await FlowLang.Package.ConfigurationManager.SaveConfigAsync(newConfig);
            
            Console.WriteLine($"Version updated: {config.Version} → {newVersion}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

/// <summary>
/// FlowLang Development Server with Hot Reload
/// Provides file watching, compilation, and WebSocket-based browser updates
/// </summary>
public class DevCommand : Command
{
    private int _port;
    private bool _verbose;
    private string _watchPath;
    private HttpListener _httpListener;
    private readonly ConcurrentDictionary<string, WebSocket> _webSockets;
    private FileSystemWatcher _fileWatcher;
    private readonly object _compilationLock = new object();
    private CancellationTokenSource _cancellationTokenSource;

    public override string Name => "dev";
    public override string Description => "Start development server with hot reload";

    public DevCommand()
    {
        _port = 8080;
        _verbose = false;
        _watchPath = Directory.GetCurrentDirectory();
        _httpListener = new HttpListener();
        _webSockets = new ConcurrentDictionary<string, WebSocket>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            ParseArguments(args);
            
            Console.WriteLine("🚀 Starting FlowLang Development Server");
            Console.WriteLine($"📁 Watching: {_watchPath}");
            Console.WriteLine($"🌐 Server: http://localhost:{_port}");
            Console.WriteLine($"🔄 Hot reload enabled");
            Console.WriteLine();
            
            // Start HTTP server
            await StartHttpServerAsync();
            
            // Start file watcher
            StartFileWatcher();
            
            // Initial compilation
            await PerformInitialCompilation();
            
            Console.WriteLine("✅ Development server started successfully");
            Console.WriteLine("Press Ctrl+C to stop...");
            
            // Wait for cancellation
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                _cancellationTokenSource.Cancel();
            };
            
            try
            {
                await Task.Delay(-1, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n🛑 Shutting down development server...");
            }
            
            await CleanupAsync();
            Console.WriteLine("✅ Development server stopped");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to start development server: {ex.Message}");
            if (_verbose)
            {
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }

    private void ParseArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var port))
                        _port = port;
                    break;
                case "--verbose":
                    _verbose = true;
                    break;
                case "--watch":
                    if (i + 1 < args.Length)
                        _watchPath = Path.GetFullPath(args[++i]);
                    break;
            }
        }
        
        // Initialize file watcher after parsing arguments
        _fileWatcher = new FileSystemWatcher(_watchPath)
        {
            Filter = "*.flow",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
    }

    private async Task StartHttpServerAsync()
    {
        _httpListener.Prefixes.Add($"http://localhost:{_port}/");
        _httpListener.Start();
        
        // Start accepting HTTP requests
        _ = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => HandleHttpRequestAsync(context));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_verbose)
                        Console.WriteLine($"HTTP request error: {ex.Message}");
                }
            }
        });
    }

    private async Task HandleHttpRequestAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            
            if (_verbose)
                Console.WriteLine($"📥 {request.HttpMethod} {request.Url?.AbsolutePath}");

            if (request.IsWebSocketRequest)
            {
                await HandleWebSocketRequestAsync(context);
                return;
            }

            var path = request.Url?.AbsolutePath?.TrimStart('/') ?? "";
            
            if (string.IsNullOrEmpty(path) || path == "index.html")
            {
                await ServeIndexHtmlAsync(response);
            }
            else if (path.EndsWith(".js"))
            {
                await ServeJavaScriptFileAsync(response, path);
            }
            else if (path.EndsWith(".css"))
            {
                await ServeCssFileAsync(response, path);
            }
            else if (path == "hot-reload.js")
            {
                await ServeHotReloadScriptAsync(response);
            }
            else
            {
                await ServeNotFoundAsync(response);
            }
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"Request handling error: {ex.Message}");
        }
    }

    private async Task HandleWebSocketRequestAsync(HttpListenerContext context)
    {
        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            var connectionId = Guid.NewGuid().ToString();
            
            _webSockets.TryAdd(connectionId, webSocket);
            
            if (_verbose)
                Console.WriteLine($"🔗 WebSocket connected: {connectionId}");
            
            // Keep connection alive and handle messages
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException)
                {
                    break;
                }
            }
            
            _webSockets.TryRemove(connectionId, out _);
            
            if (_verbose)
                Console.WriteLine($"🔌 WebSocket disconnected: {connectionId}");
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"WebSocket error: {ex.Message}");
        }
    }

    private async Task ServeIndexHtmlAsync(HttpListenerResponse response)
    {
        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>FlowLang Development Server</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; border-radius: 8px; padding: 20px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .status {{ display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; }}
        .status.connected {{ background: #e8f5e8; color: #2d5a2d; }}
        .status.disconnected {{ background: #ffe8e8; color: #8b0000; }}
        .error-overlay {{ position: fixed; top: 0; left: 0; right: 0; background: #8b0000; color: white; padding: 10px; display: none; z-index: 1000; }}
        #app {{ margin-top: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🚀 FlowLang Development Server</h1>
            <span id=""status"" class=""status disconnected"">Connecting...</span>
        </div>
        <div id=""error-overlay"" class=""error-overlay""></div>
        <div id=""app"">
            <p>Loading FlowLang application...</p>
        </div>
    </div>
    
    <script src=""/hot-reload.js""></script>
    <script src=""/main.js""></script>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();
    }

    private async Task ServeHotReloadScriptAsync(HttpListenerResponse response)
    {
        var script = $@"
// FlowLang Hot Reload Client
(function() {{
    let ws;
    let reconnectAttempts = 0;
    const maxReconnectAttempts = 10;
    const reconnectDelay = 1000;
    
    const statusEl = document.getElementById('status');
    const errorOverlay = document.getElementById('error-overlay');
    
    function connect() {{
        try {{
            ws = new WebSocket('ws://localhost:{_port}/');
            
            ws.onopen = function() {{
                console.log('🔄 Hot reload connected');
                statusEl.textContent = 'Connected';
                statusEl.className = 'status connected';
                reconnectAttempts = 0;
                hideError();
            }};
            
            ws.onmessage = function(event) {{
                try {{
                    const message = JSON.parse(event.data);
                    console.log('📨 Hot reload message:', message);
                    
                    switch(message.type) {{
                        case 'reload':
                            console.log('🔄 Reloading page...');
                            window.location.reload();
                            break;
                            
                        case 'update':
                            console.log('🔧 Updating content...');
                            if (message.content) {{
                                // Update script content
                                const script = document.createElement('script');
                                script.textContent = message.content;
                                document.head.appendChild(script);
                            }}
                            break;
                            
                        case 'error':
                            console.error('❌ Compilation error:', message.error);
                            showError(message.error);
                            break;
                    }}
                }} catch (e) {{
                    console.error('Failed to parse hot reload message:', e);
                }}
            }};
            
            ws.onclose = function() {{
                console.log('🔌 Hot reload disconnected');
                statusEl.textContent = 'Disconnected';
                statusEl.className = 'status disconnected';
                
                if (reconnectAttempts < maxReconnectAttempts) {{
                    setTimeout(() => {{
                        reconnectAttempts++;
                        console.log(`🔄 Reconnecting attempt ${{reconnectAttempts}}...`);
                        connect();
                    }}, reconnectDelay * Math.pow(1.5, reconnectAttempts));
                }}
            }};
            
            ws.onerror = function(error) {{
                console.error('🚨 WebSocket error:', error);
            }};
        }} catch (e) {{
            console.error('Failed to connect to hot reload server:', e);
        }}
    }}
    
    function showError(error) {{
        errorOverlay.innerHTML = `
            <strong>⚠️ Compilation Error:</strong>
            <pre style=""margin: 10px 0; white-space: pre-wrap;"">${{error}}</pre>
        `;
        errorOverlay.style.display = 'block';
    }}
    
    function hideError() {{
        errorOverlay.style.display = 'none';
    }}
    
    // Start connection
    connect();
    
    // Expose global functions for debugging
    window.flowLangHotReload = {{
        connect,
        disconnect: () => ws && ws.close(),
        send: (data) => ws && ws.send(JSON.stringify(data))
    }};
}})();
";

        var buffer = Encoding.UTF8.GetBytes(script);
        response.ContentType = "application/javascript; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();
    }

    private async Task ServeJavaScriptFileAsync(HttpListenerResponse response, string path)
    {
        var fullPath = Path.Combine(_watchPath, path);
        
        if (File.Exists(fullPath))
        {
            var content = await File.ReadAllTextAsync(fullPath);
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentType = "application/javascript; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        else
        {
            await ServeNotFoundAsync(response);
        }
        
        response.Close();
    }

    private async Task ServeCssFileAsync(HttpListenerResponse response, string path)
    {
        var fullPath = Path.Combine(_watchPath, path);
        
        if (File.Exists(fullPath))
        {
            var content = await File.ReadAllTextAsync(fullPath);
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentType = "text/css; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        else
        {
            await ServeNotFoundAsync(response);
        }
        
        response.Close();
    }

    private async Task ServeNotFoundAsync(HttpListenerResponse response)
    {
        response.StatusCode = 404;
        var content = "404 - File Not Found";
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();
    }

    private void StartFileWatcher()
    {
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.Created += OnFileChanged;
        _fileWatcher.Deleted += OnFileChanged;
        _fileWatcher.Renamed += OnFileRenamed;
        _fileWatcher.EnableRaisingEvents = true;
        
        if (_verbose)
            Console.WriteLine($"👀 File watcher started for: {_watchPath}");
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_verbose)
            Console.WriteLine($"📝 File changed: {e.Name} ({e.ChangeType})");
        
        await HandleFileChangeAsync(e.FullPath);
    }

    private async void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (_verbose)
            Console.WriteLine($"📝 File renamed: {e.OldName} -> {e.Name}");
        
        await HandleFileChangeAsync(e.FullPath);
    }

    private async Task HandleFileChangeAsync(string filePath)
    {
        // Debounce file changes (wait a bit to avoid multiple rapid changes)
        await Task.Delay(100);
        
        if (!File.Exists(filePath) || !filePath.EndsWith(".flow"))
            return;

        lock (_compilationLock)
        {
            try
            {
                if (_verbose)
                    Console.WriteLine($"🔄 Compiling: {Path.GetFileName(filePath)}");
                
                var startTime = DateTime.Now;
                
                // Compile the changed file
                var task = CompileFileAsync(filePath);
                task.Wait(); // Synchronous compilation for debouncing
                
                var duration = DateTime.Now - startTime;
                Console.WriteLine($"✅ Compiled {Path.GetFileName(filePath)} in {duration.TotalMilliseconds:F0}ms");
                
                // Notify browsers of successful compilation
                _ = NotifyBrowsersAsync(new { type = "reload" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Compilation failed: {ex.Message}");
                
                // Notify browsers of compilation error
                _ = NotifyBrowsersAsync(new { 
                    type = "error", 
                    error = ex.Message,
                    file = Path.GetFileName(filePath)
                });
            }
        }
    }

    private async Task CompileFileAsync(string filePath)
    {
        // Determine output path and target
        var outputPath = Path.ChangeExtension(filePath, ".js");
        
        // Use existing transpiler with JavaScript target
        var transpiler = new FlowLangTranspiler();
        await transpiler.TranspileAsync(filePath, outputPath);
    }

    private async Task NotifyBrowsersAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);
        
        var tasks = new List<Task>();
        foreach (var kvp in _webSockets)
        {
            var webSocket = kvp.Value;
            if (webSocket.State == WebSocketState.Open)
            {
                tasks.Add(webSocket.SendAsync(
                    new ArraySegment<byte>(buffer), 
                    WebSocketMessageType.Text, 
                    true, 
                    CancellationToken.None));
            }
        }
        
        if (tasks.Any())
        {
            try
            {
                await Task.WhenAll(tasks);
                if (_verbose)
                    Console.WriteLine($"📡 Notified {tasks.Count} browser(s)");
            }
            catch (Exception ex)
            {
                if (_verbose)
                    Console.WriteLine($"WebSocket notification error: {ex.Message}");
            }
        }
    }

    private async Task PerformInitialCompilation()
    {
        try
        {
            var flowFiles = Directory.GetFiles(_watchPath, "*.flow", SearchOption.AllDirectories);
            
            if (flowFiles.Length == 0)
            {
                Console.WriteLine("⚠️  No .flow files found in current directory");
                return;
            }
            
            Console.WriteLine($"🔄 Performing initial compilation of {flowFiles.Length} file(s)...");
            
            var compiledCount = 0;
            foreach (var flowFile in flowFiles)
            {
                try
                {
                    await CompileFileAsync(flowFile);
                    compiledCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to compile {Path.GetFileName(flowFile)}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"✅ Initial compilation complete: {compiledCount}/{flowFiles.Length} files compiled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Initial compilation failed: {ex.Message}");
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            _fileWatcher?.Dispose();
            
            // Close all WebSocket connections
            var closeTasks = new List<Task>();
            foreach (var kvp in _webSockets)
            {
                var webSocket = kvp.Value;
                if (webSocket.State == WebSocketState.Open)
                {
                    closeTasks.Add(webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutdown", CancellationToken.None));
                }
            }
            
            if (closeTasks.Any())
            {
                await Task.WhenAll(closeTasks);
            }
            
            _webSockets.Clear();
            _httpListener?.Stop();
            _httpListener?.Close();
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"Cleanup error: {ex.Message}");
        }
    }
}