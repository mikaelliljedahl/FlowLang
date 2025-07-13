// FlowLang Core Compiler - Pure Transpilation Only
// Extracted from flowc.cs for simplicity and reliability

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
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
public record ModuleDeclaration(string Name, List<ASTNode> Body, List<string>? Exports = null) : ASTNode;
public record ImportStatement(string ModuleName, List<string>? SpecificImports = null, bool IsWildcard = false) : ASTNode;
public record ExportStatement(List<string> ExportedNames) : ASTNode;

// Function AST nodes
public record FunctionDeclaration(
    string Name, 
    List<Parameter> Parameters, 
    string? ReturnType, 
    List<ASTNode> Body, 
    bool IsPure = false,
    List<string>? Effects = null,
    bool IsExported = false
) : ASTNode;

public record Parameter(string Name, string Type);

// Statement AST nodes
public record ReturnStatement(ASTNode? Expression) : ASTNode;
public record IfStatement(ASTNode Condition, List<ASTNode> ThenBody, List<ASTNode>? ElseBody = null) : ASTNode;
public record LetStatement(string Name, string? Type, ASTNode Expression) : ASTNode;
public record GuardStatement(ASTNode Condition, ASTNode? Expression = null) : ASTNode;

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

public class FlowLangLexer
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
        {"uses", TokenType.Uses},
        {"Result", TokenType.Result},
        {"Ok", TokenType.Ok},
        {"Error", TokenType.Error},
        {"let", TokenType.Let},
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
        {"bool", TokenType.Bool}
    };

    public FlowLangLexer(string source)
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
            return ParseModuleDeclaration();
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
            return ParseFunctionDeclaration();
        if (Match(TokenType.Return))
            return ParseReturnStatement();
        if (Match(TokenType.If))
            return ParseIfStatement();
        if (Match(TokenType.Let))
            return ParseLetStatement();
        if (Match(TokenType.Guard))
            return ParseGuardStatement();

        // Expression statement
        var expr = ParseExpression();
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        return expr;
    }

    private ModuleDeclaration ParseModuleDeclaration()
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
        
        return new ModuleDeclaration(name, body, exports.Any() ? exports : null);
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

    private ExportStatement ParseExportStatement()
    {
        var exports = new List<string>();
        
        if (Match(TokenType.Function) || Match(TokenType.Pure))
        {
            // This is an export function declaration - mark it as exported
            Previous(); // Go back
            var func = ParseFunctionDeclaration();
            if (func is FunctionDeclaration fd)
            {
                exports.Add(fd.Name);
                return new ExportStatement(exports);
            }
        }
        else
        {
            // Export list
            do
            {
                exports.Add(Consume(TokenType.Identifier, "Expected export name").Lexeme);
            } while (Match(TokenType.Comma));
        }
        
        return new ExportStatement(exports);
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
        
        List<StateDeclaration>? state = null;
        List<EventHandler>? events = null;
        ASTNode? onMount = null;
        
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

    private FunctionDeclaration ParseFunctionDeclaration()
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
        
        return new FunctionDeclaration(name, parameters, returnType, body, isPure, effects);
    }

    private List<string> ParseEffectsList()
    {
        var effects = new List<string>();
        
        Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
        
        do
        {
            var effect = Consume(TokenType.Identifier, "Expected effect name").Lexeme;
            effects.Add(effect);
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
        
        ASTNode? expression = null;
        if (Match(TokenType.Else))
        {
            expression = ParseExpression();
        }
        
        if (Match(TokenType.Semicolon)) {} // Optional semicolon
        
        return new GuardStatement(condition, expression);
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
                    var lexer = new FlowLangLexer(exprString);
                    var tokens = lexer.ScanTokens();
                    var parser = new FlowLangParser(tokens);
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
}

// =============================================================================
// C# CODE GENERATOR
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
        var resultClass = GenerateResultClass();
        globalMembers.Add(resultClass);
        
        foreach (var statement in program.Statements)
        {
            var member = GenerateStatement(statement);
            if (member != null)
            {
                if (statement is ModuleDeclaration module)
                {
                    var namespaceName = $"FlowLang.Modules.{module.Name}";
                    if (!namespaceMembers.ContainsKey(namespaceName))
                    {
                        namespaceMembers[namespaceName] = new List<MemberDeclarationSyntax>();
                    }
                    namespaceMembers[namespaceName].AddRange(member as IEnumerable<MemberDeclarationSyntax> ?? new[] { member });
                }
                else
                {
                    globalMembers.Add(member);
                }
            }
        }
        
        // Create compilation unit
        var compilationUnit = CompilationUnit();
        
        // Add using statements
        _usingStatements.Add("System");
        _usingStatements.Add("System.Collections.Generic");
        _usingStatements.Add("System.Threading.Tasks");
        
        foreach (var usingStmt in _usingStatements.Distinct())
        {
            compilationUnit = compilationUnit.AddUsings(UsingDirective(ParseName(usingStmt)));
        }
        
        // Add namespace members
        foreach (var (namespaceName, members) in namespaceMembers)
        {
            var namespaceDecl = NamespaceDeclaration(ParseName(namespaceName))
                .AddMembers(members.ToArray());
            compilationUnit = compilationUnit.AddMembers(namespaceDecl);
        }
        
        // Add global members
        compilationUnit = compilationUnit.AddMembers(globalMembers.ToArray());
        
        return CSharpSyntaxTree.Create(compilationUnit);
    }
    
    private ClassDeclarationSyntax GenerateResultClass()
    {
        return ClassDeclaration("Result")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddMembers(
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
            ParseTypeName(func.ReturnType ?? "void"),
            func.Name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));
        
        // Add parameters
        foreach (var param in func.Parameters)
        {
            method = method.AddParameterListParameters(
                Parameter(Identifier(param.Name))
                    .WithType(ParseTypeName(param.Type))
            );
        }
        
        // Add XML documentation for effects
        if (func.Effects?.Any() == true)
        {
            var effectsComment = $"Effects: {string.Join(", ", func.Effects)}";
            method = method.WithLeadingTrivia(
                Comment($"/// <summary>{effectsComment}</summary>"));
        }
        
        // Generate method body
        var statements = new List<StatementSyntax>();
        foreach (var stmt in func.Body)
        {
            var generated = GenerateStatementSyntax(stmt);
            if (generated != null)
            {
                statements.Add(generated);
            }
        }
        
        method = method.WithBody(Block(statements));
        
        return method;
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
            _ => ExpressionStatement(GenerateExpression(statement))
        };
    }
    
    private ReturnStatementSyntax GenerateReturnStatement(ReturnStatement ret)
    {
        if (ret.Expression != null)
        {
            return ReturnStatement(GenerateExpression(ret.Expression));
        }
        return ReturnStatement();
    }
    
    private LocalDeclarationStatementSyntax GenerateLetStatement(LetStatement let)
    {
        var variableType = let.Type != null ? ParseTypeName(let.Type) : IdentifierName("var");
        
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
    
    private ExpressionSyntax GenerateExpression(ASTNode expression)
    {
        return expression switch
        {
            BinaryExpression binary => GenerateBinaryExpression(binary),
            CallExpression call => GenerateCallExpression(call),
            Identifier id => IdentifierName(id.Name),
            NumberLiteral num => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(num.Value)),
            StringLiteral str => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str.Value)),
            ResultExpression result => GenerateResultExpression(result),
            ErrorPropagation error => GenerateErrorPropagation(error),
            MemberAccessExpression member => GenerateMemberAccess(member),
            StringInterpolation interpolation => GenerateStringInterpolation(interpolation),
            TernaryExpression ternary => GenerateTernaryExpression(ternary),
            LogicalExpression logical => GenerateLogicalExpression(logical),
            ComparisonExpression comparison => GenerateComparisonExpression(comparison),
            ArithmeticExpression arithmetic => GenerateArithmeticExpression(arithmetic),
            UnaryExpression unary => GenerateUnaryExpression(unary),
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

    private InvocationExpressionSyntax GenerateCallExpression(CallExpression call)
    {
        var expression = IdentifierName(call.Name);
        var args = call.Arguments.Select(arg => Argument(GenerateExpression(arg))).ToArray();
        
        return InvocationExpression(expression)
            .AddArgumentListArguments(args);
    }
    
    private InvocationExpressionSyntax GenerateResultExpression(ResultExpression result)
    {
        var methodName = result.Type == "Ok" ? "Ok" : "Error";
        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Result"),
                IdentifierName(methodName)
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
        return MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            GenerateExpression(member.Object),
            IdentifierName(member.Member)
        );
    }
    
    private InterpolatedStringExpressionSyntax GenerateStringInterpolation(StringInterpolation interpolation)
    {
        var content = new List<InterpolatedStringContentSyntax>();
        
        foreach (var part in interpolation.Parts)
        {
            if (part is StringLiteral str)
            {
                content.Add(InterpolatedStringText(Token(SyntaxKind.InterpolatedStringTextToken, str.Value)));
            }
            else
            {
                content.Add(Interpolation(GenerateExpression(part)));
            }
        }
        
        return InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
            .AddContents(content.ToArray());
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
}

// =============================================================================
// TRANSPILER
// =============================================================================

public class FlowLangTranspiler
{
    public async Task<string> TranspileAsync(string sourceFile, string? outputFile = null)
    {
        // Read source file
        var source = await File.ReadAllTextAsync(sourceFile);
        
        // Lex
        var lexer = new FlowLangLexer(source);
        var tokens = lexer.ScanTokens();
        
        // Parse
        var parser = new FlowLangParser(tokens);
        var ast = parser.Parse();
        
        // Generate C#
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(ast);
        
        var csharpCode = syntaxTree.ToString();
        
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
        var jsCode = "// JavaScript generation not yet implemented in core compiler\n// Use flowc-dev tool for JavaScript compilation";
        
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, jsCode);
        }
        
        return jsCode;
    }
}

// =============================================================================
// MAIN PROGRAM
// =============================================================================

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return 0;
            }
            
            if (args[0] == "--help" || args[0] == "-h")
            {
                ShowHelp();
                return 0;
            }
            
            if (args[0] == "--version" || args[0] == "-v")
            {
                Console.WriteLine("FlowLang Core Compiler v1.0.0");
                return 0;
            }
            
            // Default: transpile to C#
            if (args.Length >= 2)
            {
                var inputFile = args[0];
                var outputFile = args[1];
                
                // Check for target option
                string target = "csharp";
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] == "--target" && i + 1 < args.Length)
                    {
                        target = args[i + 1];
                        break;
                    }
                }
                
                if (!File.Exists(inputFile))
                {
                    Console.Error.WriteLine($"Error: Input file '{inputFile}' not found");
                    return 1;
                }
                
                var transpiler = new FlowLangTranspiler();
                
                switch (target.ToLowerInvariant())
                {
                    case "csharp":
                    case "cs":
                        await transpiler.TranspileAsync(inputFile, outputFile);
                        Console.WriteLine($"Successfully compiled {inputFile} -> {outputFile}");
                        break;
                    
                    case "javascript":
                    case "js":
                        await transpiler.TranspileToJavaScriptAsync(inputFile, outputFile);
                        Console.WriteLine($"Successfully compiled {inputFile} -> {outputFile} (JavaScript)");
                        break;
                    
                    default:
                        Console.Error.WriteLine($"Error: Unsupported target '{target}'. Supported targets: csharp, javascript");
                        return 1;
                }
                
                return 0;
            }
            else
            {
                Console.Error.WriteLine("Error: Input and output files required");
                ShowHelp();
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
    
    private static void ShowHelp()
    {
        Console.WriteLine("FlowLang Core Compiler - Pure Transpilation");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  flowc-core <input.flow> <output.cs> [--target csharp|javascript]");
        Console.WriteLine("  flowc-core --help");
        Console.WriteLine("  flowc-core --version");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --target    Target language (csharp, javascript)");
        Console.WriteLine("  --help      Show this help message");
        Console.WriteLine("  --version   Show version information");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  flowc-core hello.flow hello.cs");
        Console.WriteLine("  flowc-core app.flow app.js --target javascript");
    }
}