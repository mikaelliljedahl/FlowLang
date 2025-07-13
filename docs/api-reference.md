# FlowLang Transpiler API Reference

This document provides a comprehensive reference for the FlowLang transpiler's internal API, designed for developers who want to understand or extend the transpiler implementation.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Components](#core-components)
3. [Lexical Analysis (Lexer)](#lexical-analysis-lexer)
4. [Syntax Analysis (Parser)](#syntax-analysis-parser)
5. [Abstract Syntax Tree (AST)](#abstract-syntax-tree-ast)
6. [Code Generation](#code-generation)
7. [Effect System](#effect-system)
8. [Module System](#module-system)
9. [CLI Framework](#cli-framework)
10. [Configuration System](#configuration-system)
11. [Testing Infrastructure](#testing-infrastructure)
12. [Phase 2 Components](#phase-2-components)
13. [Extension Points](#extension-points)

## Architecture Overview

The FlowLang transpiler follows a classic compiler architecture with these phases:

```
FlowLang Source → Lexer → Parser → AST → Code Generator → C# Code
```

### Main Components

| Component | Responsibility | Input | Output |
|-----------|---------------|-------|--------|
| **Lexer** | Tokenization | FlowLang source | Token stream |
| **Parser** | Syntax analysis | Tokens | AST |
| **Type Checker** | Type validation | AST | Validated AST |
| **Code Generator** | C# generation | AST | C# syntax tree |
| **CLI** | User interface | Commands | Execution |

### Core Classes

```csharp
// Main transpiler class
public class FlowLangTranspiler
{
    public string TranspileToCS(string flowLangSource)
    public async Task TranspileAsync(string inputPath, string? outputPath = null)
}

// Core pipeline components
public class FlowLangLexer
public class FlowLangParser  
public class CSharpGenerator
```

## Core Components

### FlowLangTranspiler Class

The main entry point for transpilation operations.

```csharp
public class FlowLangTranspiler
{
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
    
    public async Task TranspileAsync(string inputPath, string? outputPath = null)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input file not found: {inputPath}");

        var flowLangSource = await File.ReadAllTextAsync(inputPath);
        var csharpCode = TranspileToCS(flowLangSource);

        if (outputPath == null)
            outputPath = Path.ChangeExtension(inputPath, ".cs");

        await File.WriteAllTextAsync(outputPath, csharpCode);
    }
}
```

## Lexical Analysis (Lexer)

### Token Types

```csharp
public enum TokenType
{
    // Literals
    Identifier, Number, String, StringInterpolation,
    
    // Keywords  
    Function, Return, If, Else, Effects, Pure, Uses, Result, Ok, Error, Let,
    
    // Effect names
    Database, Network, Logging, FileSystem, Memory, IO,
    
    // Types
    Int, String_Type, Bool,
    
    // Symbols
    LeftParen, RightParen, LeftBrace, RightBrace, LeftBracket, RightBracket,
    Comma, Semicolon, Colon, Arrow, Assign,
    
    // Operators
    Plus, Minus, Multiply, Divide, Greater, Less, GreaterEqual, LessEqual,
    Equal, NotEqual, Question, And, Or, Not,
    
    // Control flow
    Guard,
    
    // Module system
    Module, Import, Export, From, Dot,
    
    // Special
    EOF, Newline
}
```

### Token Record

```csharp
public record Token(TokenType Type, string Value, int Line, int Column);
```

### FlowLangLexer Class

```csharp
public class FlowLangLexer
{
    private readonly string _source;
    private int _position = 0;
    private int _line = 1;
    private int _column = 1;
    private readonly Dictionary<string, TokenType> _keywords;

    public FlowLangLexer(string source)
    {
        _source = source;
        // Initialize keyword dictionary
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        
        while (!IsAtEnd())
        {
            var token = NextToken();
            if (token != null)
                tokens.Add(token);
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
            // ... other character mappings
            _ when char.IsDigit(c) => ReadNumber(c, startLine, startColumn),
            _ when char.IsLetter(c) || c == '_' => ReadIdentifier(c, startLine, startColumn),
            _ => throw new Exception($"Unexpected character '{c}' at line {startLine}, column {startColumn}")
        };
    }

    // Helper methods for tokenization
    private Token ReadNumber(char firstChar, int line, int column) { /* ... */ }
    private Token ReadString(int line, int column) { /* ... */ }
    private Token ReadStringInterpolation(int line, int column) { /* ... */ }
    private Token ReadIdentifier(char firstChar, int line, int column) { /* ... */ }
}
```

## Syntax Analysis (Parser)

### FlowLangParser Class

```csharp
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
                statements.Add(stmt);
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
        
        if (Match(TokenType.Function))
            return ParseFunctionDeclaration();
        
        if (Match(TokenType.Pure))
        {
            _current--;
            return ParseFunctionDeclaration();
        }
        
        return null;
    }

    // Parsing methods for different constructs
    private FunctionDeclaration ParseFunctionDeclaration() { /* ... */ }
    private EffectAnnotation ParseEffectAnnotation() { /* ... */ }
    private IfStatement ParseIfStatement() { /* ... */ }
    private GuardStatement ParseGuardStatement() { /* ... */ }
    private ModuleDeclaration ParseModuleDeclaration() { /* ... */ }
    private ImportStatement ParseImportStatement() { /* ... */ }
    private ExportStatement ParseExportStatement() { /* ... */ }
    
    // Expression parsing with operator precedence
    private ASTNode ParseExpression() { /* ... */ }
    private ASTNode ParseLogicalOr() { /* ... */ }
    private ASTNode ParseLogicalAnd() { /* ... */ }
    private ASTNode ParseEquality() { /* ... */ }
    private ASTNode ParseComparison() { /* ... */ }
    private ASTNode ParseAddition() { /* ... */ }
    private ASTNode ParseMultiplication() { /* ... */ }
    private ASTNode ParseUnary() { /* ... */ }
    private ASTNode ParsePrimary() { /* ... */ }
}
```

### Parser Utility Methods

```csharp
public class FlowLangParser
{
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
}
```

## Abstract Syntax Tree (AST)

### Base AST Node

```csharp
public abstract record ASTNode;
```

### Program and Top-level Nodes

```csharp
public record Program(List<ASTNode> Statements) : ASTNode;

// Module system AST nodes
public record ModuleDeclaration(string Name, List<ASTNode> Body, List<string>? Exports = null) : ASTNode;
public record ImportStatement(string ModuleName, List<string>? SpecificImports = null, bool IsWildcard = false) : ASTNode;
public record ExportStatement(List<string> ExportedNames) : ASTNode;
public record QualifiedName(string ModuleName, string Name) : ASTNode;
```

### Function-related Nodes

```csharp
public record FunctionDeclaration(
    string Name, 
    List<Parameter> Parameters, 
    string ReturnType, 
    List<ASTNode> Body, 
    EffectAnnotation? Effects = null, 
    bool IsPure = false
) : ASTNode;

public record Parameter(string Name, string Type);
public record EffectAnnotation(List<string> Effects) : ASTNode;
```

### Statement Nodes

```csharp
public record ReturnStatement(ASTNode Expression) : ASTNode;
public record LetStatement(string Name, ASTNode Expression) : ASTNode;
public record IfStatement(ASTNode Condition, List<ASTNode> ThenBody, List<ASTNode>? ElseBody = null) : ASTNode;
public record GuardStatement(ASTNode Condition, List<ASTNode> ElseBody) : ASTNode;
```

### Expression Nodes

```csharp
public record BinaryExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;
public record UnaryExpression(string Operator, ASTNode Operand) : ASTNode;
public record Identifier(string Name) : ASTNode;
public record NumberLiteral(int Value) : ASTNode;
public record StringLiteral(string Value) : ASTNode;
public record StringInterpolation(List<ASTNode> Parts) : ASTNode;
public record FunctionCall(string Name, List<ASTNode> Arguments) : ASTNode;
```

### Result Type Nodes

```csharp
public record ResultType(string OkType, string ErrorType) : ASTNode;
public record OkExpression(ASTNode Value) : ASTNode;
public record ErrorExpression(ASTNode Value) : ASTNode;
public record ErrorPropagationExpression(ASTNode Expression) : ASTNode;
```

## Code Generation

### CSharpGenerator Class

```csharp
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
                    globalMembers.Add(GenerateMethod(func));
                    break;
                    
                case ExportStatement export:
                    // Export statements are handled within modules
                    break;
            }
        }
        
        // Build compilation unit
        return BuildCompilationUnit(globalMembers, namespaceMembers);
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
                bodyStatements.AddRange(block.Statements);
            }
            else
            {
                bodyStatements.Add(generated);
            }
        }
        
        // Generate XML documentation comment
        var xmlDocComment = GenerateXmlDocComment(func);
        
        return MethodDeclaration(ParseTypeName(func.ReturnType), func.Name)
            .WithModifiers(TokenList(GetMethodModifiers(isPublic)))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithBody(Block(bodyStatements))
            .WithLeadingTrivia(xmlDocComment);
    }
}
```

### Statement Generation

```csharp
public class CSharpGenerator
{
    private StatementSyntax GenerateStatement(ASTNode node)
    {
        return node switch
        {
            ReturnStatement ret => ReturnStatement(GenerateExpression(ret.Expression)),
            LetStatement let => GenerateLetStatement(let),
            IfStatement ifStmt => GenerateIfStatement(ifStmt),
            GuardStatement guardStmt => GenerateGuardStatement(guardStmt),
            _ => throw new NotImplementedException($"Statement type {node.GetType().Name} not implemented")
        };
    }

    private StatementSyntax GenerateLetStatement(LetStatement let)
    {
        if (let.Expression is ErrorPropagationExpression errorProp)
        {
            // Generate error propagation handling
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
            
            return Block(tempVarDeclaration, errorCheck, valueExtraction);
        }
        else
        {
            return LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(let.Name)
                            .WithInitializer(EqualsValueClause(GenerateExpression(let.Expression))))));
        }
    }
}
```

### Expression Generation

```csharp
public class CSharpGenerator
{
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
            OkExpression ok => GenerateOkExpression(ok),
            ErrorExpression err => GenerateErrorExpression(err),
            FunctionCall func => GenerateFunctionCall(func),
            QualifiedName qualified => GenerateQualifiedName(qualified),
            ErrorPropagationExpression prop => GenerateErrorPropagation(prop),
            _ => throw new NotImplementedException($"Expression type {node.GetType().Name} not implemented")
        };
    }
}
```

## Effect System

### Effect Validator

```csharp
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
```

### Effect Annotation Processing

The parser validates effect annotations when parsing function declarations:

```csharp
private EffectAnnotation ParseEffectAnnotation()
{
    Consume(TokenType.LeftBracket, "Expected '[' after 'uses'");
    
    var effects = new List<string>();
    
    if (!Check(TokenType.RightBracket))
    {
        do
        {
            var effectToken = Peek();
            if (IsValidEffectToken(effectToken.Type))
            {
                effects.Add(Advance().Value);
            }
            else
            {
                throw new Exception($"Expected effect name, got: {effectToken.Value}");
            }
        }
        while (Match(TokenType.Comma));
    }
    
    Consume(TokenType.RightBracket, "Expected ']' after effect list");
    
    EffectValidator.ValidateEffects(effects);
    
    return new EffectAnnotation(effects);
}
```

## Module System

### Module Processing

```csharp
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

private ClassDeclarationSyntax GenerateModuleClass(ModuleDeclaration module)
{
    var methods = new List<MemberDeclarationSyntax>();
    var exportedFunctions = new HashSet<string>();
    
    // Collect exported function names
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
    
    return ClassDeclaration($"{module.Name}Module")
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
        .WithMembers(List(methods));
}
```

## CLI Framework

### Command Base Class

```csharp
public abstract class Command
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Task<int> ExecuteAsync(string[] args);
}
```

### Command Implementations

```csharp
public class NewCommand : Command
{
    public override string Name => "new";
    public override string Description => "Create a new FlowLang project";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Project name is required");
            return 1;
        }

        var projectName = args[0];
        await CreateProjectStructure(projectName);
        return 0;
    }

    private async Task CreateProjectStructure(string projectName)
    {
        // Implementation for creating project structure
    }
}

public class BuildCommand : Command
{
    public override string Name => "build";
    public override string Description => "Build the current FlowLang project";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var config = await LoadConfig();
        var transpiler = new FlowLangTranspiler();

        // Build implementation
        return 0;
    }
}
```

### CLI Application Main

```csharp
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

        // Handle version and help flags
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            Console.WriteLine("FlowLang Transpiler (flowc) v1.0.0");
            return 0;
        }

        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            await Commands["help"].ExecuteAsync(Array.Empty<string>());
            return 0;
        }

        // Execute command
        var commandName = args[0];
        if (Commands.TryGetValue(commandName, out var command))
        {
            var commandArgs = args.Skip(1).ToArray();
            return await command.ExecuteAsync(commandArgs);
        }

        Console.WriteLine($"Unknown command: {commandName}");
        return 1;
    }
}
```

## Configuration System

### Configuration Records

```csharp
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
```

### Configuration Loading

```csharp
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
```

## Testing Infrastructure

### Test Framework Components

The testing infrastructure provides multiple types of tests:

- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end transpilation
- **Golden File Tests**: Input/expected output validation
- **Performance Tests**: Speed and memory benchmarks
- **Regression Tests**: Prevention of breaking changes

### Test Organization

```csharp
// Base test class
[TestFixture]
public abstract class FlowLangTestBase
{
    protected FlowLangTranspiler Transpiler { get; private set; }
    
    [SetUp]
    public void SetUp()
    {
        Transpiler = new FlowLangTranspiler();
    }
}

// Unit test example
[TestFixture]
public class LexerTests : FlowLangTestBase
{
    [Test]
    public void Lexer_ShouldTokenizeNumbers()
    {
        var lexer = new FlowLangLexer("42");
        var tokens = lexer.Tokenize();
        
        Assert.That(tokens.Count, Is.EqualTo(2)); // Number + EOF
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Value, Is.EqualTo("42"));
    }
}
```

## Phase 2 Components

### Language Server Protocol (LSP)

The LSP implementation provides real-time IDE integration with comprehensive language services.

#### FlowLangLanguageServer Class

```csharp
public class FlowLangLanguageServer
{
    private readonly DocumentManager _documentManager;
    private readonly DiagnosticsProvider _diagnosticsProvider;
    private readonly CompletionProvider _completionProvider;
    private readonly HoverProvider _hoverProvider;
    private readonly DefinitionProvider _definitionProvider;

    public async Task StartAsync()
    {
        // Initialize JSON-RPC communication
        // Register LSP capabilities
        // Start message processing loop
    }
    
    public Task<CompletionList> GetCompletionsAsync(CompletionParams parameters)
    public Task<Hover> GetHoverAsync(HoverParams parameters)
    public Task<Location[]> GetDefinitionAsync(DefinitionParams parameters)
}
```

#### Document Management

```csharp
public class DocumentManager
{
    private readonly Dictionary<string, FlowLangDocument> _documents;
    
    public void OpenDocument(string uri, string text)
    public void UpdateDocument(string uri, TextDocumentContentChangeEvent[] changes)
    public void CloseDocument(string uri)
    public FlowLangDocument GetDocument(string uri)
}

public class FlowLangDocument
{
    public string Uri { get; }
    public string Text { get; }
    public List<Token> Tokens { get; }
    public ASTNode AST { get; }
    public List<Diagnostic> Diagnostics { get; }
}
```

#### Diagnostics Provider

```csharp
public class DiagnosticsProvider
{
    public List<Diagnostic> GetDiagnostics(FlowLangDocument document)
    {
        var diagnostics = new List<Diagnostic>();
        
        // Syntax error detection
        diagnostics.AddRange(GetSyntaxDiagnostics(document));
        
        // Effect system validation
        diagnostics.AddRange(GetEffectDiagnostics(document));
        
        // Result type validation
        diagnostics.AddRange(GetResultDiagnostics(document));
        
        return diagnostics;
    }
}
```

### Static Analysis Engine

The static analysis system provides comprehensive code quality and security analysis.

#### StaticAnalyzer Class

```csharp
public class StaticAnalyzer
{
    private readonly LintRuleEngine _ruleEngine;
    private readonly EffectAnalyzer _effectAnalyzer;
    private readonly ResultTypeAnalyzer _resultAnalyzer;
    private readonly SecurityAnalyzer _securityAnalyzer;
    private readonly PerformanceAnalyzer _performanceAnalyzer;

    public AnalysisReport AnalyzeProject(string projectPath, LintConfiguration config)
    {
        var report = new AnalysisReport();
        var sourceFiles = FindFlowLangFiles(projectPath);
        
        foreach (var file in sourceFiles)
        {
            var fileAnalysis = AnalyzeFile(file, config);
            report.FileReports.Add(fileAnalysis);
        }
        
        return report;
    }
}
```

#### Lint Rules System

```csharp
public abstract class LintRule
{
    public abstract string RuleId { get; }
    public abstract string Description { get; }
    public abstract LintSeverity DefaultSeverity { get; }
    
    public abstract List<LintDiagnostic> Analyze(ASTNode node, AnalysisContext context);
}

public class EffectCompletenessRule : LintRule
{
    public override string RuleId => "effect-completeness";
    public override string Description => "Functions must declare all effects they use";
    public override LintSeverity DefaultSeverity => LintSeverity.Error;
    
    public override List<LintDiagnostic> Analyze(ASTNode node, AnalysisContext context)
    {
        // Implementation for effect completeness checking
    }
}
```

#### Analysis Reports

```csharp
public class AnalysisReport
{
    public List<FileAnalysisReport> FileReports { get; set; } = new();
    public AnalysisMetrics Metrics { get; set; } = new();
    public TimeSpan AnalysisTime { get; set; }
    
    public int TotalIssues => FileReports.Sum(f => f.Issues.Count);
    public int ErrorCount => FileReports.Sum(f => f.Issues.Count(i => i.Severity == LintSeverity.Error));
}

public class LintDiagnostic
{
    public string RuleId { get; set; }
    public string Message { get; set; }
    public LintSeverity Severity { get; set; }
    public SourceLocation Location { get; set; }
    public string Category { get; set; }
    public string FixSuggestion { get; set; }
}
```

### Package Management System

The package management system provides .NET ecosystem integration with automatic effect inference.

#### PackageManager Class

```csharp
public class PackageManager
{
    private readonly DependencyResolver _resolver;
    private readonly NuGetIntegration _nugetClient;
    private readonly SecurityScanner _securityScanner;
    private readonly FlowLangRegistry _registry;

    public async Task AddPackageAsync(string packageName, string version = "latest", bool isDev = false)
    {
        var package = await _registry.ResolvePackageAsync(packageName, version);
        var resolved = await _resolver.ResolveWithDependenciesAsync(package);
        
        await InstallPackagesAsync(resolved);
        UpdateProjectConfiguration(package, isDev);
        await GenerateEffectMappingsAsync(resolved);
    }
    
    public async Task<SecurityAuditReport> AuditSecurityAsync()
    {
        var installedPackages = GetInstalledPackages();
        return await _securityScanner.ScanPackagesAsync(installedPackages);
    }
}
```

#### Dependency Resolution

```csharp
public class DependencyResolver
{
    public async Task<List<ResolvedPackage>> ResolveWithDependenciesAsync(PackageReference package)
    {
        var resolved = new List<ResolvedPackage>();
        var visited = new HashSet<string>();
        
        await ResolveRecursiveAsync(package, resolved, visited);
        DetectCircularDependencies(resolved);
        ResolveVersionConflicts(resolved);
        
        return resolved;
    }
    
    private void ResolveVersionConflicts(List<ResolvedPackage> packages)
    {
        // Semantic version conflict resolution
        // Highest compatible version wins
    }
}
```

#### NuGet Integration

```csharp
public class NuGetIntegration
{
    public async Task<List<NuGetPackage>> SearchPackagesAsync(string query)
    {
        // Search NuGet.org and configured feeds
    }
    
    public async Task<FlowLangBinding> GenerateBindingAsync(NuGetPackage package)
    {
        var assembly = await LoadAssemblyAsync(package);
        var types = ExtractPublicTypes(assembly);
        var effects = InferEffectsFromTypes(types);
        
        return new FlowLangBinding
        {
            Package = package,
            GeneratedModule = GenerateFlowLangModule(types),
            EffectMappings = effects
        };
    }
}
```

#### Security Scanning

```csharp
public class SecurityScanner
{
    private readonly GitHubAdvisoryDatabase _githubDb;
    private readonly OSVDatabase _osvDb;
    
    public async Task<SecurityAuditReport> ScanPackagesAsync(List<InstalledPackage> packages)
    {
        var vulnerabilities = new List<SecurityVulnerability>();
        
        foreach (var package in packages)
        {
            var githubVulns = await _githubDb.CheckVulnerabilitiesAsync(package);
            var osvVulns = await _osvDb.CheckVulnerabilitiesAsync(package);
            
            vulnerabilities.AddRange(githubVulns);
            vulnerabilities.AddRange(osvVulns);
        }
        
        return new SecurityAuditReport
        {
            ScannedPackages = packages,
            Vulnerabilities = vulnerabilities,
            Recommendations = GenerateRecommendations(vulnerabilities)
        };
    }
}
```

### Workspace Management

```csharp
public class WorkspaceManager
{
    public async Task<List<FlowLangProject>> DiscoverProjectsAsync(string workspaceRoot)
    {
        var config = await LoadWorkspaceConfigAsync(workspaceRoot);
        var projects = new List<FlowLangProject>();
        
        foreach (var pattern in config.Projects)
        {
            var matchingDirs = Glob.Expand(pattern);
            foreach (var dir in matchingDirs.Where(d => !config.Exclude.Any(e => Glob.IsMatch(d, e))))
            {
                if (File.Exists(Path.Combine(dir, "flowc.json")))
                {
                    projects.Add(await LoadProjectAsync(dir));
                }
            }
        }
        
        return projects;
    }
    
    public async Task ExecuteWorkspaceCommandAsync(string command, string[] args)
    {
        var projects = await DiscoverProjectsAsync(".");
        
        await Parallel.ForEachAsync(projects, async (project, ct) =>
        {
            await ExecuteProjectCommandAsync(project, command, args);
        });
    }
}
```

## Extension Points

### Adding New Language Features

1. **Add Token Types**: Extend `TokenType` enum
2. **Update Lexer**: Add tokenization rules in `FlowLangLexer`
3. **Add AST Nodes**: Create new record types inheriting from `ASTNode`
4. **Update Parser**: Add parsing methods in `FlowLangParser`
5. **Update Code Generator**: Add generation logic in `CSharpGenerator`

### Adding New Effects

```csharp
// In EffectValidator
private static readonly HashSet<string> KnownEffects = new()
{
    "Database", "Network", "Logging", "FileSystem", "Memory", "IO",
    "NewEffect" // Add your effect here
};

// In FlowLangLexer keywords
["NewEffect"] = TokenType.NewEffect,

// Add TokenType.NewEffect to enum
```

### Adding New CLI Commands

```csharp
public class CustomCommand : Command
{
    public override string Name => "custom";
    public override string Description => "Custom command description";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        // Implementation
        return 0;
    }
}

// Register in Main method
Commands["custom"] = new CustomCommand();
```

## Error Handling

### Error Types

The transpiler uses different error types for different phases:

```csharp
// Lexical errors
public class LexicalException : Exception
{
    public int Line { get; }
    public int Column { get; }
    
    public LexicalException(string message, int line, int column) : base(message)
    {
        Line = line;
        Column = column;
    }
}

// Syntax errors
public class SyntaxException : Exception
{
    public Token Token { get; }
    
    public SyntaxException(string message, Token token) : base(message)
    {
        Token = token;
    }
}

// Semantic errors
public class SemanticException : Exception
{
    public ASTNode Node { get; }
    
    public SemanticException(string message, ASTNode node) : base(message)
    {
        Node = node;
    }
}
```

### Error Recovery

The parser implements error recovery strategies:

```csharp
private ASTNode ParseStatementWithRecovery()
{
    try
    {
        return ParseStatement();
    }
    catch (SyntaxException ex)
    {
        // Log error and attempt recovery
        LogError(ex);
        SynchronizeToNextStatement();
        return null; // Skip this statement
    }
}

private void SynchronizeToNextStatement()
{
    while (!IsAtEnd() && !Check(TokenType.Function) && !Check(TokenType.Module))
    {
        Advance();
    }
}
```

## Performance Considerations

### Memory Management

- **Token reuse**: Tokens are immutable records for efficient memory usage
- **AST sharing**: AST nodes use immutable records to enable sharing
- **String interning**: Common strings are interned for memory efficiency

### Compilation Speed

- **Single-pass parsing**: Parser builds AST in one pass
- **Incremental compilation**: Only recompile changed files (future feature)
- **Parallel processing**: Multiple files can be transpiled in parallel

### Generated Code Quality

- **Optimized C#**: Generated code follows C# best practices
- **Minimal allocations**: Avoid unnecessary object creation
- **Efficient string handling**: Use StringBuilder for complex concatenations

## Debugging and Diagnostics

### Debug Output

Enable debug output for development:

```csharp
public class FlowLangTranspiler
{
    public bool EnableDebugOutput { get; set; } = false;
    
    private void DebugLog(string message)
    {
        if (EnableDebugOutput)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }
}
```

### AST Visualization

```csharp
public static class ASTVisualizer
{
    public static string ToTreeString(ASTNode node, int indent = 0)
    {
        var indentStr = new string(' ', indent * 2);
        var result = $"{indentStr}{node.GetType().Name}";
        
        // Add node-specific details
        switch (node)
        {
            case FunctionDeclaration func:
                result += $": {func.Name}";
                break;
            case Identifier id:
                result += $": {id.Name}";
                break;
            // ... other node types
        }
        
        return result;
    }
}
```

## Conclusion

This API reference provides a comprehensive overview of the FlowLang transpiler's internal architecture. The modular design allows for easy extension and modification of language features. Key design principles include:

- **Separation of Concerns**: Each phase has a clear responsibility
- **Immutable Data Structures**: AST nodes and tokens are immutable
- **Error Recovery**: Graceful handling of syntax and semantic errors
- **Extensibility**: Clear extension points for new features
- **Performance**: Efficient algorithms and data structures

For implementation examples and usage patterns, see the source code in `/src/flowc.cs` and the test suite in `/tests/`.