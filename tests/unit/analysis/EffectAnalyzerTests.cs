using System;
using System.Collections.Generic;
using System.Linq;
using FlowLang.Analysis;
using Xunit;

namespace FlowLang.Tests.Unit.Analysis;

public class EffectAnalyzerTests
{
    [Fact]
    public void PureFunctionValidationRule_ShouldDetectPureFunctionWithEffects()
    {
        // Arrange
        var rule = new EffectAnalyzer.PureFunctionValidationRule();
        var ast = new Program(new List<ASTNode>
        {
            new FunctionDeclaration(
                "badPure",
                new List<Parameter>(),
                "int",
                new List<ASTNode>(),
                new EffectAnnotation(new List<string> { "Database" }),
                isPure: true
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", "pure function badPure() uses [Database] -> int {}").ToList();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("pure-function-validation", diagnostics[0].RuleId);
        Assert.Contains("Pure function 'badPure' cannot declare effects", diagnostics[0].Message);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
    }

    [Fact]
    public void PureFunctionValidationRule_ShouldAllowPureFunctionWithoutEffects()
    {
        // Arrange
        var rule = new EffectAnalyzer.PureFunctionValidationRule();
        var ast = new Program(new List<ASTNode>
        {
            new FunctionDeclaration(
                "goodPure",
                new List<Parameter>(),
                "int",
                new List<ASTNode>(),
                effectAnnotation: null,
                isPure: true
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", "pure function goodPure() -> int {}").ToList();

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EffectCompletenessRule_ShouldDetectMissingEffectDeclarations()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectCompletenessRule();
        var functionBody = new List<ASTNode>
        {
            new FunctionCall("database_query", new List<ASTNode>()),
            new ReturnStatement(new NumberLiteral(42))
        };

        var ast = new Program(new List<ASTNode>
        {
            new FunctionDeclaration(
                "testFunction",
                new List<Parameter>(),
                "int",
                functionBody,
                effectAnnotation: null // No effects declared
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", 
            "function testFunction() -> int { database_query() return 42 }").ToList();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("effect-completeness", diagnostics[0].RuleId);
        Assert.Contains("uses effects", diagnostics[0].Message);
        Assert.Contains("doesn't declare them", diagnostics[0].Message);
    }

    [Fact]
    public void EffectMinimalityRule_ShouldDetectUnusedEffects()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectMinimalityRule();
        var functionBody = new List<ASTNode>
        {
            new ReturnStatement(new NumberLiteral(42))
        };

        var ast = new Program(new List<ASTNode>
        {
            new FunctionDeclaration(
                "testFunction",
                new List<Parameter>(),
                "int",
                functionBody,
                new EffectAnnotation(new List<string> { "Database", "Network" }) // Effects declared but not used
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", 
            "function testFunction() uses [Database, Network] -> int { return 42 }").ToList();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("effect-minimality", diagnostics[0].RuleId);
        Assert.Contains("declares effects", diagnostics[0].Message);
        Assert.Contains("doesn't appear to use them", diagnostics[0].Message);
    }

    [Fact]
    public void EffectPropagationRule_ShouldDetectMissingEffectPropagation()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectPropagationRule();
        
        // First function with effects
        var dbFunction = new FunctionDeclaration(
            "queryDatabase",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>(),
            new EffectAnnotation(new List<string> { "Database" })
        );

        // Second function that calls the first but doesn't declare Database effect
        var callerFunction = new FunctionDeclaration(
            "processData",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>
            {
                new FunctionCall("queryDatabase", new List<ASTNode>())
            },
            effectAnnotation: null // Missing Database effect
        );

        var ast = new Program(new List<ASTNode> { dbFunction, callerFunction });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", "test source").ToList();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("effect-propagation", diagnostics[0].RuleId);
        Assert.Contains("calls 'queryDatabase' but doesn't declare required effects", diagnostics[0].Message);
    }

    [Fact]
    public void EffectPropagationRule_ShouldDetectPureFunctionCallingEffectFunction()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectPropagationRule();
        
        // Function with effects
        var effectFunction = new FunctionDeclaration(
            "writeLog",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>(),
            new EffectAnnotation(new List<string> { "Logging" })
        );

        // Pure function that calls the effect function (violation)
        var pureFunction = new FunctionDeclaration(
            "calculate",
            new List<Parameter>(),
            "int",
            new List<ASTNode>
            {
                new FunctionCall("writeLog", new List<ASTNode>()),
                new ReturnStatement(new NumberLiteral(42))
            },
            effectAnnotation: null,
            isPure: true
        );

        var ast = new Program(new List<ASTNode> { effectFunction, pureFunction });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", "test source").ToList();

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal("effect-propagation", diagnostics[0].RuleId);
        Assert.Contains("Pure function 'calculate' cannot call function 'writeLog' which has effects", diagnostics[0].Message);
    }

    [Fact]
    public void EffectPropagationRule_ShouldAllowProperEffectPropagation()
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectPropagationRule();
        
        // Function with effects
        var dbFunction = new FunctionDeclaration(
            "queryDatabase",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>(),
            new EffectAnnotation(new List<string> { "Database" })
        );

        // Function that properly declares effects it uses
        var callerFunction = new FunctionDeclaration(
            "processData",
            new List<Parameter>(),
            "Result<string, string>",
            new List<ASTNode>
            {
                new FunctionCall("queryDatabase", new List<ASTNode>())
            },
            new EffectAnnotation(new List<string> { "Database" }) // Properly declares Database effect
        );

        var ast = new Program(new List<ASTNode> { dbFunction, callerFunction });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", "test source").ToList();

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EffectAnalyzer_ShouldProvideAllRules()
    {
        // Act
        var rules = EffectAnalyzer.GetRules().ToList();

        // Assert
        Assert.NotEmpty(rules);
        Assert.Contains(rules, r => r.RuleId == "pure-function-validation");
        Assert.Contains(rules, r => r.RuleId == "effect-completeness");
        Assert.Contains(rules, r => r.RuleId == "effect-minimality");
        Assert.Contains(rules, r => r.RuleId == "effect-propagation");
        
        // All rules should have the effect system category
        Assert.All(rules, r => Assert.Equal(AnalysisCategories.EffectSystem, r.Category));
    }

    [Theory]
    [InlineData("database_save", "Database")]
    [InlineData("http_request", "Network")]
    [InlineData("log_message", "Logging")]
    [InlineData("file_read", "FileSystem")]
    [InlineData("cache_store", "Memory")]
    [InlineData("input_stream", "IO")]
    public void EffectCompletenessRule_ShouldDetectEffectsByFunctionName(string functionName, string expectedEffect)
    {
        // Arrange
        var rule = new EffectAnalyzer.EffectCompletenessRule();
        var functionBody = new List<ASTNode>
        {
            new FunctionCall(functionName, new List<ASTNode>()),
            new ReturnStatement(new NumberLiteral(42))
        };

        var ast = new Program(new List<ASTNode>
        {
            new FunctionDeclaration(
                "testFunction",
                new List<Parameter>(),
                "int",
                functionBody,
                effectAnnotation: null
            )
        });

        // Act
        var diagnostics = rule.Analyze(ast, "test.flow", $"function testFunction() -> int {{ {functionName}() return 42 }}").ToList();

        // Assert
        Assert.Single(diagnostics);
        Assert.Contains(expectedEffect, diagnostics[0].Message);
    }
}