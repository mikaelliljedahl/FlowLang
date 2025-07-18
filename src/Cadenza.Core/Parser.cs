// Cadenza Core Parser - Extracted from cadenzac-core.cs
// Handles parsing of Cadenza language tokens into AST nodes

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

    public ProgramNode Parse()
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
        
        return new ProgramNode(statements);
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
        if (Match(TokenType.Match))
            return ParseMatchExpression();

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
                // Handle wildcard imports: import Utils.*
                if (Check(TokenType.Multiply))
                {
                    Advance();
                    isWildcard = true;
                }
                else
                {
                    // Handle specific imports: import Utils.{add, subtract}
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
            // Handle else if
            if (Match(TokenType.If))
            {
                // Parse the else if as a nested if statement
                var elseIfCondition = ParseExpression();
                
                Consume(TokenType.LeftBrace, "Expected '{' after else if condition");
                var elseIfThenBody = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after else if body");
                
                List<ASTNode>? elseIfElseBody = null;
                if (Match(TokenType.Else))
                {
                    // Handle nested else if or else
                    if (Match(TokenType.If))
                    {
                        // Recursively parse more else if statements
                        var nestedElseIfCondition = ParseExpression();
                        
                        Consume(TokenType.LeftBrace, "Expected '{' after nested else if condition");
                        var nestedElseIfThenBody = ParseStatements();
                        Consume(TokenType.RightBrace, "Expected '}' after nested else if body");
                        
                        var nestedElseIf = new IfStatement(nestedElseIfCondition, nestedElseIfThenBody, null);
                        elseIfElseBody = new List<ASTNode> { nestedElseIf };
                    }
                    else
                    {
                        Consume(TokenType.LeftBrace, "Expected '{' after else");
                        elseIfElseBody = ParseStatements();
                        Consume(TokenType.RightBrace, "Expected '}' after else body");
                    }
                }
                
                var elseIfStatement = new IfStatement(elseIfCondition, elseIfThenBody, elseIfElseBody);
                elseBody = new List<ASTNode> { elseIfStatement };
            }
            else
            {
                Consume(TokenType.LeftBrace, "Expected '{' after else");
                elseBody = ParseStatements();
                Consume(TokenType.RightBrace, "Expected '}' after else body");
            }
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
            string? variable = null;
            
            if (Check(TokenType.Ok) || Check(TokenType.Error) || Check(TokenType.Some) || Check(TokenType.None))
            {
                pattern = Advance().Lexeme;
                
                if (Match(TokenType.LeftParen))
                {
                    variable = Consume(TokenType.Identifier, "Expected variable name in pattern").Lexeme;
                    Consume(TokenType.RightParen, "Expected ')' after pattern variable");
                }
            }
            else if (Check(TokenType.Number) || Check(TokenType.String))
            {
                pattern = Advance().Literal?.ToString() ?? "";
            }
            else if (Check(TokenType.Identifier))
            {
                pattern = Advance().Lexeme;
                // Handle wildcard '_' or other identifiers
                if (pattern == "_")
                {
                    // Wildcard pattern
                }
                else
                {
                    // Could be a constructor pattern or variable binding
                    if (Match(TokenType.LeftParen))
                    {
                        variable = Consume(TokenType.Identifier, "Expected variable name in pattern").Lexeme;
                        Consume(TokenType.RightParen, "Expected ')' after pattern variable");
                    }
                }
            }
            else
            {
                throw new Exception($"Expected pattern in match case. Got '{Peek().Lexeme}' at line {Peek().Line}");
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