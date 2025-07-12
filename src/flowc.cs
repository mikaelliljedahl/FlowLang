// Using packages from transpiler.csproj

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
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
        var config = new FlowcConfig(
            Name: projectName,
            Description: $"A FlowLang project: {projectName}"
        );
        var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(projectPath, "flowc.json"), configJson);

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

    private async Task<FlowcConfig> LoadConfig()
    {
        var configPath = "flowc.json";
        if (!File.Exists(configPath))
        {
            Console.WriteLine("Warning: No flowc.json found, using default configuration");
            return new FlowcConfig();
        }

        var configJson = await File.ReadAllTextAsync(configPath);
        return JsonSerializer.Deserialize<FlowcConfig>(configJson) ?? new FlowcConfig();
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

    private async Task<FlowcConfig> LoadConfig()
    {
        var configPath = "flowc.json";
        if (!File.Exists(configPath))
        {
            return new FlowcConfig();
        }

        var configJson = await File.ReadAllTextAsync(configPath);
        return JsonSerializer.Deserialize<FlowcConfig>(configJson) ?? new FlowcConfig();
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