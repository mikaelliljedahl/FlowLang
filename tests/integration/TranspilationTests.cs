using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Cadenza.Core;
using Cadenza.Tests.Framework;

namespace Cadenza.Tests.Integration
{
    [TestFixture]
    public class TranspilationTests : TestBase
    {
        private CadenzaTranspiler _transpiler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _transpiler = new CadenzaTranspiler();
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
            
            return syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
        }

        [Test]
        public void Transpiler_ShouldTranspileSimpleFunction()
        {
            // Arrange
            var input = "function add(a: int, b: int) -> int { return a + b }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("public static int add(int a, int b)"));
            Assert.That(output, Contains.Substring("return a + b;"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspilePureFunction()
        {
            // Arrange
            var input = "pure function multiply(x: int, y: int) -> int { return x * y }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("/// Pure function - no side effects"));
            Assert.That(output, Contains.Substring("public static int multiply(int x, int y)"));
            Assert.That(output, Contains.Substring("return x * y;"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileFunctionWithEffects()
        {
            // Arrange
            var input = @"function saveUser(name: string) uses [Database, Logging] -> int { 
                return 42 
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("/// Effects: Database, Logging"));
            Assert.That(output, Contains.Substring("public static int saveUser(string name)"));
            Assert.That(output, Contains.Substring("return 42;"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileResultTypes()
        {
            // Arrange
            var input = @"function divide(a: int, b: int) -> Result<int, string> { 
                if b == 0 {
                    return Error(""Division by zero"")
                } else {
                    return Ok(a / b)
                }
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("public class Result<T, E>"));
            Assert.That(output, Contains.Substring("Result<int, string> divide(int a, int b)"));
            Assert.That(output, Contains.Substring("Result.Error(\"Division by zero\")"));
            Assert.That(output, Contains.Substring("Result.Ok(a / b)"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileControlFlow()
        {
            // Arrange
            var input = @"function testControlFlow(x: int) -> string {
                if x > 10 {
                    return ""large""
                } else if x > 5 {
                    return ""medium""
                } else {
                    return ""small""
                }
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("if (x > 10)"));
            Assert.That(output, Contains.Substring("return \"large\";"));
            Assert.That(output, Contains.Substring("else"));
            Assert.That(output, Contains.Substring("if (x > 5)"));
            Assert.That(output, Contains.Substring("return \"medium\";"));
            Assert.That(output, Contains.Substring("return \"small\";"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileGuardStatements()
        {
            // Arrange
            var input = @"function validateInput(x: int) -> int {
                guard x > 0 else {
                    return -1
                }
                return x * 2
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("if (!(x > 0))"));
            Assert.That(output, Contains.Substring("return -1;"));
            Assert.That(output, Contains.Substring("return x * 2;"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileLetStatements()
        {
            // Arrange
            var input = @"function calculateTotal(baseValue: int, tax: int) -> int {
                let subtotal = baseValue + tax
                let discount = subtotal / 10
                return subtotal - discount
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("var subtotal = baseValue + tax;"));
            Assert.That(output, Contains.Substring("var discount = subtotal / 10;"));
            Assert.That(output, Contains.Substring("return subtotal - discount;"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileErrorPropagation()
        {
            // Arrange
            var input = @"function processData(input: string) -> Result<int, string> {
                let result = parseNumber(input)?
                return Ok(result * 2)
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("var result_result = parseNumber(input);"));
            Assert.That(output, Contains.Substring("if (result_result.IsError)"));
            Assert.That(output, Contains.Substring("return result_result;"));
            Assert.That(output, Contains.Substring("var result = result_result.Value;"));
            Assert.That(output, Contains.Substring("return Result.Ok(result * 2);"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileStringInterpolation()
        {
            // Arrange
            var input = @"function greet(name: string, age: int) -> string {
                return $""Hello {name}, you are {age} years old!""
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("return $\"Hello {name}, you are {age} years old!\";"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileModules()
        {
            // Arrange
            var input = @"module Math {
                function add(a: int, b: int) -> int {
                    return a + b
                }
                
                function multiply(a: int, b: int) -> int {
                    return a * b
                }
                
                export {add, multiply}
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("namespace Math"));
            Assert.That(output, Contains.Substring("public static class Math"));
            Assert.That(output, Contains.Substring("public static int add(int a, int b)"));
            Assert.That(output, Contains.Substring("public static int multiply(int a, int b)"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileImportsAndQualifiedCalls()
        {
            // Arrange
            var input = @"import Math.{add}
            
            function calculate(x: int, y: int) -> int {
                return Math.add(x, y) * 2
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("using Math;"));
            Assert.That(output, Contains.Substring("Cadenza.Modules.Math.Math.add(x, y)"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileComplexExpressions()
        {
            // Arrange
            var input = @"function complexCalculation(a: int, b: int, c: int) -> bool {
                return (a + b * c) > 10 && (a - b) < c || c == 0
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("return (a + b * c) > 10 && (a - b) < c || c == 0;"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldTranspileFunctionCalls()
        {
            // Arrange
            var input = @"function fibonacci(n: int) -> int {
                if n <= 1 {
                    return n
                } else {
                    return fibonacci(n - 1) + fibonacci(n - 2)
                }
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("return fibonacci(n - 1) + fibonacci(n - 2);"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldHandleMultipleFunctions()
        {
            // Arrange
            var input = @"pure function add(a: int, b: int) -> int {
                return a + b
            }
            
            pure function multiply(a: int, b: int) -> int {
                return a * b
            }
            
            function calculate(x: int, y: int) -> int {
                let sum = add(x, y)
                return multiply(sum, 2)
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("public static int add(int a, int b)"));
            Assert.That(output, Contains.Substring("public static int multiply(int a, int b)"));
            Assert.That(output, Contains.Substring("public static int calculate(int x, int y)"));
            Assert.That(output, Contains.Substring("var sum = add(x, y);"));
            Assert.That(output, Contains.Substring("return multiply(sum, 2);"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldHandleNestedControlFlow()
        {
            // Arrange
            var input = @"function nestedLogic(x: int, y: int) -> string {
                if x > 0 {
                    if y > 0 {
                        return ""both positive""
                    } else {
                        return ""x positive, y negative""
                    }
                } else {
                    if y > 0 {
                        return ""x negative, y positive""
                    } else {
                        return ""both negative""
                    }
                }
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("if (x > 0)"));
            Assert.That(output, Contains.Substring("if (y > 0)"));
            Assert.That(output, Contains.Substring("return \"both positive\";"));
            Assert.That(output, Contains.Substring("return \"both negative\";"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldHandleEmptyFunctionBody()
        {
            // Arrange
            var input = @"function doNothing() -> int {
                return 0
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("public static int doNothing()"));
            Assert.That(output, Contains.Substring("return 0;"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldPreserveComments()
        {
            // Arrange - Comments should be ignored during parsing but XML docs should be generated
            var input = @"// This is a pure function that adds two numbers
            pure function add(a: int, b: int) -> int {
                return a + b // Simple addition
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("/// Pure function - no side effects"));
            Assert.That(output, Contains.Substring("public static int add(int a, int b)"));
            ValidateGeneratedCode(output);
        }

        [Test]
        public void Transpiler_ShouldHandleLogicalOperators()
        {
            // Arrange
            var input = @"function testLogical(a: bool, b: bool, c: bool) -> bool {
                return a && b || !c
            }";

            // Act
            var output = TranspileCodeDirectly(input);

            // Assert
            Assert.That(output, Contains.Substring("return a && b || !c;"));
            ValidateGeneratedCode(output);
        }

        private void ValidateGeneratedCode(string generatedCode)
        {
            // Verify that the generated code compiles successfully
            var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);
            
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Cadenza.Core.CadenzaLexer).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (errors.Any())
            {
                var errorMessages = string.Join("\n", errors.Select(e => $"{e.GetMessage()} at {e.Location}"));
                Assert.Fail($"Generated code has compilation errors:\n{errorMessages}\n\nGenerated code:\n{generatedCode}");
            }
        }
    }
}