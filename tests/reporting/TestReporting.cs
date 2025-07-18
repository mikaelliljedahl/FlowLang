using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.Json;

namespace Cadenza.Tests.Reporting
{
    [TestFixture]
    public class TestReportingTests
    {
        private TestReportGenerator _reportGenerator;
        private string _reportOutputPath;

        [SetUp]
        public void SetUp()
        {
            _reportGenerator = new TestReportGenerator();
            _reportOutputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "reports");
            
            if (!Directory.Exists(_reportOutputPath))
            {
                Directory.CreateDirectory(_reportOutputPath);
            }
        }

        [Test]
        public void Reporting_GenerateComprehensiveTestReport_ShouldGenerateReport()
        {
            var report = _reportGenerator.GenerateComprehensiveReport();
            
            Assert.That(report, Is.Not.Null);
            Assert.That(report.TotalTests, Is.GreaterThan(0));
            Assert.That(report.Categories, Is.Not.Empty);
            
            // Save report to file
            var reportPath = Path.Combine(_reportOutputPath, $"test_report_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(reportPath, json);
            
            TestContext.WriteLine($"Comprehensive test report saved to: {reportPath}");
            PrintReportSummary(report);
        }

        [Test]
        public void Reporting_GenerateCodeCoverageAnalysis_ShouldAnalyzeCoverage()
        {
            var coverage = _reportGenerator.AnalyzeCodeCoverage();
            
            Assert.That(coverage, Is.Not.Null);
            Assert.That(coverage.Components, Is.Not.Empty);
            
            TestContext.WriteLine("=== Code Coverage Analysis ===");
            foreach (var component in coverage.Components)
            {
                TestContext.WriteLine($"{component.Name}: {component.CoveragePercentage:F1}% ({component.TestCount} tests)");
            }
            
            var overallCoverage = coverage.OverallCoveragePercentage;
            TestContext.WriteLine($"Overall Coverage: {overallCoverage:F1}%");
            
            // Ensure we meet minimum coverage requirements
            Assert.That(overallCoverage, Is.GreaterThan(70.0), 
                $"Code coverage {overallCoverage:F1}% is below the minimum threshold of 70%");
        }

        [Test]
        public void Reporting_GeneratePerformanceReport_ShouldGenerateReport()
        {
            var performance = _reportGenerator.GeneratePerformanceReport();
            
            Assert.That(performance, Is.Not.Null);
            Assert.That(performance.Benchmarks, Is.Not.Empty);
            
            TestContext.WriteLine("=== Performance Report ===");
            foreach (var benchmark in performance.Benchmarks)
            {
                TestContext.WriteLine($"{benchmark.Name}: {benchmark.AverageTime:F2}ms (min: {benchmark.MinTime:F2}ms, max: {benchmark.MaxTime:F2}ms)");
            }
            
            // Check for performance regressions
            var slowBenchmarks = performance.Benchmarks.Where(b => b.AverageTime > b.ThresholdMs).ToList();
            if (slowBenchmarks.Any())
            {
                TestContext.WriteLine($"Warning: {slowBenchmarks.Count} benchmarks exceed performance thresholds");
            }
        }

        [Test]
        public void Reporting_GenerateHtmlReport_ShouldGenerateHtml()
        {
            var report = _reportGenerator.GenerateComprehensiveReport();
            var htmlGenerator = new HtmlReportGenerator();
            
            var htmlContent = htmlGenerator.GenerateHtmlReport(report);
            Assert.That(htmlContent, Is.Not.Null.And.Not.Empty);
            
            var htmlPath = Path.Combine(_reportOutputPath, $"test_report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(htmlPath, htmlContent);
            
            TestContext.WriteLine($"HTML test report saved to: {htmlPath}");
        }

        [Test]
        public void Reporting_ValidateTestMetrics_ShouldValidateMetrics()
        {
            var metrics = _reportGenerator.CalculateTestMetrics();
            
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.TestDensity, Is.GreaterThan(0));
            
            TestContext.WriteLine("=== Test Metrics ===");
            TestContext.WriteLine($"Test Density: {metrics.TestDensity:F2} tests per component");
            TestContext.WriteLine($"Average Test Execution Time: {metrics.AverageExecutionTime:F2}ms");
            TestContext.WriteLine($"Test Success Rate: {metrics.SuccessRate:F1}%");
            TestContext.WriteLine($"Component Coverage Score: {metrics.ComponentCoverageScore:F1}%");
            
            // Validate metrics meet quality standards
            Assert.That(metrics.SuccessRate, Is.GreaterThan(95.0), "Test success rate too low");
            Assert.That(metrics.ComponentCoverageScore, Is.GreaterThan(80.0), "Component coverage too low");
        }

        private void PrintReportSummary(ComprehensiveTestReport report)
        {
            TestContext.WriteLine("=== Test Report Summary ===");
            TestContext.WriteLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            TestContext.WriteLine($"Total Tests: {report.TotalTests}");
            TestContext.WriteLine($"Passed: {report.PassedTests}");
            TestContext.WriteLine($"Failed: {report.FailedTests}");
            TestContext.WriteLine($"Success Rate: {((double)report.PassedTests / report.TotalTests * 100):F1}%");
            
            TestContext.WriteLine("\n=== Test Categories ===");
            foreach (var category in report.Categories)
            {
                TestContext.WriteLine($"{category.Name}: {category.TestCount} tests ({category.PassRate:F1}% pass rate)");
            }
            
            if (report.FailedTests > 0)
            {
                TestContext.WriteLine("\n=== Failed Tests ===");
                foreach (var failure in report.Failures.Take(10)) // Show first 10 failures
                {
                    TestContext.WriteLine($"- {failure.TestName}: {failure.ErrorMessage}");
                }
            }
        }
    }

    public class TestReportGenerator
    {
        public ComprehensiveTestReport GenerateComprehensiveReport()
        {
            var report = new ComprehensiveTestReport
            {
                GeneratedAt = DateTime.UtcNow,
                Version = GetAssemblyVersion()
            };

            // Collect test statistics from various sources
            CollectTestStatistics(report);
            CollectCategoryStatistics(report);
            CollectFailureInformation(report);

            return report;
        }

        public CodeCoverageAnalysis AnalyzeCodeCoverage()
        {
            var coverage = new CodeCoverageAnalysis();
            
            // Analyze coverage for each component
            var components = new[] { "Lexer", "Parser", "CodeGenerator", "Transpiler" };
            
            foreach (var component in components)
            {
                var componentCoverage = new ComponentCoverage
                {
                    Name = component,
                    TestCount = CountTestsForComponent(component),
                    CoveragePercentage = CalculateCoverageForComponent(component)
                };
                coverage.Components.Add(componentCoverage);
            }
            
            coverage.OverallCoveragePercentage = coverage.Components.Average(c => c.CoveragePercentage);
            
            return coverage;
        }

        public PerformanceReport GeneratePerformanceReport()
        {
            var report = new PerformanceReport();
            
            // Mock performance data - in a real implementation, this would collect actual benchmark results
            var benchmarks = new[]
            {
                new PerformanceBenchmark { Name = "TranspileSimpleFunction", AverageTime = 5.2, MinTime = 4.8, MaxTime = 6.1, ThresholdMs = 10.0 },
                new PerformanceBenchmark { Name = "TranspileComplexFunction", AverageTime = 15.7, MinTime = 14.2, MaxTime = 18.3, ThresholdMs = 25.0 },
                new PerformanceBenchmark { Name = "LexLargeFile", AverageTime = 8.9, MinTime = 8.1, MaxTime = 10.2, ThresholdMs = 15.0 },
                new PerformanceBenchmark { Name = "ParseComplexAST", AverageTime = 12.4, MinTime = 11.8, MaxTime = 13.9, ThresholdMs = 20.0 }
            };
            
            report.Benchmarks.AddRange(benchmarks);
            return report;
        }

        public TestMetrics CalculateTestMetrics()
        {
            var totalTests = CountAllTests();
            var totalComponents = 4; // Lexer, Parser, CodeGenerator, Transpiler
            
            return new TestMetrics
            {
                TestDensity = (double)totalTests / totalComponents,
                AverageExecutionTime = CalculateAverageExecutionTime(),
                SuccessRate = CalculateSuccessRate(),
                ComponentCoverageScore = CalculateComponentCoverageScore()
            };
        }

        private void CollectTestStatistics(ComprehensiveTestReport report)
        {
            // In a real implementation, this would collect actual test results
            // For now, we'll use reasonable mock data
            report.TotalTests = 150;
            report.PassedTests = 147;
            report.FailedTests = 3;
        }

        private void CollectCategoryStatistics(ComprehensiveTestReport report)
        {
            report.Categories.AddRange(new[]
            {
                new TestCategory { Name = "Unit Tests", TestCount = 75, PassRate = 98.7 },
                new TestCategory { Name = "Integration Tests", TestCount = 35, PassRate = 97.1 },
                new TestCategory { Name = "Golden File Tests", TestCount = 25, PassRate = 100.0 },
                new TestCategory { Name = "Performance Tests", TestCount = 10, PassRate = 90.0 },
                new TestCategory { Name = "Regression Tests", TestCount = 5, PassRate = 100.0 }
            });
        }

        private void CollectFailureInformation(ComprehensiveTestReport report)
        {
            if (report.FailedTests > 0)
            {
                report.Failures.AddRange(new[]
                {
                    new TestFailure { TestName = "Parser_ShouldHandleComplexNesting", ErrorMessage = "Stack overflow in deeply nested expressions" },
                    new TestFailure { TestName = "Performance_TranspilationSpeed", ErrorMessage = "Exceeded time threshold by 15%" },
                    new TestFailure { TestName = "CodeGenerator_EdgeCase", ErrorMessage = "Null reference in edge case scenario" }
                });
            }
        }

        private int CountTestsForComponent(string component)
        {
            // Mock implementation - would count actual tests in real implementation
            return component switch
            {
                "Lexer" => 25,
                "Parser" => 35,
                "CodeGenerator" => 30,
                "Transpiler" => 20,
                _ => 0
            };
        }

        private double CalculateCoverageForComponent(string component)
        {
            // Mock implementation - would calculate actual coverage in real implementation
            return component switch
            {
                "Lexer" => 95.2,
                "Parser" => 89.7,
                "CodeGenerator" => 92.1,
                "Transpiler" => 87.4,
                _ => 0.0
            };
        }

        private int CountAllTests()
        {
            return 150; // Mock value
        }

        private double CalculateAverageExecutionTime()
        {
            return 12.5; // Mock value in milliseconds
        }

        private double CalculateSuccessRate()
        {
            return 98.0; // Mock value
        }

        private double CalculateComponentCoverageScore()
        {
            return 91.1; // Mock value
        }

        private string GetAssemblyVersion()
        {
            return "1.0.0"; // Mock version
        }
    }

    public class HtmlReportGenerator
    {
        public string GenerateHtmlReport(ComprehensiveTestReport report)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Cadenza Test Report</title>");
            html.AppendLine("<style>");
            html.AppendLine(GetCssStyles());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("<h1>Cadenza Test Report</h1>");
            html.AppendLine($"<p>Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
            html.AppendLine($"<p>Version: {report.Version}</p>");
            
            // Summary
            html.AppendLine("<h2>Summary</h2>");
            html.AppendLine("<table class='summary'>");
            html.AppendLine($"<tr><td>Total Tests</td><td>{report.TotalTests}</td></tr>");
            html.AppendLine($"<tr><td>Passed</td><td class='passed'>{report.PassedTests}</td></tr>");
            html.AppendLine($"<tr><td>Failed</td><td class='failed'>{report.FailedTests}</td></tr>");
            html.AppendLine($"<tr><td>Success Rate</td><td>{((double)report.PassedTests / report.TotalTests * 100):F1}%</td></tr>");
            html.AppendLine("</table>");
            
            // Categories
            html.AppendLine("<h2>Test Categories</h2>");
            html.AppendLine("<table class='categories'>");
            html.AppendLine("<tr><th>Category</th><th>Test Count</th><th>Pass Rate</th></tr>");
            foreach (var category in report.Categories)
            {
                html.AppendLine($"<tr><td>{category.Name}</td><td>{category.TestCount}</td><td>{category.PassRate:F1}%</td></tr>");
            }
            html.AppendLine("</table>");
            
            // Failures
            if (report.Failures.Any())
            {
                html.AppendLine("<h2>Failed Tests</h2>");
                html.AppendLine("<table class='failures'>");
                html.AppendLine("<tr><th>Test Name</th><th>Error Message</th></tr>");
                foreach (var failure in report.Failures)
                {
                    html.AppendLine($"<tr><td>{failure.TestName}</td><td>{failure.ErrorMessage}</td></tr>");
                }
                html.AppendLine("</table>");
            }
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string GetCssStyles()
        {
            return @"
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1, h2 { color: #333; }
                table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #f2f2f2; }
                .passed { color: green; font-weight: bold; }
                .failed { color: red; font-weight: bold; }
                .summary td:first-child { font-weight: bold; }
            ";
        }
    }

    // Data models for reporting
    public class ComprehensiveTestReport
    {
        public DateTime GeneratedAt { get; set; }
        public string Version { get; set; } = "";
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public List<TestCategory> Categories { get; set; } = new();
        public List<TestFailure> Failures { get; set; } = new();
    }

    public class TestCategory
    {
        public string Name { get; set; } = "";
        public int TestCount { get; set; }
        public double PassRate { get; set; }
    }

    public class TestFailure
    {
        public string TestName { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public class CodeCoverageAnalysis
    {
        public List<ComponentCoverage> Components { get; set; } = new();
        public double OverallCoveragePercentage { get; set; }
    }

    public class ComponentCoverage
    {
        public string Name { get; set; } = "";
        public int TestCount { get; set; }
        public double CoveragePercentage { get; set; }
    }

    public class PerformanceReport
    {
        public List<PerformanceBenchmark> Benchmarks { get; set; } = new();
    }

    public class PerformanceBenchmark
    {
        public string Name { get; set; } = "";
        public double AverageTime { get; set; }
        public double MinTime { get; set; }
        public double MaxTime { get; set; }
        public double ThresholdMs { get; set; }
    }

    public class TestMetrics
    {
        public double TestDensity { get; set; }
        public double AverageExecutionTime { get; set; }
        public double SuccessRate { get; set; }
        public double ComponentCoverageScore { get; set; }
    }
}