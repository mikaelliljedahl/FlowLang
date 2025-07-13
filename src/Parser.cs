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
        
        if (Match(TokenType.Component))
        {
            return ParseComponentDeclaration();
        }
        
        if (Match(TokenType.AppState))
        {
            return ParseAppStateDeclaration();
        }
        
        if (Match(TokenType.ApiClient))
        {
            return ParseApiClientDeclaration();
        }
        
        if (Match(TokenType.Saga))
        {
            return ParseSagaDeclaration();
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

    // UI Component parsing methods
    private ComponentDeclaration ParseComponentDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected component name").Value;
        
        Consume(TokenType.LeftParen, "Expected '(' after component name");
        var parameters = ParseParameterList();
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        // Parse uses clause (effects)
        var effects = new List<string>();
        if (Match(TokenType.Effects) || Check(TokenType.Identifier) && Peek().Value == "uses")
        {
            if (Peek().Value == "uses") Advance(); // consume 'uses'
            Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
            effects = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after effects");
        }
        
        // Parse state clause
        var stateVariables = new List<string>();
        if (Match(TokenType.State))
        {
            Consume(TokenType.LeftBracket, "Expected '[' after 'state'");
            stateVariables = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after state variables");
        }
        
        // Parse events clause
        var events = new List<string>();
        if (Match(TokenType.Events))
        {
            Consume(TokenType.LeftBracket, "Expected '[' after 'events'");
            events = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after events");
        }
        
        Consume(TokenType.Arrow, "Expected '->' after component signature");
        var returnType = ConsumeType("Expected return type").Value;
        
        Consume(TokenType.LeftBrace, "Expected '{' before component body");
        
        // Parse component body
        var stateDeclarations = new List<StateDeclaration>();
        ComponentLifecycle? lifecycle = null;
        var eventHandlers = new List<EventHandler>();
        UIElement? renderTree = null;
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.DeclareState))
            {
                stateDeclarations.Add(ParseStateDeclaration());
            }
            else if (Match(TokenType.OnMount))
            {
                lifecycle = lifecycle ?? new ComponentLifecycle();
                lifecycle = lifecycle with { OnMount = ParseLifecycleBlock() };
            }
            else if (Match(TokenType.OnUnmount))
            {
                lifecycle = lifecycle ?? new ComponentLifecycle();
                lifecycle = lifecycle with { OnUnmount = ParseLifecycleBlock() };
            }
            else if (Match(TokenType.OnUpdate))
            {
                lifecycle = lifecycle ?? new ComponentLifecycle();
                lifecycle = lifecycle with { OnUpdate = ParseLifecycleBlock() };
            }
            else if (Match(TokenType.EventHandler))
            {
                eventHandlers.Add(ParseEventHandler());
            }
            else if (Match(TokenType.Render))
            {
                renderTree = ParseRenderBlock();
            }
            else
            {
                // Skip unknown tokens for now
                Advance();
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after component body");
        
        // Create a default render tree if none provided
        if (renderTree == null)
        {
            renderTree = new UIElement("div", new Dictionary<string, ASTNode>(), new List<UIElement>(), new List<UIEvent>());
        }
        
        return new ComponentDeclaration(name, parameters, effects, stateVariables, events, returnType, 
            stateDeclarations, lifecycle, eventHandlers, renderTree);
    }
    
    private AppStateDeclaration ParseAppStateDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected app state name").Value;
        
        // Parse uses clause (effects)
        var effects = new List<string>();
        if (Check(TokenType.Identifier) && Peek().Value == "uses")
        {
            Advance(); // consume 'uses'
            Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
            effects = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after effects");
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' before app state body");
        
        var properties = new List<StateProperty>();
        var actions = new List<StateAction>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.Action))
            {
                actions.Add(ParseStateAction());
            }
            else if (Check(TokenType.Identifier))
            {
                // Parse property declaration
                properties.Add(ParseStateProperty());
            }
            else
            {
                Advance(); // Skip unknown tokens
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after app state body");
        
        return new AppStateDeclaration(name, effects, properties, actions);
    }
    
    private ApiClientDeclaration ParseApiClientDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected API client name").Value;
        
        Consume(TokenType.From, "Expected 'from' after API client name");
        var fromService = Consume(TokenType.Identifier, "Expected service name after 'from'").Value;
        
        Consume(TokenType.LeftBrace, "Expected '{' before API client body");
        
        var configuration = new Dictionary<string, ASTNode>();
        var methods = new List<ApiMethod>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.Function))
            {
                methods.Add(ParseApiMethod());
            }
            else if (Check(TokenType.Identifier))
            {
                // Parse configuration property
                var key = Advance().Value;
                Consume(TokenType.Colon, "Expected ':' after configuration key");
                var value = ParseExpression();
                configuration[key] = value;
            }
            else
            {
                Advance(); // Skip unknown tokens
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after API client body");
        
        return new ApiClientDeclaration(name, fromService, configuration, methods);
    }
    
    private SagaDeclaration ParseSagaDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected saga name").Value;
        
        Consume(TokenType.LeftBrace, "Expected '{' before saga body");
        
        var steps = new List<SagaStep>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.Step))
            {
                steps.Add(ParseSagaStep());
            }
            else
            {
                Advance(); // Skip unknown tokens
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after saga body");
        
        return new SagaDeclaration(name, steps);
    }
    
    // Helper parsing methods
    private List<Parameter> ParseParameterList()
    {
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
        
        return parameters;
    }
    
    private List<string> ParseStringList()
    {
        var strings = new List<string>();
        
        if (!Check(TokenType.RightBracket))
        {
            do
            {
                var value = Consume(TokenType.Identifier, "Expected identifier").Value;
                strings.Add(value);
            }
            while (Match(TokenType.Comma));
        }
        
        return strings;
    }
    
    private StateDeclaration ParseStateDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected state variable name").Value;
        Consume(TokenType.Colon, "Expected ':' after state variable name");
        var type = ConsumeType("Expected state variable type").Value;
        
        ASTNode? initialValue = null;
        if (Match(TokenType.Assign))
        {
            initialValue = ParseExpression();
        }
        
        return new StateDeclaration(name, type, initialValue);
    }
    
    private ASTNode ParseLifecycleBlock()
    {
        Consume(TokenType.LeftBrace, "Expected '{' after lifecycle method");
        
        var statements = new List<ASTNode>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            // Parse lifecycle statements (simplified for now)
            if (Match(TokenType.SetState))
            {
                statements.Add(ParseSetStateCall());
            }
            else
            {
                // Parse other statements
                var expr = ParseExpression();
                statements.Add(expr);
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after lifecycle block");
        
        // Return a simple identifier for now - would need a StatementBlock AST node
        return new Identifier("lifecycle_block");
    }
    
    private EventHandler ParseEventHandler()
    {
        var name = Consume(TokenType.Identifier, "Expected event handler name").Value;
        
        Consume(TokenType.LeftParen, "Expected '(' after event handler name");
        var parameters = ParseParameterList();
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        // Parse uses clause (effects)
        var effects = new List<string>();
        if (Check(TokenType.Identifier) && Peek().Value == "uses")
        {
            Advance(); // consume 'uses'
            Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
            effects = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after effects");
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' before event handler body");
        
        var body = new List<ASTNode>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.SetState))
            {
                body.Add(ParseSetStateCall());
            }
            else if (Match(TokenType.Return))
            {
                var expr = ParseExpression();
                body.Add(new ReturnStatement(expr));
            }
            else
            {
                var expr = ParseExpression();
                body.Add(expr);
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after event handler body");
        
        return new EventHandler(name, parameters, effects, body);
    }
    
    private UIElement ParseRenderBlock()
    {
        Consume(TokenType.LeftBrace, "Expected '{' after 'render'");
        
        // Parse render content - may include conditionals, loops, or regular elements
        var renderItems = new List<ASTNode>();
        
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Check(TokenType.If))
            {
                Match(TokenType.If);
                renderItems.Add(ParseConditionalRender());
            }
            else if (Check(TokenType.For))
            {
                Match(TokenType.For);
                renderItems.Add(ParseLoopRender());
            }
            else
            {
                renderItems.Add(ParseUIElement());
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after render block");
        
        // If we have multiple items, wrap them in a fragment-like container
        if (renderItems.Count == 0)
        {
            return new UIElement("div", new Dictionary<string, ASTNode>(), new List<UIElement>(), new List<UIEvent>());
        }
        else if (renderItems.Count == 1 && renderItems[0] is UIElement singleElement)
        {
            return singleElement;
        }
        else
        {
            // Create a container element that holds all render items
            var containerElement = new UIElement("fragment", new Dictionary<string, ASTNode>(), new List<UIElement>(), new List<UIEvent>());
            
            // Convert render items to children if they are UIElements
            foreach (var item in renderItems)
            {
                if (item is UIElement element)
                {
                    containerElement.Children.Add(element);
                }
                // For conditional/loop renders, we'll handle them in code generation
            }
            
            return containerElement;
        }
    }
    
    private UIElement ParseUIElement()
    {
        var tagName = "div"; // Default
        
        if (Check(TokenType.Container) || Check(TokenType.Identifier))
        {
            tagName = Advance().Value;
        }
        
        var attributes = new Dictionary<string, ASTNode>();
        var events = new List<UIEvent>();
        
        // Parse attributes if present
        if (Match(TokenType.LeftParen))
        {
            while (!Check(TokenType.RightParen) && !IsAtEnd())
            {
                var attrName = Consume(TokenType.Identifier, "Expected attribute name").Value;
                Consume(TokenType.Colon, "Expected ':' after attribute name");
                var attrValue = ParsePrimaryExpression(); // Use simple expression parser for now
                attributes[attrName] = attrValue;
                
                if (!Check(TokenType.RightParen))
                {
                    Match(TokenType.Comma); // Optional comma
                }
            }
            Consume(TokenType.RightParen, "Expected ')' after attributes");
        }
        
        var children = new List<UIElement>();
        
        // Parse children if present
        if (Match(TokenType.LeftBrace))
        {
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                if (Match(TokenType.Newline)) continue;
                
                // Check for conditional or loop rendering in children
                if (Check(TokenType.If))
                {
                    Match(TokenType.If);
                    var conditionalRender = ParseConditionalRender();
                    // Convert to child element - this will be handled in code generation
                    children.Add(new UIElement("conditional-child", new Dictionary<string, ASTNode>(), new List<UIElement>(), new List<UIEvent>()));
                }
                else if (Check(TokenType.For))
                {
                    Match(TokenType.For);
                    var loopRender = ParseLoopRender();
                    // Convert to child element - this will be handled in code generation
                    children.Add(new UIElement("loop-child", new Dictionary<string, ASTNode>(), new List<UIElement>(), new List<UIEvent>()));
                }
                else
                {
                    children.Add(ParseUIElement());
                }
            }
            Consume(TokenType.RightBrace, "Expected '}' after UI element children");
        }
        
        return new UIElement(tagName, attributes, children, events);
    }
    
    private SetStateCall ParseSetStateCall()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'set_state'");
        var stateName = Consume(TokenType.Identifier, "Expected state variable name").Value;
        Consume(TokenType.Comma, "Expected ',' after state variable name");
        var value = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after set_state call");
        
        return new SetStateCall(stateName, value);
    }
    
    private StateProperty ParseStateProperty()
    {
        var name = Consume(TokenType.Identifier, "Expected property name").Value;
        Consume(TokenType.Colon, "Expected ':' after property name");
        var type = ConsumeType("Expected property type").Value;
        
        ASTNode? defaultValue = null;
        if (Match(TokenType.Assign))
        {
            defaultValue = ParseExpression();
        }
        
        return new StateProperty(name, type, defaultValue);
    }
    
    private StateAction ParseStateAction()
    {
        var name = Consume(TokenType.Identifier, "Expected action name").Value;
        
        Consume(TokenType.LeftParen, "Expected '(' after action name");
        var parameters = ParseParameterList();
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        // Parse uses clause
        var effects = new List<string>();
        if (Check(TokenType.Identifier) && Peek().Value == "uses")
        {
            Advance();
            Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
            effects = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after effects");
        }
        
        // Parse updates clause
        var updatedProperties = new List<string>();
        if (Match(TokenType.Updates))
        {
            Consume(TokenType.LeftBracket, "Expected '[' after 'updates'");
            updatedProperties = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after updated properties");
        }
        
        Consume(TokenType.Arrow, "Expected '->' after action signature");
        var returnType = ConsumeType("Expected return type").Value;
        
        Consume(TokenType.LeftBrace, "Expected '{' before action body");
        
        var body = new List<ASTNode>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.Return))
            {
                var expr = ParseExpression();
                body.Add(new ReturnStatement(expr));
            }
            else
            {
                var expr = ParseExpression();
                body.Add(expr);
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after action body");
        
        return new StateAction(name, parameters, effects, updatedProperties, returnType, body);
    }
    
    private ApiMethod ParseApiMethod()
    {
        var name = Consume(TokenType.Identifier, "Expected API method name").Value;
        
        Consume(TokenType.LeftParen, "Expected '(' after API method name");
        var parameters = ParseParameterList();
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        // Parse uses clause
        var effects = new List<string>();
        if (Check(TokenType.Identifier) && Peek().Value == "uses")
        {
            Advance();
            Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
            effects = ParseStringList();
            Consume(TokenType.RightBracket, "Expected ']' after effects");
        }
        
        Consume(TokenType.Arrow, "Expected '->' after API method signature");
        var returnType = ConsumeType("Expected return type").Value;
        
        return new ApiMethod(name, parameters, effects, returnType);
    }
    
    private SagaStep ParseSagaStep()
    {
        var name = Consume(TokenType.Identifier, "Expected saga step name").Value;
        
        Consume(TokenType.LeftParen, "Expected '(' after saga step name");
        var parameters = ParseParameterList();
        Consume(TokenType.RightParen, "Expected ')' after parameters");
        
        Consume(TokenType.LeftBrace, "Expected '{' before saga step body");
        
        var body = new List<ASTNode>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline)) continue;
            
            if (Match(TokenType.Return))
            {
                var expr = ParseExpression();
                body.Add(new ReturnStatement(expr));
            }
            else
            {
                var expr = ParseExpression();
                body.Add(expr);
            }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' after saga step body");
        
        // For now, create a simple body node - would need a StatementBlock AST node
        var bodyNode = body.Count > 0 ? body[0] : new Identifier("empty_body");
        
        return new SagaStep(name, parameters, bodyNode);
    }

    // Advanced UI parsing methods for LLM-friendly components
    private ASTNode ParseConditionalRender()
    {
        // Parse: if condition { element } [else { element }]
        var condition = ParseExpression();
        
        Consume(TokenType.LeftBrace, "Expected '{' after if condition");
        var thenElement = ParseUIElementOrRenderContent();
        Consume(TokenType.RightBrace, "Expected '}' after if block");
        
        UIElement? elseElement = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after else");
            elseElement = ParseUIElementOrRenderContent();
            Consume(TokenType.RightBrace, "Expected '}' after else block");
        }
        
        return new ConditionalRender(condition, thenElement, elseElement);
    }

    private ASTNode ParseLoopRender()
    {
        // Parse: for item in collection [where condition] { template }
        var itemName = Consume(TokenType.Identifier, "Expected iterator variable name").Value;
        Consume(TokenType.In, "Expected 'in' after iterator variable");
        var collection = ParseExpression();
        
        ASTNode? whereCondition = null;
        if (Match(TokenType.Where))
        {
            whereCondition = ParseExpression();
        }
        
        Consume(TokenType.LeftBrace, "Expected '{' after for loop header");
        var template = ParseUIElementOrRenderContent();
        Consume(TokenType.RightBrace, "Expected '}' after for loop body");
        
        return new IterativeRender(itemName, collection, template, whereCondition);
    }

    private UIElement ParseUIElementOrRenderContent()
    {
        // This method handles parsing either a single UI element or multiple render items
        if (Check(TokenType.If))
        {
            Match(TokenType.If);
            var conditionalRender = (ConditionalRender)ParseConditionalRender();
            // Convert to UIElement wrapper for consistency
            return new UIElement("conditional", new Dictionary<string, ASTNode>(), new List<UIElement>(), new List<UIEvent>());
        }
        else if (Check(TokenType.For))
        {
            Match(TokenType.For);
            var loopRender = (IterativeRender)ParseLoopRender();
            // Convert to UIElement wrapper for consistency
            return new UIElement("loop", new Dictionary<string, ASTNode>(), new List<UIElement>(), new List<UIEvent>());
        }
        else
        {
            return ParseUIElement();
        }
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
            var thenExpr = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after ternary then expression");
            var elseExpr = ParseExpression();
            return new TernaryExpression(expr, thenExpr, elseExpr);
        }
        
        return expr;
    }

    private ASTNode ParseLogicalOrExpression()
    {
        var expr = ParseLogicalAndExpression();
        
        while (Match(TokenType.LogicalOr))
        {
            var op = Previous().Value;
            var right = ParseLogicalAndExpression();
            expr = new LogicalExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseLogicalAndExpression()
    {
        var expr = ParseEqualityExpression();
        
        while (Match(TokenType.LogicalAnd))
        {
            var op = Previous().Value;
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
            var op = Previous().Value;
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
            var op = Previous().Value;
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
            var op = Previous().Value;
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
            var op = Previous().Value;
            var right = ParseUnaryExpression();
            expr = new ArithmeticExpression(expr, op, right);
        }
        
        return expr;
    }

    private ASTNode ParseUnaryExpression()
    {
        if (Match(TokenType.LogicalNot, TokenType.Minus))
        {
            var op = Previous().Value;
            var expr = ParseUnaryExpression();
            return new UnaryExpression(op, expr);
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
                var memberName = Consume(TokenType.Identifier, "Expected property name after '.'").Value;
                
                // Check if this is a method call
                if (Check(TokenType.LeftParen))
                {
                    Match(TokenType.LeftParen);
                    var arguments = new List<ASTNode>();
                    
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            arguments.Add(ParseExpression());
                        }
                        while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightParen, "Expected ')' after method arguments");
                    expr = new MethodCallExpression(expr, memberName, arguments);
                }
                else
                {
                    expr = new MemberAccessExpression(expr, memberName);
                }
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
            return new NumberLiteral(int.Parse(Previous().Value));
        }
        
        if (Match(TokenType.String))
        {
            return new StringLiteral(Previous().Value);
        }
        
        if (Match(TokenType.InterpolatedString))
        {
            // For now, treat interpolated strings as regular strings
            // In a full implementation, we'd parse the interpolation expressions
            return new StringLiteral(Previous().Value);
        }
        
        if (Match(TokenType.Identifier))
        {
            var name = Previous().Value;
            
            // Check for boolean literals
            if (name == "true") return new BooleanLiteral(true);
            if (name == "false") return new BooleanLiteral(false);
            
            return new Identifier(name);
        }
        
        if (Match(TokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return expr;
        }
        
        throw new Exception($"Unexpected token '{Peek().Value}' at line {Peek().Line}, column {Peek().Column}");
    }

}