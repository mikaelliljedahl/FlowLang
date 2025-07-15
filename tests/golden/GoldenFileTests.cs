using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cadenza.Tests.Golden
{
    [TestFixture]
    public class GoldenFileTests
    {
        private CadenzaTranspiler _transpiler;
        private string _goldenInputsPath;
        private string _goldenExpectedPath;

        [SetUp]
        public void SetUp()
        {
            _transpiler = new CadenzaTranspiler();
            _goldenInputsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "golden", "inputs");
            _goldenExpectedPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "golden", "expected");
        }

        [Test]
        public void GoldenFile_BasicFunctions()
        {
            ExecuteGoldenFileTest("basic_functions");
        }

        [Test]
        public void GoldenFile_ControlFlow()
        {
            ExecuteGoldenFileTest("control_flow");
        }

        [Test]
        public void GoldenFile_ResultTypes()
        {
            ExecuteGoldenFileTest("result_types");
        }

        [Test]
        public void GoldenFile_Modules()
        {
            ExecuteGoldenFileTest("modules");
        }

        [Test]
        public void GoldenFile_StringInterpolation()
        {
            ExecuteGoldenFileTest("string_interpolation");
        }

        [Test]
        public void GoldenFile_EffectSystem()
        {
            ExecuteGoldenFileTest("effect_system");
        }

        [Test]
        public void GoldenFile_PureFunctions()
        {
            ExecuteGoldenFileTest("pure_functions");
        }

        private void ExecuteGoldenFileTest(string testName)
        {
            // Arrange
            var inputFile = Path.Combine(_goldenInputsPath, $"{testName}.cdz");
            var expectedFile = Path.Combine(_goldenExpectedPath, $"{testName}.cs");

            Assert.That(File.Exists(inputFile), $"Input file not found: {inputFile}");
            Assert.That(File.Exists(expectedFile), $"Expected file not found: {expectedFile}");

            var input = File.ReadAllText(inputFile);
            var expected = File.ReadAllText(expectedFile);

            // Act
            var actual = _transpiler.TranspileToCS(input);

            // Assert
            var normalizedExpected = NormalizeCode(expected);
            var normalizedActual = NormalizeCode(actual);

            Assert.That(normalizedActual, Is.EqualTo(normalizedExpected), 
                $"Generated code doesn't match expected output for {testName}.\n\n" +
                $"Expected:\n{normalizedExpected}\n\n" +
                $"Actual:\n{normalizedActual}");

            // Additional validation - ensure generated code compiles
            ValidateGeneratedCode(actual, testName);
        }

        private string NormalizeCode(string code)
        {
            // Parse and normalize the C# code for comparison
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                return syntaxTree.GetRoot().NormalizeWhitespace().ToFullString().Trim();
            }
            catch (Exception)
            {
                // If parsing fails, just normalize whitespace manually
                return code.Trim()
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Replace("  ", " ")
                    .Replace("\n ", "\n")
                    .Replace(" \n", "\n");
            }
        }

        private void ValidateGeneratedCode(string generatedCode, string testName)
        {
            // Verify that the generated code compiles successfully
            var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);
            
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                $"TestAssembly_{testName}",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (errors.Any())
            {
                var errorMessages = string.Join("\n", errors.Select(e => $"{e.GetMessage()} at {e.Location}"));
                Assert.Fail($"Generated code for {testName} has compilation errors:\n{errorMessages}\n\nGenerated code:\n{generatedCode}");
            }
        }

        [Test]
        public void GoldenFile_AllInputsHaveExpectedOutputs()
        {
            // Verify that every input file has a corresponding expected output file
            var inputFiles = Directory.GetFiles(_goldenInputsPath, "*.cdz");
            var missingOutputs = new List<string>();

            foreach (var inputFile in inputFiles)
            {
                var testName = Path.GetFileNameWithoutExtension(inputFile);
                var expectedFile = Path.Combine(_goldenExpectedPath, $"{testName}.cs");
                
                if (!File.Exists(expectedFile))
                {
                    missingOutputs.Add(testName);
                }
            }

            Assert.That(missingOutputs, Is.Empty, 
                $"Missing expected output files for: {string.Join(", ", missingOutputs)}");
        }

        [Test]
        public void GoldenFile_AllOutputsHaveInputs()
        {
            // Verify that every expected output file has a corresponding input file
            var outputFiles = Directory.GetFiles(_goldenExpectedPath, "*.cs");
            var missingInputs = new List<string>();

            foreach (var outputFile in outputFiles)
            {
                var testName = Path.GetFileNameWithoutExtension(outputFile);
                var inputFile = Path.Combine(_goldenInputsPath, $"{testName}.cdz");
                
                if (!File.Exists(inputFile))
                {
                    missingInputs.Add(testName);
                }
            }

            Assert.That(missingInputs, Is.Empty, 
                $"Missing input files for: {string.Join(", ", missingInputs)}");
        }

        [Test]
        public void GoldenFile_ValidateAllExpectedOutputsCompile()
        {
            // Ensure all expected output files contain valid C# code
            var outputFiles = Directory.GetFiles(_goldenExpectedPath, "*.cs");
            var compilationErrors = new List<string>();

            foreach (var outputFile in outputFiles)
            {
                var testName = Path.GetFileNameWithoutExtension(outputFile);
                var code = File.ReadAllText(outputFile);

                try
                {
                    ValidateGeneratedCode(code, testName);
                }
                catch (Exception ex)
                {
                    compilationErrors.Add($"{testName}: {ex.Message}");
                }
            }

            Assert.That(compilationErrors, Is.Empty, 
                $"Expected output files have compilation errors:\n{string.Join("\n", compilationErrors)}");
        }

        [Test]
        public void GoldenFile_TestCoverageAnalysis()
        {
            // Analyze what language features are covered by golden tests
            var inputFiles = Directory.GetFiles(_goldenInputsPath, "*.cdz");
            var features = new Dictionary<string, List<string>>();

            foreach (var inputFile in inputFiles)
            {
                var testName = Path.GetFileNameWithoutExtension(inputFile);
                var content = File.ReadAllText(inputFile);
                var detectedFeatures = AnalyzeLanguageFeatures(content);
                
                foreach (var feature in detectedFeatures)
                {
                    if (!features.ContainsKey(feature))
                    {
                        features[feature] = new List<string>();
                    }
                    features[feature].Add(testName);
                }
            }

            // Report coverage
            TestContext.WriteLine("Golden File Test Coverage Analysis:");
            foreach (var feature in features.OrderBy(f => f.Key))
            {
                TestContext.WriteLine($"  {feature.Key}: {string.Join(", ", feature.Value)}");
            }

            // Ensure critical features are covered
            var criticalFeatures = new[] 
            { 
                "functions", "pure_functions", "effects", "result_types", 
                "if_statements", "guard_statements", "modules", "imports", "string_interpolation" 
            };

            var missingFeatures = criticalFeatures.Where(f => !features.ContainsKey(f)).ToList();
            Assert.That(missingFeatures, Is.Empty, 
                $"Critical language features not covered by golden tests: {string.Join(", ", missingFeatures)}");
        }

        private List<string> AnalyzeLanguageFeatures(string content)
        {
            var features = new List<string>();

            if (content.Contains("function ")) features.Add("functions");
            if (content.Contains("pure function")) features.Add("pure_functions");
            if (content.Contains("uses [")) features.Add("effects");
            if (content.Contains("Result<")) features.Add("result_types");
            if (content.Contains("Ok(")) features.Add("ok_expressions");
            if (content.Contains("Error(")) features.Add("error_expressions");
            if (content.Contains("?")) features.Add("error_propagation");
            if (content.Contains("if ")) features.Add("if_statements");
            if (content.Contains("guard ")) features.Add("guard_statements");
            if (content.Contains("let ")) features.Add("let_statements");
            if (content.Contains("module ")) features.Add("modules");
            if (content.Contains("import ")) features.Add("imports");
            if (content.Contains("export ")) features.Add("exports");
            if (content.Contains("$\"")) features.Add("string_interpolation");
            if (content.Contains("&&") || content.Contains("||")) features.Add("logical_operators");
            if (content.Contains(">") || content.Contains("<")) features.Add("comparison_operators");
            if (content.Contains("+") || content.Contains("*")) features.Add("arithmetic_operators");

            return features;
        }
    }
}