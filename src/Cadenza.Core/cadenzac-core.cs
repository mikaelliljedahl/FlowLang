// Cadenza Core Compiler - Pure Transpilation Only
// Extracted from cadenzac.cs for simplicity and reliability

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

// =============================================================================
// TOKEN DEFINITIONS
// =============================================================================

// Token types for Cadenza
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
    Some,
    None,
    Match,
    
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
    List,
    Option,
    
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
    LeftBraceCurly,  // { for import lists
    RightBraceCurly, // } for import lists
    
    // UI Component Keywords (Phase 4)
    Component,
    State,
    Events,
    Render,
    OnMount,
    EventHandler,
    AppState,
    Action,
    Updates,
    ApiClient,
    Endpoint,
    For,
    In,
    Where,
    
    // Modulus operator
    Modulo,   // %
    
    // Specification block tokens
    SpecStart,  // /*spec
    SpecEnd,    // spec*/
    
    EOF
}

public record Token(TokenType Type, string Lexeme, object? Literal, int Line, int Column);

// =============================================================================
// AST DEFINITIONS
// =============================================================================

// AST Node definitions
public abstract record ASTNode;
public record Program(List<ASTNode> Statements) : ASTNode;

// Module system AST nodes
public record ModuleDeclaration(string Name, List<ASTNode> Body, List<string>? Exports = null, SpecificationBlock? Specification = null) : ASTNode;
public record ImportStatement(string ModuleName, List<string>? SpecificImports = null, bool IsWildcard = false) : ASTNode;
public record ExportStatement(List<string> ExportedNames) : ASTNode;

// Specification block AST node
public record SpecificationBlock(
    string Intent,
    List<string>? Rules = null,
    List<string>? Postconditions = null,
    string? SourceDoc = null
) : ASTNode;

// Function AST nodes
public record FunctionDeclaration(
    string Name, 
    List<Parameter> Parameters, 
    string? ReturnType, 
    List<ASTNode> Body, 
    bool IsPure = false,
    List<string>? Effects = null,
    bool IsExported = false,
    SpecificationBlock? Specification = null
) : ASTNode;

public record Parameter(string Name, string Type);

// Statement AST nodes
public record ReturnStatement(ASTNode? Expression) : ASTNode;
public record IfStatement(ASTNode Condition, List<ASTNode> ThenBody, List<ASTNode>? ElseBody = null) : ASTNode;
public record LetStatement(string Name, string? Type, ASTNode Expression) : ASTNode;
public record GuardStatement(ASTNode Condition, List<ASTNode>? ElseBody = null) : ASTNode;

// Expression AST nodes
public record BinaryExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record UnaryExpression(string Operator, ASTNode Operand) : ASTNode;
public record CallExpression(string Name, List<ASTNode> Arguments) : ASTNode;
public record MemberAccessExpression(ASTNode Object, string Member) : ASTNode;
public record MethodCallExpression(ASTNode Object, string Method, List<ASTNode> Arguments) : ASTNode;
public record Identifier(string Name) : ASTNode;
public record NumberLiteral(int Value) : ASTNode;
public record StringLiteral(string Value) : ASTNode;
public record StringInterpolation(List<ASTNode> Parts) : ASTNode;
public record BooleanLiteral(bool Value) : ASTNode;
public record ResultExpression(string Type, ASTNode Value) : ASTNode; // Ok(...) or Error(...)
public record ErrorPropagation(ASTNode Expression) : ASTNode; // expr?
public record TernaryExpression(ASTNode Condition, ASTNode ThenExpr, ASTNode ElseExpr) : ASTNode;
public record LogicalExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record ComparisonExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record ArithmeticExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record ListExpression(List<ASTNode> Elements) : ASTNode;
public record ListAccessExpression(ASTNode List, ASTNode Index) : ASTNode;
public record GenericType(string BaseType, List<string> TypeArguments) : ASTNode;
public record OptionExpression(string Type, ASTNode? Value) : ASTNode; // Some(value) or None
public record MatchExpression(ASTNode Value, List<MatchCase> Cases) : ASTNode;
public record MatchCase(string Pattern, string? Variable, List<ASTNode> Body) : ASTNode;

// UI Component AST nodes (Phase 4)
public record ComponentDeclaration(
    string Name,
    List<Parameter> Parameters,
    List<string>? Effects,
    List<StateDeclaration>? State,
    List<EventHandler>? Events,
    ASTNode? OnMount,
    ASTNode RenderBlock
) : ASTNode;

public record StateDeclaration(string Name, string Type, ASTNode? InitialValue = null) : ASTNode;
public record EventHandler(string Name, List<Parameter> Parameters, List<string>? Effects, List<ASTNode> Body) : ASTNode;
public record UIElement(string Tag, List<UIAttribute> Attributes, List<ASTNode> Children) : ASTNode;
public record UIAttribute(string Name, ASTNode Value) : ASTNode;
public record ComponentInstance(string Name, List<UIAttribute> Props, List<ASTNode>? Children = null) : ASTNode;

public record ConditionalRender(ASTNode Condition, List<ASTNode> ThenBody, List<ASTNode>? ElseBody = null) : ASTNode;
public record IterativeRender(string Variable, ASTNode Collection, ASTNode? Condition, List<ASTNode> Body) : ASTNode;

public record AppStateDeclaration(
    string Name,
    List<StateDeclaration> StateVariables,
    List<StateAction> Actions,
    List<string>? Effects
) : ASTNode;

public record StateAction(string Name, List<Parameter> Parameters, List<string>? Effects, List<ASTNode> Body) : ASTNode;

public record ApiClientDeclaration(
    string Name,
    string BaseUrl,
    List<ApiMethod> Methods
) : ASTNode;

public record ApiMethod(string Name, List<Parameter> Parameters, string ReturnType, List<string>? Effects) : ASTNode;

// =============================================================================
// LEXER
// =============================================================================

public class CadenzaLexer
{
    private readonly string _source;
    private readonly List<Token> _tokens = new();
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        {"function", TokenType.Function},
        {"return", TokenType.Return},
        {"if", TokenType.If},
        {"else", TokenType.Else},
        {"effects", TokenType.Effects},
        {"pure", TokenType.Pure},
        {"true", TokenType.Bool},
        {"false", TokenType.Bool},
        {"null", TokenType.Identifier}, // For now, handle null as an identifier
        {"uses", TokenType.Uses},
        {"Result", TokenType.Result},
        {"Ok", TokenType.Ok},
        {"Error", TokenType.Error},
        {"let", TokenType.Let},
        {"Some", TokenType.Some},
        {"None", TokenType.None},
        {"match", TokenType.Match},
        {"guard", TokenType.Guard},
        {"module", TokenType.Module},
        {"import", TokenType.Import},
        {"export", TokenType.Export},
        {"from", TokenType.From},
        {"component", TokenType.Component},
        {"state", TokenType.State},
        {"events", TokenType.Events},
        {"render", TokenType.Render},
        {"on_mount", TokenType.OnMount},
        {"event_handler", TokenType.EventHandler},
        {"app_state", TokenType.AppState},
        {"action", TokenType.Action},
        {"updates", TokenType.Updates},
        {"api_client", TokenType.ApiClient},
        {"endpoint", TokenType.Endpoint},
        {"for", TokenType.For},
        {"in", TokenType.In},
        {"where", TokenType.Where},
        {"Database", TokenType.Database},
        {"Network", TokenType.Network},
        {"Logging", TokenType.Logging},
        {"FileSystem", TokenType.FileSystem},
        {"Memory", TokenType.Memory},
        {"IO", TokenType.IO},
        {"int", TokenType.Int},
        {"string", TokenType.String_Type},
        {"bool", TokenType.Bool},
        {"List", TokenType.List},
        {"Option", TokenType.Option}
    };

    public CadenzaLexer(string source)
    {
        _source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF, "", null, _line, _column));
        return _tokens;
    }

    private bool IsAtEnd() => _current >= _source.Length;

    private void ScanToken()
    {
        char c = Advance();
        
        switch (c)
        {
            case ' ':
            case '\r':
            case '\t':
                _column++;
                break;
            case '\n':
                _line++;
                _column = 1;
                break;
            case '(':
                AddToken(TokenType.LeftParen);
                break;
            case ')':
                AddToken(TokenType.RightParen);
                break;
            case '{':
                AddToken(TokenType.LeftBrace);
                break;
            case '}':
                AddToken(TokenType.RightBrace);
                break;
            case '[':
                AddToken(TokenType.LeftBracket);
                break;
            case ']':
                AddToken(TokenType.RightBracket);
                break;
            case ',':
                AddToken(TokenType.Comma);
                break;
            case ';':
                AddToken(TokenType.Semicolon);
                break;
            case ':':
                AddToken(TokenType.Colon);
                break;
            case '+':
                AddToken(TokenType.Plus);
                break;
            case '*':
                AddToken(TokenType.Multiply);
                break;
            case '/':
                if (Match('/'))
                {
                    // Line comment
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else if (Match('*'))
                {
                    // Check for specification block comment
                    ScanSpecificationOrComment();
                }
                else
                {
                    AddToken(TokenType.Divide);
                }
                break;
            case '%':
                AddToken(TokenType.Modulo);
                break;
            case '?':
                AddToken(TokenType.Question);
                break;
            case '.':
                AddToken(TokenType.Dot);
                break;
            case '-':
                if (Match('>'))
                {
                    AddToken(TokenType.Arrow);
                }
                else
                {
                    AddToken(TokenType.Minus);
                }
                break;
            case '=':
                if (Match('='))
                {
                    AddToken(TokenType.Equal);
                }
                else
                {
                    AddToken(TokenType.Assign);
                }
                break;
            case '!':
                if (Match('='))
                {
                    AddToken(TokenType.NotEqual);
                }
                else
                {
                    AddToken(TokenType.Not);
                }
                break;
            case '<':
                if (Match('='))
                {
                    AddToken(TokenType.LessEqual);
                }
                else
                {
                    AddToken(TokenType.Less);
                }
                break;
            case '>':
                if (Match('='))
                {
                    AddToken(TokenType.GreaterEqual);
                }
                else
                {
                    AddToken(TokenType.Greater);
                }
                break;
            case '&':
                if (Match('&'))
                {
                    AddToken(TokenType.And);
                }
                else
                {
                    throw new Exception($"Unexpected character '&' at line {_line}, column {_column}");
                }
                break;
            case '|':
                if (Match('|'))
                {
                    AddToken(TokenType.Or);
                }
                else
                {
                    throw new Exception($"Unexpected character '|' at line {_line}, column {_column}");
                }
                break;
            case '"':
                StringLiteral();
                break;
            case '$':
                if (Peek() == '"')
                {
                    Advance(); // consume the "
                    InterpolatedString();
                }
                else
                {
                    throw new Exception($"Unexpected character '$' at line {_line}, column {_column}");
                }
                break;
            default:
                if (IsDigit(c))
                {
                    Number();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    throw new Exception($"Unexpected character '{c}' at line {_line}, column {_column}");
                }
                break;
        }
    }

    private char Advance()
    {
        _column++;
        return _source[_current++];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;

        _current++;
        _column++;
        return true;
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];

    private char PeekNext() => (_current + 1 >= _source.Length) ? '\0' : _source[_current + 1];

    private bool MatchSequence(string sequence)
    {
        if (_current + sequence.Length > _source.Length) return false;
        
        for (int i = 0; i < sequence.Length; i++)
        {
            if (_source[_current + i] != sequence[i]) return false;
        }
        
        // Consume the sequence
        _current += sequence.Length;
        _column += sequence.Length;
        return true;
    }

    private void ScanSpecificationOrComment()
    {
        // We're already past /*
        // Check if this is a specification block
        if (MatchSequence("spec"))
        {
            // Scan the specification content
            var specContent = new System.Text.StringBuilder();
            
            // Scan until we find spec*/
            while (!IsAtEnd())
            {
                if (Peek() == 's')
                {
                    // Check if this is the end marker
                    var saved_current = _current;
                    var saved_column = _column;
                    
                    if (MatchSequence("spec*/"))
                    {
                        // Found the end - create a token with the content
                        AddToken(TokenType.SpecStart, specContent.ToString().Trim());
                        return;
                    }
                    
                    // Not the end marker, restore position and continue
                    _current = saved_current;
                    _column = saved_column;
                }
                
                if (Peek() == '\n')
                {
                    _line++;
                    _column = 1;
                    specContent.AppendLine();
                }
                else
                {
                    _column++;
                    specContent.Append(Peek());
                }
                Advance();
            }
            
            throw new Exception($"Unterminated specification block starting at line {_line}");
        }
        else
        {
            // Regular block comment - skip it
            SkipBlockComment();
        }
    }

    private void SkipBlockComment()
    {
        // We're already past /*
        while (!IsAtEnd())
        {
            if (Peek() == '*' && PeekNext() == '/')
            {
                Advance(); // consume '*'
                Advance(); // consume '/'
                break;
            }
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            Advance();
        }
    }

    private void AddToken(TokenType type, object? literal = null)
    {
        string text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line, _column - text.Length));
    }

    private void StringLiteral()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            Advance();
        }

        if (IsAtEnd())
        {
            throw new Exception($"Unterminated string at line {_line}");
        }

        // The closing "
        Advance();

        // Trim the surrounding quotes and handle escape sequences
        string value = _source.Substring(_start + 1, _current - _start - 2);
        value = value.Replace("\\n", "\n")
                     .Replace("\\t", "\t")
                     .Replace("\\r", "\r")
                     .Replace("\\\\", "\\")
                     .Replace("\\\"", "\"");
        
        AddToken(TokenType.String, value);
    }

    private void InterpolatedString()
    {
        var parts = new List<object>();
        var currentPart = "";
        
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '{')
            {
                // Add the string part if not empty
                if (!string.IsNullOrEmpty(currentPart))
                {
                    parts.Add(currentPart);
                    currentPart = "";
                }
                
                Advance(); // consume '{'
                
                // Find the matching '}'
                var braceCount = 1;
                var expressionStart = _current;
                
                while (braceCount > 0 && !IsAtEnd())
                {
                    if (Peek() == '{') braceCount++;
                    else if (Peek() == '}') braceCount--;
                    Advance();
                }
                
                if (braceCount > 0)
                {
                    throw new Exception($"Unterminated interpolation expression at line {_line}");
                }
                
                // Extract the expression (excluding the closing '}')
                var expression = _source.Substring(expressionStart, _current - expressionStart - 1);
                parts.Add(new { IsExpression = true, Value = expression });
            }
            else
            {
                if (Peek() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                currentPart += Advance();
            }
        }
        
        if (IsAtEnd())
        {
            throw new Exception($"Unterminated interpolated string at line {_line}");
        }
        
        // Add the final string part if not empty
        if (!string.IsNullOrEmpty(currentPart))
        {
            parts.Add(currentPart);
        }
        
        // The closing "
        Advance();
        
        AddToken(TokenType.StringInterpolation, parts);
    }

    private void Number()
    {
        while (IsDigit(Peek())) Advance();

        // Look for a fractional part
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            // Consume the "."
            Advance();

            while (IsDigit(Peek())) Advance();
        }

        string numberStr = _source.Substring(_start, _current - _start);
        if (numberStr.Contains('.'))
        {
            AddToken(TokenType.Number, double.Parse(numberStr));
        }
        else
        {
            AddToken(TokenType.Number, int.Parse(numberStr));
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();

        string text = _source.Substring(_start, _current - _start);
        TokenType type = Keywords.ContainsKey(text) ? Keywords[text] : TokenType.Identifier;
        AddToken(type);
    }

    private bool IsDigit(char c) => c >= '0' && c <= '9';

    private bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

    private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
}

// =============================================================================
// PARSER
// =============================================================================

public class CadenzaParser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    public CadenzaParser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public Program Parse()
    {
        var statements = new List<ASTNode>();
        
        while (!IsAtEnd())
        {
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
        // Check for specification block first
        var specification = ParseSpecificationBlock();
        
        if (Match(TokenType.Module))
            return ParseModuleDeclaration(specification);
        if (Match(TokenType.Import))
            return ParseImportStatement();
        if (Match(TokenType.Export))
            return ParseExportStatement();
        if (Match(TokenType.Component))
            return ParseComponentDeclaration();
        if (Match(TokenType.AppState))
            return ParseAppStateDeclaration();
        if (Match(TokenType.ApiClient))
            return ParseApiClientDeclaration();
        if (Match(TokenType.Function) || Match(TokenType.Pure))
            return ParseFunctionDeclaration(specification);
        if (Match(TokenType.Return))
            return ParseReturnStatement();
        if (Match(TokenType.If))
            return ParseIfStatement();
        if (Match(TokenType.Let))
            return ParseLetStatement();
        if (Match(TokenType.Guard))
            return ParseGuardStatement();

        // If we have a specification but no matching declaration, that's an error
        if (specification != null)
        {
            throw new Exception($"Specification block found but no function or module declaration follows at line {Peek().Line}");
        }

        // Expression statement
        var expr = ParseExpression();
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        return expr;
    }

    private ModuleDeclaration ParseModuleDeclaration(SpecificationBlock? specification = null)
    {
        var name = Consume(TokenType.Identifier, "Expected module name").Lexeme;
        Consume(TokenType.LeftBrace, "Expected '{' after module name");
        
        var body = new List<ASTNode>();
        var exports = new List<string>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                body.Add(stmt);
                
                // Check if this is an exported function
                if (stmt is FunctionDeclaration func && func.IsExported)
                {
                    exports.Add(func.Name);
                }
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after module body");
        
        return new ModuleDeclaration(name, body, exports.Any() ? exports : null, specification);
    }

    private ImportStatement ParseImportStatement()
    {
        var moduleName = "";
        List<string>? specificImports = null;
        bool isWildcard = false;
        
        if (Check(TokenType.Identifier))
        {
            moduleName = Advance().Lexeme;
            
            if (Match(TokenType.Dot))
            {
                Consume(TokenType.LeftBrace, "Expected '{' for specific imports");
                specificImports = new List<string>();
                
                if (Check(TokenType.Multiply))
                {
                    Advance();
                    isWildcard = true;
                }
                else
                {
                    do
                    {
                        specificImports.Add(Consume(TokenType.Identifier, "Expected import name").Lexeme);
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightBrace, "Expected '}' after imports");
            }
        }
        
        return new ImportStatement(moduleName, specificImports, isWildcard);
    }

    private ASTNode ParseExportStatement()
    {
        if (Match(TokenType.Function) || Match(TokenType.Pure))
        {
            // This is an export function declaration - mark it as exported
            Previous(); // Go back
            return ParseFunctionDeclaration(null, true); // Mark as exported
        }
        else
        {
            // Export list - handle both syntax: export add, multiply AND export { add, multiply }
            var exports = new List<string>();
            
            // Check if using curly brace syntax: export { ... }
            if (Match(TokenType.LeftBrace))
            {
                // Parse: export { add, multiply }
                do
                {
                    exports.Add(Consume(TokenType.Identifier, "Expected export name").Lexeme);
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.RightBrace, "Expected '}' after export list");
            }
            else
            {
                // Parse: export add, multiply
                do
                {
                    exports.Add(Consume(TokenType.Identifier, "Expected export name").Lexeme);
                } while (Match(TokenType.Comma));
            }
            
            return new ExportStatement(exports);
        }
    }

    private ComponentDeclaration ParseComponentDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected component name").Lexeme;
        
        Consume(TokenType.LeftParen, "Expected '(' after component name");
        var parameters = new List<Parameter>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after parameter name");
                var paramType = ConsumeType("Expected parameter type").Lexeme;
                parameters.Add(new Parameter(paramName, paramType));
            } while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        List<string>? effects = null;
        if (Match(TokenType.Uses))
        {
            effects = ParseEffectsList();
        }
        
        List<StateDeclaration>? state = null;
        List<EventHandler>? events = null;
        ASTNode? onMount = null;
        
        // Parse state declarations if present
        if (Match(TokenType.State))
        {
            state = ParseStateDeclarationsList();
        }
        
        // Parse event handlers if present
        if (Match(TokenType.Events))
        {
            events = ParseEventHandlersList();
        }
        
        Consume(TokenType.Arrow, "Expected '->' after component signature");
        Consume(TokenType.Identifier, "Expected return type"); // UIComponent, etc.
        Consume(TokenType.LeftBrace, "Expected '{' to start component body");
        
        // Parse component body sections
        while (!Check(TokenType.Render) && !Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.State))
            {
                state = ParseStateDeclarations();
            }
            else if (Match(TokenType.Events))
            {
                events = ParseEventHandlers();
            }
            else if (Match(TokenType.OnMount))
            {
                onMount = ParseOnMount();
            }
            else
            {
                // Skip unknown tokens or parse other statements
                Advance();
            }
        }
        
        // Parse render block
        ASTNode renderBlock;
        if (Match(TokenType.Render))
        {
            renderBlock = ParseRenderBlock();
        }
        else
        {
            throw new Exception("Expected render block in component");
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after component body");
        
        return new ComponentDeclaration(name, parameters, effects, state, events, onMount, renderBlock);
    }

    private List<StateDeclaration> ParseStateDeclarations()
    {
        var declarations = new List<StateDeclaration>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after state keyword");
        
        do
        {
            var name = Consume(TokenType.Identifier, "Expected state variable name").Lexeme;
            Consume(TokenType.Colon, "Expected ':' after state variable name");
            var type = Consume(TokenType.Identifier, "Expected state variable type").Lexeme;
            
            ASTNode? initialValue = null;
            if (Match(TokenType.Assign))
            {
                initialValue = ParseExpression();
            }
            
            declarations.Add(new StateDeclaration(name, type, initialValue));
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after state declarations");
        
        return declarations;
    }

    private List<EventHandler> ParseEventHandlers()
    {
        var handlers = new List<EventHandler>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after events keyword");
        
        do
        {
            var name = Consume(TokenType.Identifier, "Expected event handler name").Lexeme;
            
            Consume(TokenType.LeftParen, "Expected '(' after event handler name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                    Consume(TokenType.Colon, "Expected ':' after parameter name");
                    var paramType = Consume(TokenType.Identifier, "Expected parameter type").Lexeme;
                    parameters.Add(new Parameter(paramName, paramType));
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            
            List<string>? effects = null;
            if (Match(TokenType.Uses))
            {
                effects = ParseEffectsList();
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' to start event handler body");
            var body = ParseStatements();
            Consume(TokenType.RightBrace, "Expected '}' after event handler body");
            
            handlers.Add(new EventHandler(name, parameters, effects, body));
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after event handlers");
        
        return handlers;
    }

    private ASTNode ParseOnMount()
    {
        Consume(TokenType.LeftBrace, "Expected '{' after on_mount");
        var statements = ParseStatements();
        Consume(TokenType.RightBrace, "Expected '}' after on_mount body");
        
        // Return a synthetic block expression
        return new CallExpression("on_mount_block", statements);
    }

    private ASTNode ParseRenderBlock()
    {
        Consume(TokenType.LeftBrace, "Expected '{' after render");
        
        var renderItems = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var item = ParseRenderItem();
            if (item != null)
            {
                renderItems.Add(item);
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after render block");
        
        // If multiple items, wrap in a fragment
        if (renderItems.Count == 1)
        {
            return renderItems[0];
        }
        else
        {
            return new UIElement("fragment", new List<UIAttribute>(), renderItems);
        }
    }

    private ASTNode? ParseRenderItem()
    {
        if (Match(TokenType.If))
        {
            return ParseConditionalRender();
        }
        else if (Match(TokenType.For))
        {
            return ParseLoopRender();
        }
        else if (Check(TokenType.Identifier))
        {
            // Could be a UI element or component instance
            var name = Advance().Lexeme;
            
            if (Match(TokenType.LeftParen))
            {
                // Component instance with props
                var props = ParseUIAttributes();
                Consume(TokenType.RightParen, "Expected ')' after component props");
                
                List<ASTNode>? children = null;
                if (Match(TokenType.LeftBrace))
                {
                    children = new List<ASTNode>();
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        var child = ParseRenderItem();
                        if (child != null) children.Add(child);
                    }
                    Consume(TokenType.RightBrace, "Expected '}' after component children");
                }
                
                return new ComponentInstance(name, props, children);
            }
            else
            {
                // Simple UI element
                var attributes = new List<UIAttribute>();
                List<ASTNode> children = new List<ASTNode>();
                
                if (Match(TokenType.LeftParen))
                {
                    attributes = ParseUIAttributes();
                    Consume(TokenType.RightParen, "Expected ')' after attributes");
                }
                
                if (Match(TokenType.LeftBrace))
                {
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        var child = ParseRenderItem();
                        if (child != null) children.Add(child);
                    }
                    Consume(TokenType.RightBrace, "Expected '}' after element children");
                }
                
                return new UIElement(name, attributes, children);
            }
        }
        else
        {
            // Expression (like text content)
            return ParseExpression();
        }
    }

    private ConditionalRender ParseConditionalRender()
    {
        var condition = ParseExpression();
        
        Consume(TokenType.LeftBrace, "Expected '{' after if condition");
        var thenBody = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var item = ParseRenderItem();
            if (item != null) thenBody.Add(item);
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after if body");
        
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after else");
            elseBody = new List<ASTNode>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var item = ParseRenderItem();
                if (item != null) elseBody.Add(item);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after else body");
        }
        
        return new ConditionalRender(condition, thenBody, elseBody);
    }

    private IterativeRender ParseLoopRender()
    {
        var variable = Consume(TokenType.Identifier, "Expected variable name after 'for'").Lexeme;
        Consume(TokenType.In, "Expected 'in' after loop variable");
        var collection = ParseExpression();
        
        ASTNode? condition = null;
        if (Match(TokenType.Where))
        {
            condition = ParseExpression();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' after for statement");
        var body = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var item = ParseRenderItem();
            if (item != null) body.Add(item);
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after for body");
        
        return new IterativeRender(variable, collection, condition, body);
    }

    private List<UIAttribute> ParseUIAttributes()
    {
        var attributes = new List<UIAttribute>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var name = Consume(TokenType.Identifier, "Expected attribute name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after attribute name");
                var value = ParseComplexExpression();
                attributes.Add(new UIAttribute(name, value));
            } while (Match(TokenType.Comma));
        }
        
        return attributes;
    }

    private AppStateDeclaration ParseAppStateDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected app state name").Lexeme;
        
        List<string>? effects = null;
        if (Match(TokenType.Uses))
        {
            effects = ParseEffectsList();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' after app state declaration");
        
        var stateVariables = new List<StateDeclaration>();
        var actions = new List<StateAction>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(TokenType.Identifier))
            {
                // State variable declaration
                var varName = Advance().Lexeme;
                Consume(TokenType.Colon, "Expected ':' after state variable name");
                var varType = Consume(TokenType.Identifier, "Expected state variable type").Lexeme;
                
                ASTNode? initialValue = null;
                if (Match(TokenType.Assign))
                {
                    initialValue = ParseExpression();
                }
                
                stateVariables.Add(new StateDeclaration(varName, varType, initialValue));
            }
            else if (Match(TokenType.Action))
            {
                // Action declaration
                var actionName = Consume(TokenType.Identifier, "Expected action name").Lexeme;
                
                Consume(TokenType.LeftParen, "Expected '(' after action name");
                var parameters = new List<Parameter>();
                
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                        Consume(TokenType.Colon, "Expected ':' after parameter name");
                        var paramType = Consume(TokenType.Identifier, "Expected parameter type").Lexeme;
                        parameters.Add(new Parameter(paramName, paramType));
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after parameters");
                
                List<string>? actionEffects = null;
                if (Match(TokenType.Uses))
                {
                    actionEffects = ParseEffectsList();
                }
                
                Consume(TokenType.LeftBrace, "Expected '{' to start action body");
                var body = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after action body");
                
                actions.Add(new StateAction(actionName, parameters, actionEffects, body));
            }
            else
            {
                Advance(); // Skip unknown tokens
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after app state body");
        
        return new AppStateDeclaration(name, stateVariables, actions, effects);
    }

    private ApiClientDeclaration ParseApiClientDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected API client name").Lexeme;
        Consume(TokenType.From, "Expected 'from' after API client name");
        var baseUrl = Consume(TokenType.String, "Expected base URL string").Literal?.ToString() ?? "";
        
        Consume(TokenType.LeftBrace, "Expected '{' after API client declaration");
        
        var methods = new List<ApiMethod>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var methodName = Consume(TokenType.Identifier, "Expected method name").Lexeme;
            
            Consume(TokenType.LeftParen, "Expected '(' after method name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                    Consume(TokenType.Colon, "Expected ':' after parameter name");
                    var paramType = Consume(TokenType.Identifier, "Expected parameter type").Lexeme;
                    parameters.Add(new Parameter(paramName, paramType));
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            Consume(TokenType.Arrow, "Expected '->' after method parameters");
            var returnType = Consume(TokenType.Identifier, "Expected return type").Lexeme;
            
            List<string>? effects = null;
            if (Match(TokenType.Uses))
            {
                effects = ParseEffectsList();
            }
            
            methods.Add(new ApiMethod(methodName, parameters, returnType, effects));
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after API client body");
        
        return new ApiClientDeclaration(name, baseUrl, methods);
    }

    private FunctionDeclaration ParseFunctionDeclaration(SpecificationBlock? specification = null, bool isExported = false)
    {
        bool isPure = Previous().Type == TokenType.Pure;
        if (isPure && !Match(TokenType.Function))
        {
            throw new Exception("Expected 'function' after 'pure'");
        }

        var name = Consume(TokenType.Identifier, "Expected function name").Lexeme;
        
        Consume(TokenType.LeftParen, "Expected '(' after function name");
        var parameters = new List<Parameter>();
        
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after parameter name");
                var paramType = ParseType();
                parameters.Add(new Parameter(paramName, paramType));
            } while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        List<string>? effects = null;
        if (Match(TokenType.Uses))
        {
            effects = ParseEffectsList();
        }
        
        string? returnType = null;
        if (Match(TokenType.Arrow))
        {
            returnType = ParseType();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' before function body");
        var body = ParseStatements();
        Consume(TokenType.RightBrace, "Expected '}' after function body");
        
        return new FunctionDeclaration(name, parameters, returnType, body, isPure, effects, isExported, specification);
    }

    private List<string> ParseEffectsList()
    {
        var effects = new List<string>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
        
        do
        {
            // Effect names can be specific token types or identifiers
            var token = Advance();
            string effectName = token.Type switch
            {
                TokenType.Database => "Database",
                TokenType.Network => "Network", 
                TokenType.Logging => "Logging",
                TokenType.FileSystem => "FileSystem",
                TokenType.Memory => "Memory",
                TokenType.IO => "IO",
                TokenType.Identifier => token.Lexeme,
                _ => throw new Exception($"Expected effect name. Got '{token.Lexeme}' at line {token.Line}")
            };
            effects.Add(effectName);
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after effects list");
        
        return effects;
    }

    private string ParseType()
    {
        if (Match(TokenType.Result))
        {
            Consume(TokenType.Less, "Expected '<' after Result");
            var okType = ParseType();
            Consume(TokenType.Comma, "Expected ',' in Result type");
            var errorType = ParseType();
            Consume(TokenType.Greater, "Expected '>' after Result type");
            return $"Result<{okType}, {errorType}>";
        }
        
        if (Match(TokenType.List))
        {
            Consume(TokenType.Less, "Expected '<' after List");
            var elementType = ParseType();
            Consume(TokenType.Greater, "Expected '>' after List type");
            return $"List<{elementType}>";
        }
        
        if (Match(TokenType.Option))
        {
            Consume(TokenType.Less, "Expected '<' after Option");
            var valueType = ParseType();
            Consume(TokenType.Greater, "Expected '>' after Option type");
            return $"Option<{valueType}>";
        }
        
        var token = Advance();
        return token.Lexeme;
    }

    private ReturnStatement ParseReturnStatement()
    {
        ASTNode? expression = null;
        if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
        {
            expression = ParseExpression();
        }
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        return new ReturnStatement(expression);
    }

    private IfStatement ParseIfStatement()
    {
        var condition = ParseExpression();
        
        Consume(TokenType.LeftBrace, "Expected '{' after if condition");
        var thenBody = ParseStatements();
        Consume(TokenType.RightBrace, "Expected '}' after if body");
        
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after else");
            elseBody = ParseStatements();
            Consume(TokenType.RightBrace, "Expected '}' after else body");
        }
        
        return new IfStatement(condition, thenBody, elseBody);
    }

    private LetStatement ParseLetStatement()
    {
        var name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
        
        string? type = null;
        if (Match(TokenType.Colon))
        {
            type = ParseType();
        }
        
        Consume(TokenType.Assign, "Expected '=' after variable declaration");
        var expression = ParseExpression();
        
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        
        return new LetStatement(name, type, expression);
    }

    private GuardStatement ParseGuardStatement()
    {
        var condition = ParseExpression();
        
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after 'else' in guard statement");
            elseBody = ParseStatements();
            Consume(TokenType.RightBrace, "Expected '}' to close guard else block");
        }
        
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        
        return new GuardStatement(condition, elseBody);
    }
    
    private MatchExpression ParseMatchExpression()
    {
        var value = ParseExpression();
        Consume(TokenType.LeftBrace, "Expected '{' after match expression");
        
        var cases = new List<MatchCase>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            // Parse pattern like "Ok(x)" or "Error(e)" or "Some(val)" or "None"
            string pattern;
            if (Check(TokenType.Ok) || Check(TokenType.Error) || Check(TokenType.Some) || Check(TokenType.None))
            {
                pattern = Advance().Lexeme;
            }
            else
            {
                pattern = Consume(TokenType.Identifier, "Expected pattern in match case").Lexeme;
            }
            string? variable = null;
            
            if (Match(TokenType.LeftParen))
            {
                variable = Consume(TokenType.Identifier, "Expected variable name in pattern").Lexeme;
                Consume(TokenType.RightParen, "Expected ')' after pattern variable");
            }
            
            Consume(TokenType.Arrow, "Expected '->' after match pattern");
            
            // Parse the case body
            var caseBody = new List<ASTNode>();
            if (Match(TokenType.LeftBrace))
            {
                caseBody = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after match case body");
            }
            else
            {
                // Single expression
                caseBody.Add(ParseExpression());
            }
            
            cases.Add(new MatchCase(pattern, variable, caseBody));
            
            // Optional comma between cases
            Match(TokenType.Comma);
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after match cases");
        return new MatchExpression(value, cases);
    }

    private List<ASTNode> ParseStatements()
    {
        var statements = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }
        
        return statements;
    }

    private ASTNode ParseExpression()
    {
        return ParseComplexExpression();
    }

    private ASTNode ParseComplexExpression()
    {
        return ParseTernaryExpression();
    }

    private ASTNode ParseTernaryExpression()
    {
        var expr = ParseLogicalOrExpression();
        
        if (Match(TokenType.Question))
        {
            var thenExpr = ParseLogicalOrExpression();
            Consume(TokenType.Colon, "Expected ':' after ternary then expression");
            var elseExpr = ParseTernaryExpression();
            return new TernaryExpression(expr, thenExpr, elseExpr);
        }
        
        return expr;
    }

    private ASTNode ParseLogicalOrExpression()
    {
        var expr = ParseLogicalAndExpression();
        
        while (Match(TokenType.Or))
        {
            var op = Previous().Lexeme;
            var right = ParseLogicalAndExpression();
            expr = new LogicalExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseLogicalAndExpression()
    {
        var expr = ParseEqualityExpression();
        
        while (Match(TokenType.And))
        {
            var op = Previous().Lexeme;
            var right = ParseEqualityExpression();
            expr = new LogicalExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseEqualityExpression()
    {
        var expr = ParseComparisonExpression();
        
        while (Match(TokenType.Equal, TokenType.NotEqual))
        {
            var op = Previous().Lexeme;
            var right = ParseComparisonExpression();
            expr = new ComparisonExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseComparisonExpression()
    {
        var expr = ParseArithmeticExpression();
        
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous().Lexeme;
            var right = ParseArithmeticExpression();
            expr = new ComparisonExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseArithmeticExpression()
    {
        var expr = ParseTermExpression();
        
        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var op = Previous().Lexeme;
            var right = ParseTermExpression();
            expr = new ArithmeticExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseTermExpression()
    {
        var expr = ParseUnaryExpression();
        
        while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo))
        {
            var op = Previous().Lexeme;
            var right = ParseUnaryExpression();
            expr = new ArithmeticExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseUnaryExpression()
    {
        if (Match(TokenType.Not, TokenType.Minus))
        {
            var op = Previous().Lexeme;
            var right = ParseUnaryExpression();
            return new UnaryExpression(op, right);
        }
        
        return ParseMemberAccessExpression();
    }

    private ASTNode ParseMemberAccessExpression()
    {
        var expr = ParsePrimaryExpression();
        
        while (true)
        {
            if (Match(TokenType.Dot))
            {
                var member = Consume(TokenType.Identifier, "Expected member name after '.'").Lexeme;
                
                if (Match(TokenType.LeftParen))
                {
                    // Method call
                    var args = new List<ASTNode>();
                    
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightParen, "Expected ')' after method arguments");
                    expr = new MethodCallExpression(expr, member, args);
                }
                else
                {
                    // Property access
                    expr = new MemberAccessExpression(expr, member);
                }
            }
            else if (Match(TokenType.Question))
            {
                // Error propagation
                expr = new ErrorPropagation(expr);
            }
            else if (Match(TokenType.LeftBracket))
            {
                // List access: list[index]
                var index = ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after list index");
                expr = new ListAccessExpression(expr, index);
            }
            else
            {
                break;
            }
        }
        
        return expr;
    }

    private ASTNode ParsePrimaryExpression()
    {
        if (Match(TokenType.Number))
        {
            var value = Previous().Literal;
            if (value is int intValue)
            {
                return new NumberLiteral(intValue);
            }
            else if (value is double doubleValue)
            {
                return new NumberLiteral((int)doubleValue); // For now, convert to int
            }
        }
        
        if (Match(TokenType.String))
        {
            return new StringLiteral(Previous().Literal?.ToString() ?? "");
        }
        
        if (Match(TokenType.Bool))
        {
            var value = Previous().Lexeme;
            return new BooleanLiteral(value == "true");
        }
        
        if (Match(TokenType.StringInterpolation))
        {
            var parts = Previous().Literal as List<object> ?? new List<object>();
            var interpolationParts = new List<ASTNode>();
            
            foreach (var part in parts)
            {
                if (part is string stringPart)
                {
                    interpolationParts.Add(new StringLiteral(stringPart));
                }
                else if (part is Dictionary<string, object> exprPart && exprPart.ContainsKey("IsExpression"))
                {
                    var exprString = exprPart["Value"]?.ToString() ?? "";
                    // Parse the expression string
                    var lexer = new CadenzaLexer(exprString);
                    var tokens = lexer.ScanTokens();
                    var parser = new CadenzaParser(tokens);
                    var expr = parser.ParseExpression();
                    interpolationParts.Add(expr);
                }
            }
            
            return new StringInterpolation(interpolationParts);
        }
        
        if (Match(TokenType.Identifier))
        {
            var name = Previous().Lexeme;
            
            if (Match(TokenType.LeftParen))
            {
                // Function call
                var args = new List<ASTNode>();
                
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after arguments");
                return new CallExpression(name, args);
            }
            
            return new Identifier(name);
        }
        
        if (Match(TokenType.Ok, TokenType.Error))
        {
            var type = Previous().Lexeme;
            Consume(TokenType.LeftParen, $"Expected '(' after '{type}'");
            var value = ParseExpression();
            Consume(TokenType.RightParen, $"Expected ')' after {type} value");
            return new ResultExpression(type, value);
        }
        
        if (Match(TokenType.Some))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'Some'");
            var value = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after Some value");
            return new OptionExpression("Some", value);
        }
        
        if (Match(TokenType.None))
        {
            return new OptionExpression("None", null);
        }
        
        if (Match(TokenType.Match))
        {
            return ParseMatchExpression();
        }
        
        if (Match(TokenType.LeftBracket))
        {
            // List literal: [1, 2, 3]
            var elements = new List<ASTNode>();
            
            if (!Check(TokenType.RightBracket))
            {
                do
                {
                    elements.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightBracket, "Expected ']' after list elements");
            return new ListExpression(elements);
        }
        
        if (Match(TokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return expr;
        }
        
        throw new Exception($"Unexpected token '{Peek().Lexeme}' at line {Peek().Line}");
    }

    // Utility methods
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

    private SpecificationBlock? ParseSpecificationBlock()
    {
        if (!Check(TokenType.SpecStart)) return null;
        
        var specToken = Advance(); // Consume SpecStart token
        var content = specToken.Literal?.ToString() ?? "";
        
        // Parse the YAML-like content
        var intent = "";
        var rules = new List<string>();
        var postconditions = new List<string>();
        string? sourceDoc = null;
        
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? currentSection = null;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            
            if (trimmed.StartsWith("intent:"))
            {
                intent = trimmed.Substring(7).Trim().Trim('"');
                currentSection = "intent";
            }
            else if (trimmed.StartsWith("rules:"))
            {
                currentSection = "rules";
            }
            else if (trimmed.StartsWith("postconditions:"))
            {
                currentSection = "postconditions";
            }
            else if (trimmed.StartsWith("source_doc:"))
            {
                sourceDoc = trimmed.Substring(11).Trim().Trim('"');
                currentSection = "source_doc";
            }
            else if (trimmed.StartsWith("- "))
            {
                var item = trimmed.Substring(2).Trim().Trim('"');
                if (currentSection == "rules")
                {
                    rules.Add(item);
                }
                else if (currentSection == "postconditions")
                {
                    postconditions.Add(item);
                }
            }
        }
        
        if (string.IsNullOrEmpty(intent))
        {
            throw new Exception($"Specification block missing required 'intent' field at line {specToken.Line}");
        }
        
        return new SpecificationBlock(
            intent,
            rules.Count > 0 ? rules : null,
            postconditions.Count > 0 ? postconditions : null,
            sourceDoc
        );
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    private Token Advance() => IsAtEnd() ? Previous() : _tokens[_current++];

    private bool IsAtEnd() => Peek().Type == TokenType.EOF;

    private Token Peek() => _tokens[_current];

    private Token Previous() => _tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new Exception($"{message}. Got '{Peek().Lexeme}' at line {Peek().Line}");
    }
    
    private Token ConsumeType(string message)
    {
        // Accept both identifiers and type keywords
        if (Check(TokenType.Identifier) || Check(TokenType.String_Type) || Check(TokenType.Int) || 
            Check(TokenType.Bool) || Check(TokenType.List) || Check(TokenType.Option))
        {
            return Advance();
        }
        throw new Exception($"{message}. Got '{Peek().Lexeme}' at line {Peek().Line}");
    }
    
    private List<StateDeclaration> ParseStateDeclarationsList()
    {
        var declarations = new List<StateDeclaration>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after state keyword");
        
        do
        {
            var name = Consume(TokenType.Identifier, "Expected state variable name").Lexeme;
            // For now, assume all state variables are strings (could be enhanced later)
            declarations.Add(new StateDeclaration(name, "string"));
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after state declarations");
        
        return declarations;
    }
    
    private List<EventHandler> ParseEventHandlersList()
    {
        var handlers = new List<EventHandler>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after events keyword");
        
        do
        {
            var name = Consume(TokenType.Identifier, "Expected event handler name").Lexeme;
            // Create placeholder event handlers - they will be parsed in the body
            handlers.Add(new EventHandler(name, new List<Parameter>(), null, new List<ASTNode>()));
        } while (Match(TokenType.Comma));
        
        Consume(TokenType.RightBracket, "Expected ']' after event handlers");
        
        return handlers;
    }
}

// =============================================================================
// C# CODE GENERATOR
// =============================================================================

public class CSharpGenerator
{
    private readonly HashSet<string> _generatedNamespaces = new();
    private readonly List<string> _usingStatements = new();
    private readonly Dictionary<string, string> _importedSymbols = new(); // Track imported symbols
    
    public SyntaxTree GenerateFromAST(Program program)
    {
        var namespaceMembers = new Dictionary<string, List<MemberDeclarationSyntax>>();
        var globalMembers = new List<MemberDeclarationSyntax>();
        
        // Add Result struct and helper class if any function uses Result types
        var resultTypes = GenerateResultTypes();
        globalMembers.AddRange(resultTypes);
        
        // Add Option struct and helper class if any function uses Option types
        var optionTypes = GenerateOptionTypes();
        globalMembers.AddRange(optionTypes);
        
        // First pass: Process imports to build symbol mapping
        foreach (var statement in program.Statements)
        {
            if (statement is ImportStatement import)
            {
                ProcessImportStatement(import);
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
        _usingStatements.Add("Cadenza.Runtime");
        
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
        // Generate Result<T,E> struct
        var resultStruct = StructDeclaration("Result")
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
        for (int i = 0; i < func.Body.Count; i++)
        {
            var stmt = func.Body[i];
            var isLastStatement = i == func.Body.Count - 1;
            
            // If this is the last statement and it's an expression (not a return statement),
            // wrap it in a return statement
            if (isLastStatement && stmt is not ReturnStatement && func.ReturnType != null && func.ReturnType != "void")
            {
                var returnStmt = ReturnStatement(GenerateExpression(stmt));
                statements.Add(returnStmt);
            }
            else
            {
                var generated = GenerateStatementSyntax(stmt);
                if (generated != null)
                {
                    statements.Add(generated);
                }
            }
        }
        
        method = method.WithBody(Block(statements));
        
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
    
    private LocalDeclarationStatementSyntax GenerateLetStatement(LetStatement let)
    {
        var variableType = let.Type != null ? ParseTypeName(MapCadenzaTypeToCSharp(let.Type)) : IdentifierName("var");
        
        return LocalDeclarationStatement(
            VariableDeclaration(variableType)
                .AddVariables(
                    VariableDeclarator(let.Name)
                        .WithInitializer(EqualsValueClause(GenerateExpression(let.Expression)))
                )
        );
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
        return expression switch
        {
            BinaryExpression binary => GenerateBinaryExpression(binary),
            CallExpression call => GenerateCallExpression(call),
            MethodCallExpression methodCall => GenerateMethodCallExpression(methodCall),
            Identifier id => IdentifierName(id.Name),
            NumberLiteral num => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(num.Value)),
            StringLiteral str => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str.Value)),
            BooleanLiteral boolean => LiteralExpression(boolean.Value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
            ResultExpression result => GenerateResultExpression(result),
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
            _ => SyntaxKind.AddExpression
        };
        
        return BinaryExpression(kind, left, right);
    }

    private InvocationExpressionSyntax GenerateMethodCallExpression(MethodCallExpression methodCall)
    {
        // Generate the object expression first
        var objectExpression = GenerateExpression(methodCall.Object);
        
        // Create member access expression: object.method
        var memberAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            objectExpression,
            IdentifierName(methodCall.Method)
        );
        
        // Generate arguments
        var args = methodCall.Arguments.Select(arg => Argument(GenerateExpression(arg))).ToArray();
        
        return InvocationExpression(memberAccess)
            .AddArgumentListArguments(args);
    }
    
    private InvocationExpressionSyntax GenerateCallExpression(CallExpression call)
    {
        ExpressionSyntax expression;
        
        // Handle member access calls like Console.WriteLine
        if (call.Name.Contains('.'))
        {
            var parts = call.Name.Split('.');
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
    
    private InvocationExpressionSyntax GenerateResultExpression(ResultExpression result)
    {
        var methodName = result.Type == "Ok" ? "Ok" : "Error";
        
        // For now, use common type combinations - in a full implementation, 
        // this would need proper type inference from the function signature
        var typeArgs = result.Type == "Ok" ? 
            GenericName("Ok").WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>(new[] { 
                IdentifierName("object"), 
                IdentifierName("string") 
            }))) :
            GenericName("Error").WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>(new[] { 
                IdentifierName("object"), 
                IdentifierName("string") 
            })));
        
        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Result"),
                typeArgs
            )
        ).AddArgumentListArguments(Argument(GenerateExpression(result.Value)));
    }
    
    private ExpressionSyntax GenerateErrorPropagation(ErrorPropagation error)
    {
        // For now, just generate the expression - in a full implementation,
        // this would need more sophisticated handling
        return GenerateExpression(error.Expression);
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
        // For now, convert to string concatenation
        ExpressionSyntax result = LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(""));
        
        foreach (var part in interpolation.Parts)
        {
            ExpressionSyntax partExpr;
            if (part is StringLiteral str)
            {
                partExpr = LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str.Value));
            }
            else
            {
                partExpr = GenerateExpression(part);
            }
            
            result = BinaryExpression(SyntaxKind.AddExpression, result, partExpr);
        }
        
        return result;
    }
    
    private List<SyntaxTrivia> GenerateXmlDocumentation(FunctionDeclaration func)
    {
        var trivia = new List<SyntaxTrivia>();
        
        if (func.Specification != null)
        {
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
        
        // Add parameter documentation
        foreach (var param in func.Parameters)
        {
            trivia.Add(Comment($"/// <param name=\"{param.Name}\">Parameter of type {param.Type}</param>"));
            trivia.Add(EndOfLine("\n"));
        }
        
        // Add return documentation
        if (!string.IsNullOrEmpty(func.ReturnType))
        {
            trivia.Add(Comment($"/// <returns>Returns {func.ReturnType}</returns>"));
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
        var kind = logical.Operator switch
        {
            "&&" => SyntaxKind.LogicalAndExpression,
            "||" => SyntaxKind.LogicalOrExpression,
            _ => SyntaxKind.LogicalAndExpression
        };
        
        return BinaryExpression(
            kind,
            GenerateExpression(logical.Left),
            GenerateExpression(logical.Right)
        );
    }
    
    private BinaryExpressionSyntax GenerateComparisonExpression(ComparisonExpression comparison)
    {
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
        
        return BinaryExpression(
            kind,
            GenerateExpression(comparison.Left),
            GenerateExpression(comparison.Right)
        );
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
            // For now, wildcards are not supported - could be implemented later
            // Would require knowing all exported symbols from the module
        }
    }
}

// =============================================================================
// DIRECT COMPILER
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
// TRANSPILER
// =============================================================================

public class CadenzaTranspiler
{
    public async Task<string> TranspileAsync(string sourceFile, string? outputFile = null)
    {
        // Read source file
        var source = await File.ReadAllTextAsync(sourceFile);
        
        // Lex
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        // Parse
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Generate C#
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(ast);
        
        var csharpCode = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
        
        // Write to output file if specified
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, csharpCode);
        }
        
        return csharpCode;
    }
    
    public async Task<string> TranspileToJavaScriptAsync(string sourceFile, string? outputFile = null)
    {
        // For now, return a placeholder - full JavaScript generation would be implemented here
        var jsCode = "// JavaScript generation not yet implemented in core compiler\n// Use cadenzac-dev tool for JavaScript compilation";
        
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, jsCode);
        }
        
        return jsCode;
    }
    
    public async Task<string> TranspileToBlazorAsync(string sourceFile, string? outputFile = null)
    {
        // Read source file
        var source = await File.ReadAllTextAsync(sourceFile);
        
        // Lex
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        // Parse
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Generate Blazor
        var blazorGenerator = new BlazorGenerator();
        var blazorContent = new System.Text.StringBuilder();
        
        // Generate Blazor components from AST
        foreach (var statement in ast.Statements)
        {
            if (statement is ComponentDeclaration component)
            {
                blazorContent.AppendLine(blazorGenerator.GenerateBlazorComponent(component));
            }
        }
        
        var blazorCode = blazorContent.ToString();
        
        // Write to output file if specified
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, blazorCode);
        }
        
        return blazorCode;
    }
}

// =============================================================================
// MAIN PROGRAM
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

public class FlowCoreProgram
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new DirectCompilerCLI();
        return await cli.RunAsync(args);
    }
}