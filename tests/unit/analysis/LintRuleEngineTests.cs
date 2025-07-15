using System;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Analysis;
using Xunit;

namespace Cadenza.Tests.Unit.Analysis;

public class LintRuleEngineTests
{
    [Fact]
    public void CreateDefaultConfiguration_ShouldHaveAllStandardRules()
    {
        // Arrange & Act
        var config = LintConfiguration.CreateDefaultConfiguration();

        // Assert
        Assert.NotEmpty(config.Rules);
        
        // Check for key rule categories
        Assert.True(config.Rules.ContainsKey("effect-completeness"));
        Assert.True(config.Rules.ContainsKey("unused-results"));
        Assert.True(config.Rules.ContainsKey("dead-code"));
        Assert.True(config.Rules.ContainsKey("string-concatenation"));
        Assert.True(config.Rules.ContainsKey("input-validation"));
    }

    [Fact]
    public void LintConfiguration_ShouldRespectSeverityThreshold()
    {
        // Arrange
        var config = new LintConfiguration
        {
            SeverityThreshold = DiagnosticSeverity.Warning,
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["test-error"] = new(DiagnosticSeverity.Error),
                ["test-warning"] = new(DiagnosticSeverity.Warning),
                ["test-info"] = new(DiagnosticSeverity.Info)
            }
        };

        // Act & Assert
        Assert.True(config.IsRuleEnabled("test-error"));
        Assert.True(config.IsRuleEnabled("test-warning"));
        Assert.False(config.IsRuleEnabled("test-info"));
    }

    [Fact]
    public void LintConfiguration_ShouldExcludeFilesByPattern()
    {
        // Arrange
        var config = new LintConfiguration
        {
            Exclude = new List<string> { "generated/*", "*.test.cdz", "temp_*" }
        };

        // Act & Assert
        Assert.True(config.ShouldExcludeFile("generated/auto.cdz"));
        Assert.True(config.ShouldExcludeFile("example.test.cdz"));
        Assert.True(config.ShouldExcludeFile("temp_output.cdz"));
        Assert.False(config.ShouldExcludeFile("src/main.cdz"));
    }

    [Fact]
    public void LintRule_ShouldGetParameterValues()
    {
        // Arrange
        var config = new LintConfiguration
        {
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["test-rule"] = new(DiagnosticSeverity.Warning, true, new Dictionary<string, object>
                {
                    ["maxLength"] = 100,
                    ["enabled"] = true,
                    ["pattern"] = "test-*"
                })
            }
        };

        var rule = new TestRule();
        rule.SetConfiguration(config);

        // Act & Assert
        Assert.Equal(100, rule.GetTestParameter("maxLength", 50));
        Assert.True(rule.GetTestParameter("enabled", false));
        Assert.Equal("test-*", rule.GetTestParameter("pattern", "default"));
        Assert.Equal(42, rule.GetTestParameter("missing", 42));
    }

    [Fact]
    public void AnalysisReport_ShouldAggregateMetricsCorrectly()
    {
        // Arrange
        var report = new AnalysisReport();

        // Act
        report.AddDiagnostic(new AnalysisDiagnostic(
            "test-rule-1", "Test error", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 1, 1), "test"));

        report.AddDiagnostic(new AnalysisDiagnostic(
            "test-rule-2", "Test warning", DiagnosticSeverity.Warning,
            new SourceLocation("test.cdz", 2, 1), "test"));

        report.AddDiagnostic(new AnalysisDiagnostic(
            "test-rule-1", "Another error", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 3, 1), "test"));

        // Assert
        Assert.Equal(3, report.Metrics.TotalIssues);
        Assert.Equal(2, report.Metrics.Errors);
        Assert.Equal(1, report.Metrics.Warnings);
        Assert.Equal(0, report.Metrics.Infos);
        
        Assert.Equal(2, report.RuleCounts["test-rule-1"]);
        Assert.Equal(1, report.RuleCounts["test-rule-2"]);
    }

    [Fact]
    public void AnalysisReport_ShouldFilterDiagnosticsByCategory()
    {
        // Arrange
        var report = new AnalysisReport();
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule1", "Message", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 1, 1), "category1"));
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule2", "Message", DiagnosticSeverity.Warning,
            new SourceLocation("test.cdz", 2, 1), "category2"));
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule3", "Message", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 3, 1), "category1"));

        // Act
        var category1Diagnostics = report.GetDiagnosticsByCategory("category1").ToList();
        var category2Diagnostics = report.GetDiagnosticsByCategory("category2").ToList();

        // Assert
        Assert.Equal(2, category1Diagnostics.Count);
        Assert.Equal(1, category2Diagnostics.Count);
        Assert.All(category1Diagnostics, d => Assert.Equal("category1", d.Category));
        Assert.All(category2Diagnostics, d => Assert.Equal("category2", d.Category));
    }

    [Fact]
    public void AnalysisReport_ShouldDeterminePassingResult()
    {
        // Arrange
        var report = new AnalysisReport();

        // Act & Assert - Empty report should pass
        Assert.True(report.HasPassingResult(DiagnosticSeverity.Error));
        Assert.True(report.HasPassingResult(DiagnosticSeverity.Warning));

        // Add warning
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule", "Warning", DiagnosticSeverity.Warning,
            new SourceLocation("test.cdz", 1, 1), "test"));

        Assert.True(report.HasPassingResult(DiagnosticSeverity.Error));
        Assert.False(report.HasPassingResult(DiagnosticSeverity.Warning));

        // Add error
        report.AddDiagnostic(new AnalysisDiagnostic(
            "rule", "Error", DiagnosticSeverity.Error,
            new SourceLocation("test.cdz", 2, 1), "test"));

        Assert.False(report.HasPassingResult(DiagnosticSeverity.Error));
        Assert.False(report.HasPassingResult(DiagnosticSeverity.Warning));
    }

    [Fact]
    public void LintRuleEngine_ShouldRegisterAndRunRules()
    {
        // Arrange
        var config = new LintConfiguration
        {
            Rules = new Dictionary<string, LintRuleConfig>
            {
                ["test-rule"] = new(DiagnosticSeverity.Error, true)
            }
        };
        var engine = new LintRuleEngine(config);
        var rule = new TestRule();
        engine.RegisterRule(rule);

        var ast = CreateTestProgram();

        // Act
        var report = engine.AnalyzeFile("test.cdz", "function test() -> int { return 42 }", ast);

        // Assert
        Assert.Equal(1, report.FilesAnalyzed);
        Assert.Single(report.Diagnostics);
        Assert.Equal("test-rule", report.Diagnostics[0].RuleId);
    }

    private Program CreateTestProgram()
    {
        return new Program(new List<ASTNode>
        {
            new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode>
                {
                    new ReturnStatement(new NumberLiteral(42))
                }
            )
        });
    }

    // Test rule implementation for testing
    private class TestRule : LintRule
    {
        public override string RuleId => "test-rule";
        public override string Description => "Test rule for unit tests";
        public override string Category => "test";

        public override IEnumerable<AnalysisDiagnostic> Analyze(Program ast, string filePath, string sourceText)
        {
            yield return CreateDiagnostic(
                "Test diagnostic",
                new SourceLocation(filePath, 1, 1, 0, "test line")
            );
        }

        public T? GetTestParameter<T>(string paramName, T? defaultValue = default)
        {
            return GetParameter(paramName, defaultValue);
        }
    }
}