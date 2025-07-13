using System.Collections.Generic;

namespace FlowLang.Compiler;

// AST Node definitions
public abstract record ASTNode;

public record Program(List<ASTNode> Statements) : ASTNode;

public record FunctionDeclaration(string Name, List<Parameter> Parameters, string ReturnType, List<ASTNode> Body, List<string>? Effects = null) : ASTNode;

public record Parameter(string Name, string Type);

public record ReturnStatement(ASTNode Expression) : ASTNode;

public record BinaryExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;

public record Identifier(string Name) : ASTNode;

public record NumberLiteral(int Value) : ASTNode;

// Saga-related AST nodes
public record SagaDeclaration(string Name, List<SagaStep> Steps) : ASTNode;

public record SagaStep(string Name, List<Parameter> Parameters, ASTNode Body, ASTNode? CompensationBody = null) : ASTNode;

public record TransactionBlock(List<ASTNode> Steps) : ASTNode;

public record CompensationBlock(List<ASTNode> Steps) : ASTNode;

// UI Component AST nodes - designed for LLM clarity and predictability
public record ComponentDeclaration(
    string Name, 
    List<Parameter> Parameters, 
    List<string> Effects, 
    List<string> StateVariables,
    List<string> Events,
    string ReturnType,
    List<StateDeclaration> StateDeclarations,
    ComponentLifecycle? Lifecycle,
    List<EventHandler> EventHandlers,
    UIElement RenderTree
) : ASTNode;

public record StateDeclaration(string Name, string Type, ASTNode? InitialValue = null) : ASTNode;

public record ComponentLifecycle(
    ASTNode? OnMount = null,
    ASTNode? OnUnmount = null,
    ASTNode? OnUpdate = null
) : ASTNode;

public record EventHandler(string Name, List<Parameter> Parameters, List<string> Effects, List<ASTNode> Body) : ASTNode;

public record UIElement(
    string TagName,
    Dictionary<string, ASTNode> Attributes,
    List<UIElement> Children,
    List<UIEvent> Events
) : ASTNode;

public record UIEvent(string EventType, string HandlerName, List<ASTNode> Parameters) : ASTNode;

public record SetStateCall(string StateName, ASTNode Value) : ASTNode;

// Advanced UI rendering AST nodes for LLM-friendly development
public record ConditionalRender(ASTNode Condition, UIElement ThenElement, UIElement? ElseElement = null) : ASTNode;

public record IterativeRender(string ItemName, ASTNode Collection, UIElement Template, ASTNode? WhereCondition = null) : ASTNode;

// Complex expression AST nodes
public record TernaryExpression(ASTNode Condition, ASTNode ThenExpr, ASTNode ElseExpr) : ASTNode;

public record LogicalExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode; // &&, ||

public record ComparisonExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode; // ==, !=, <, >, <=, >=

public record ArithmeticExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode; // +, -, *, /, %

public record UnaryExpression(string Operator, ASTNode Operand) : ASTNode; // !, -

public record MemberAccessExpression(ASTNode Object, string MemberName) : ASTNode; // obj.prop

public record MethodCallExpression(ASTNode Object, string MethodName, List<ASTNode> Arguments) : ASTNode; // obj.method(args)

public record StringLiteral(string Value) : ASTNode;

public record BooleanLiteral(bool Value) : ASTNode;

public record StringInterpolation(List<StringInterpolationPart> Parts) : ASTNode;

public record StringInterpolationPart(string Text, ASTNode? Expression = null) : ASTNode; // For $"text {expr}"

// Enhanced UI element with render content support
public record RenderContent : ASTNode
{
    public List<ASTNode> Items { get; init; } = new();
}

// Component composition AST node
public record ComponentInstance(string ComponentName, Dictionary<string, ASTNode> Props, List<ASTNode>? Children = null) : ASTNode;

// State management AST nodes
public record AppStateDeclaration(
    string Name,
    List<string> Effects,
    List<StateProperty> Properties,
    List<StateAction> Actions
) : ASTNode;

public record StateProperty(string Name, string Type, ASTNode? DefaultValue = null) : ASTNode;

public record StateAction(
    string Name,
    List<Parameter> Parameters,
    List<string> Effects,
    List<string> UpdatedProperties,
    string ReturnType,
    List<ASTNode> Body
) : ASTNode;

// API Client generation AST nodes
public record ApiClientDeclaration(
    string Name,
    string FromService,
    Dictionary<string, ASTNode> Configuration,
    List<ApiMethod> Methods
) : ASTNode;

public record ApiMethod(
    string Name,
    List<Parameter> Parameters,
    List<string> Effects,
    string ReturnType
) : ASTNode;