using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cadenza.Core;
using System.Text.Json;

namespace Cadenza.Tests.Regression
{
    [TestFixture]
    public class RegressionTests
    {
        private CadenzaTranspiler _transpiler;
        private string _regressionDataPath;

        [SetUp]
        public void SetUp()
        {
            _transpiler = new CadenzaTranspiler();
            _regressionDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "regression", "data");
            
            // Create regression data directory if it doesn't exist
            if (!Directory.Exists(_regressionDataPath))
            {
                Directory.CreateDirectory(_regressionDataPath);
            }
        }

        /// <summary>
        /// Helper method to transpile code directly using the existing components
        /// </summary>
        private string TranspileCodeDirectly(string cadenzaCode)
        {
            // Use the existing components directly
            var lexer = new CadenzaLexer(cadenzaCode);
            var tokens = lexer.ScanTokens();
            
            var parser = new CadenzaParser(tokens);
            var ast = parser.Parse();
            
            var generator = new CSharpGenerator();
            var syntaxTree = generator.GenerateFromAST(ast);
            
            return syntaxTree.GetRoot().ToFullString();
        }

        [Test]
        public void Regression_PreventBreakingChanges()
        {
            // Load known good test cases and verify they still work
            var testCases = LoadRegressionTestCases();
            var failedCases = new List<string>();

            foreach (var testCase in testCases)
            {
                try
                {
                    var actual = TranspileCodeDirectly(testCase.Input);
                    
                    // Verify the output still compiles and contains expected patterns
                    if (!VerifyOutputIntegrity(actual, testCase))
                    {
                        failedCases.Add($"{testCase.Name}: Output integrity check failed");
                    }
                }
                catch (Exception ex)
                {
                    failedCases.Add($"{testCase.Name}: {ex.Message}");
                }
            }

            Assert.That(failedCases, Is.Empty, 
                $"Regression test failures detected:\n{string.Join("\n", failedCases)}");
        }

        [Test]
        public void Regression_BasicLanguageFeatures()
        {
            var testCases = new[]
            {
                new RegressionTestCase
                {
                    Name = "BasicFunction",
                    Input = "function test() -> int { return 42 }",
                    ExpectedPatterns = new[] { "public static int test()", "return 42;" }
                },
                new RegressionTestCase
                {
                    Name = "PureFunction",
                    Input = "pure function add(a: int, b: int) -> int { return a + b }",
                    ExpectedPatterns = new[] { "Pure function - no side effects", "public static int add" }
                },
                new RegressionTestCase
                {
                    Name = "FunctionWithEffects",
                    Input = "function save() uses [Database] -> int { return 42 }",
                    ExpectedPatterns = new[] { "Effects: Database", "public static int save" }
                },
                new RegressionTestCase
                {
                    Name = "ResultType",
                    Input = "function test() -> Result<int, string> { return Ok(42) }",
                    ExpectedPatterns = new[] { "Result<int, string>", "Result.Ok(42)" }
                },
                new RegressionTestCase
                {
                    Name = "IfStatement",
                    Input = "function test(x: int) -> int { if x > 0 { return 1 } else { return 0 } }",
                    ExpectedPatterns = new[] { "if (x > 0)", "return 1;", "return 0;" }
                },
                new RegressionTestCase
                {
                    Name = "GuardStatement", 
                    Input = "function test(x: int) -> int { guard x > 0 else { return 0 } return x }",
                    ExpectedPatterns = new[] { "if (!(x > 0))", "return 0;", "return x;" }
                }
            };

            foreach (var testCase in testCases)
            {
                var actual = TranspileCodeDirectly(testCase.Input);
                
                foreach (var pattern in testCase.ExpectedPatterns)
                {
                    Assert.That(actual, Contains.Substring(pattern), 
                        $"Regression test '{testCase.Name}' failed: Expected pattern '{pattern}' not found in output");
                }
            }
        }

        [Test]
        public void Regression_ErrorHandling()
        {
            // Verify that known error cases still produce appropriate errors
            var errorCases = new[]
            {
                new { Input = "function test( -> int", ExpectedError = "parameter" },
                new { Input = "function test() -> int { return", ExpectedError = "Unexpected token" },
                new { Input = "pure function test() uses [Database] -> int { return 42 }", ExpectedError = "Pure functions cannot have effect annotations" },
                new { Input = "function test() uses [InvalidEffect] -> int { return 42 }", ExpectedError = "Unknown effect" }
            };

            foreach (var errorCase in errorCases)
            {
                Assert.Throws<Exception>(() => TranspileCodeDirectly(errorCase.Input), 
                    $"Expected error for input: {errorCase.Input}");
            }
        }

        [Test]
        public void Regression_PerformanceBaseline()
        {
            // Ensure transpilation performance hasn't regressed significantly
            var largeInput = GenerateLargeProgram(50); // 50 functions
            var iterations = 10;
            var times = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                TranspileCodeDirectly(largeInput);
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            var averageTime = times.Average();
            var maxAcceptableTime = 5000; // 5 seconds maximum

            Assert.That(averageTime, Is.LessThan(maxAcceptableTime), 
                $"Performance regression detected: Average transpilation time {averageTime}ms exceeds acceptable threshold of {maxAcceptableTime}ms");

            TestContext.WriteLine($"Performance baseline: Average transpilation time for large program: {averageTime:F2}ms");
        }

        [Test]
        public void Regression_GeneratedCodeCompilation()
        {
            // Verify that generated code still compiles correctly
            var testInputs = new[]
            {
                "function simple() -> int { return 42 }",
                "function withResult() -> Result<int, string> { return Ok(42) }",
                "module Test { function helper() -> int { return 1 } export {helper} }",
                "function withEffects() uses [Database] -> int { return 42 }",
                "function withInterpolation(name: string) -> string { return $\"Hello {name}!\" }"
            };

            foreach (var input in testInputs)
            {
                var output = TranspileCodeDirectly(input);
                ValidateGeneratedCodeCompiles(output);
            }
        }

        [Test]
        public void Regression_SaveKnownGoodCases()
        {
            // Save current successful test cases for future regression testing
            var testCases = new[]
            {
                new RegressionTestCase { Name = "BasicFunction", Input = "function test() -> int { return 42 }" },
                new RegressionTestCase { Name = "ComplexFunction", Input = GenerateComplexFunction() },
                new RegressionTestCase { Name = "ModuleWithExports", Input = GenerateModuleCode() }
            };

            foreach (var testCase in testCases)
            {
                try
                {
                    testCase.Output = TranspileCodeDirectly(testCase.Input);
                    testCase.Timestamp = DateTime.UtcNow;
                    SaveRegressionTestCase(testCase);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed to save regression test case '{testCase.Name}': {ex.Message}");
                }
            }
        }

        private List<RegressionTestCase> LoadRegressionTestCases()
        {
            var testCases = new List<RegressionTestCase>();
            
            if (!Directory.Exists(_regressionDataPath))
                return testCases;

            var files = Directory.GetFiles(_regressionDataPath, "*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var testCase = JsonSerializer.Deserialize<RegressionTestCase>(json);
                    if (testCase != null)
                    {
                        testCases.Add(testCase);
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Warning: Failed to load regression test case from {file}: {ex.Message}");
                }
            }

            return testCases;
        }

        private void SaveRegressionTestCase(RegressionTestCase testCase)
        {
            var fileName = $"{testCase.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_regressionDataPath, fileName);
            
            var json = JsonSerializer.Serialize(testCase, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(filePath, json);
        }

        private bool VerifyOutputIntegrity(string output, RegressionTestCase testCase)
        {
            // Basic integrity checks
            if (string.IsNullOrWhiteSpace(output))
                return false;

            // Verify it's valid C# syntax
            try
            {
                ValidateGeneratedCodeCompiles(output);
            }
            catch
            {
                return false;
            }

            // Check for expected patterns if available
            if (testCase.ExpectedPatterns != null)
            {
                foreach (var pattern in testCase.ExpectedPatterns)
                {
                    if (!output.Contains(pattern))
                        return false;
                }
            }

            return true;
        }

        private void ValidateGeneratedCodeCompiles(string code)
        {
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            
            var references = new[]
            {
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };

            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                "RegressionTest",
                new[] { syntaxTree },
                references,
                new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
            );

            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();

            if (errors.Any())
            {
                throw new Exception($"Generated code has compilation errors: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
            }
        }

        private string GenerateLargeProgram(int functionCount)
        {
            var functions = new List<string>();
            
            for (int i = 0; i < functionCount; i++)
            {
                functions.Add($"function func{i}(x: int) -> int {{ return x + {i} }}");
            }

            return string.Join("\n", functions);
        }

        private string GenerateComplexFunction()
        {
            return @"
                function complexExample(x: int, y: int) -> Result<string, string> {
                    if x < 0 {
                        return Error(""x must be positive"")
                    }
                    
                    guard y > 0 else {
                        return Error(""y must be greater than zero"")
                    }
                    
                    let result = processData(x + y)?
                    return Ok($""Result: {result}"")
                }";
        }

        private string GenerateModuleCode()
        {
            return @"
                module TestModule {
                    pure function add(a: int, b: int) -> int {
                        return a + b
                    }
                    
                    function process(x: int) uses [Database] -> Result<int, string> {
                        return Ok(x * 2)
                    }
                    
                    export {add, process}
                }";
        }
    }

    public class RegressionTestCase
    {
        public string Name { get; set; } = "";
        public string Input { get; set; } = "";
        public string? Output { get; set; }
        public string[]? ExpectedPatterns { get; set; }
        public DateTime Timestamp { get; set; }
    }
}