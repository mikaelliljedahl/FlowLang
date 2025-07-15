using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cadenza.Tests.Unit
{
    [TestFixture]
    public class CodeGeneratorTests
    {
        private CSharpGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _generator = new CSharpGenerator();
        }

        [Test]
        public void CodeGenerator_ShouldGenerateSimpleFunction()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "add",
                new List<Parameter> 
                { 
                    new("a", "int"), 
                    new("b", "int") 
                },
                "int",
                new List<ASTNode> 
                { 
                    new ReturnStatement(
                        new BinaryExpression(
                            new Identifier("a"),
                            "+",
                            new Identifier("b")
                        )
                    )
                }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("public static int add(int a, int b)"));
            Assert.That(code, Contains.Substring("return a + b;"));
        }

        [Test]
        public void CodeGenerator_ShouldGeneratePureFunction()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "add",
                new List<Parameter> { new("a", "int"), new("b", "int") },
                "int",
                new List<ASTNode> { new ReturnStatement(new NumberLiteral(42)) },
                null,
                true // isPure
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("/// Pure function - no side effects"));
            Assert.That(code, Contains.Substring("public static int add(int a, int b)"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateFunctionWithEffects()
        {
            // Arrange
            var effects = new EffectAnnotation(new List<string> { "Database", "Logging" });
            var func = new FunctionDeclaration(
                "saveUser",
                new List<Parameter> { new("user", "string") },
                "int",
                new List<ASTNode> { new ReturnStatement(new NumberLiteral(42)) },
                effects
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("/// Effects: Database, Logging"));
            Assert.That(code, Contains.Substring("public static int saveUser(string user)"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateResultClass()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "divide",
                new List<Parameter> { new("a", "int"), new("b", "int") },
                "Result<int, string>",
                new List<ASTNode> { new ReturnStatement(new OkExpression(new NumberLiteral(42))) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("public class Result<T, E>"));
            Assert.That(code, Contains.Substring("public T Value"));
            Assert.That(code, Contains.Substring("public E ErrorValue"));
            Assert.That(code, Contains.Substring("public bool IsError"));
            Assert.That(code, Contains.Substring("public static Result<T, E> Ok(T value)"));
            Assert.That(code, Contains.Substring("public static Result<T, E> Error(E error)"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateOkExpression()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "Result<int, string>",
                new List<ASTNode> { new ReturnStatement(new OkExpression(new NumberLiteral(42))) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return Result.Ok(42);"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateErrorExpression()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "Result<int, string>",
                new List<ASTNode> { new ReturnStatement(new ErrorExpression(new StringLiteral("failed"))) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return Result.Error(\"failed\");"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateIfStatement()
        {
            // Arrange
            var ifStmt = new IfStatement(
                new Identifier("condition"),
                new List<ASTNode> { new ReturnStatement(new NumberLiteral(1)) },
                new List<ASTNode> { new ReturnStatement(new NumberLiteral(2)) }
            );
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { ifStmt }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("if (condition)"));
            Assert.That(code, Contains.Substring("return 1;"));
            Assert.That(code, Contains.Substring("else"));
            Assert.That(code, Contains.Substring("return 2;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateGuardStatement()
        {
            // Arrange
            var guardStmt = new GuardStatement(
                new Identifier("condition"),
                new List<ASTNode> { new ReturnStatement(new NumberLiteral(0)) }
            );
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { guardStmt, new ReturnStatement(new NumberLiteral(1)) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("if (!(condition))"));
            Assert.That(code, Contains.Substring("return 0;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateLetStatement()
        {
            // Arrange
            var letStmt = new LetStatement("x", new NumberLiteral(42));
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { letStmt, new ReturnStatement(new Identifier("x")) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("var x = 42;"));
            Assert.That(code, Contains.Substring("return x;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateErrorPropagationInLetStatement()
        {
            // Arrange
            var errorProp = new ErrorPropagationExpression(
                new FunctionCall("getValue", new List<ASTNode>())
            );
            var letStmt = new LetStatement("x", errorProp);
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "Result<int, string>",
                new List<ASTNode> { letStmt, new ReturnStatement(new OkExpression(new Identifier("x"))) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("var x_result = getValue();"));
            Assert.That(code, Contains.Substring("if (x_result.IsError)"));
            Assert.That(code, Contains.Substring("return x_result;"));
            Assert.That(code, Contains.Substring("var x = x_result.Value;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateBinaryExpressions()
        {
            // Arrange
            var expr = new BinaryExpression(
                new Identifier("a"),
                "+",
                new BinaryExpression(
                    new Identifier("b"),
                    "*",
                    new Identifier("c")
                )
            );
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { new ReturnStatement(expr) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return a + b * c;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateUnaryExpressions()
        {
            // Arrange
            var expr = new UnaryExpression("!", new Identifier("condition"));
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "bool",
                new List<ASTNode> { new ReturnStatement(expr) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return !condition;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateFunctionCall()
        {
            // Arrange
            var call = new FunctionCall("add", new List<ASTNode> { new NumberLiteral(1), new NumberLiteral(2) });
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { new ReturnStatement(call) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return add(1, 2);"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateStringInterpolation()
        {
            // Arrange
            var interp = new StringInterpolation(new List<ASTNode>
            {
                new StringLiteral("Hello "),
                new Identifier("name"),
                new StringLiteral("!")
            });
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "string",
                new List<ASTNode> { new ReturnStatement(interp) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("string.Format"));
            Assert.That(code, Contains.Substring("\"Hello {0}!\""));
            Assert.That(code, Contains.Substring("name"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateModuleAsStaticClass()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "add",
                new List<Parameter> { new("a", "int"), new("b", "int") },
                "int",
                new List<ASTNode> { new ReturnStatement(new BinaryExpression(new Identifier("a"), "+", new Identifier("b"))) }
            );
            var module = new ModuleDeclaration(
                "Math",
                new List<ASTNode> { func }
            );
            var program = new Program(new List<ASTNode> { module });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("namespace Math"));
            Assert.That(code, Contains.Substring("public static class MathModule"));
            Assert.That(code, Contains.Substring("public static int add(int a, int b)"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateQualifiedFunctionCall()
        {
            // Arrange
            var call = new FunctionCall("Math.add", new List<ASTNode> { new NumberLiteral(1), new NumberLiteral(2) });
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { new ReturnStatement(call) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return MathModule.add(1, 2);"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateXmlDocumentation()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "add",
                new List<Parameter> { new("a", "int"), new("b", "int") },
                "int",
                new List<ASTNode> { new ReturnStatement(new NumberLiteral(42)) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("/// <summary>"));
            Assert.That(code, Contains.Substring("/// </summary>"));
            Assert.That(code, Contains.Substring("/// <param name=\"a\">Parameter of type int</param>"));
            Assert.That(code, Contains.Substring("/// <param name=\"b\">Parameter of type int</param>"));
            Assert.That(code, Contains.Substring("/// <returns>Returns int</returns>"));
        }

        [Test]
        public void CodeGenerator_ShouldHandleComplexOperatorPrecedence()
        {
            // Arrange
            var expr = new BinaryExpression(
                new BinaryExpression(
                    new Identifier("a"),
                    "+",
                    new BinaryExpression(new Identifier("b"), "*", new Identifier("c"))
                ),
                ">",
                new Identifier("d")
            );
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "bool",
                new List<ASTNode> { new ReturnStatement(expr) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return a + b * c > d;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateValidCSharpSyntax()
        {
            // Arrange
            var func = new FunctionDeclaration(
                "complexFunction",
                new List<Parameter> { new("x", "int"), new("y", "string") },
                "Result<bool, string>",
                new List<ASTNode> 
                { 
                    new LetStatement("z", new BinaryExpression(new Identifier("x"), "+", new NumberLiteral(10))),
                    new IfStatement(
                        new BinaryExpression(new Identifier("z"), ">", new NumberLiteral(5)),
                        new List<ASTNode> { new ReturnStatement(new OkExpression(new Identifier("true"))) },
                        new List<ASTNode> { new ReturnStatement(new ErrorExpression(new Identifier("y"))) }
                    )
                }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert - Verify the generated code compiles
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            Assert.That(errors.Count, Is.EqualTo(0), 
                $"Generated code has compilation errors: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
        }

        [Test]
        public void CodeGenerator_ShouldGenerateComplexNestedStructures()
        {
            // Arrange
            var nestedIf = new IfStatement(
                new BinaryExpression(new Identifier("y"), ">", new NumberLiteral(0)),
                new List<ASTNode> { new ReturnStatement(new StringLiteral("positive")) },
                new List<ASTNode> { new ReturnStatement(new StringLiteral("negative")) }
            );
            var outerIf = new IfStatement(
                new BinaryExpression(new Identifier("x"), ">", new NumberLiteral(0)),
                new List<ASTNode> { nestedIf },
                new List<ASTNode> { new ReturnStatement(new StringLiteral("zero or negative")) }
            );
            var func = new FunctionDeclaration(
                "complexNested",
                new List<Parameter> { new("x", "int"), new("y", "int") },
                "string",
                new List<ASTNode> { outerIf }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("if (x > 0)"));
            Assert.That(code, Contains.Substring("if (y > 0)"));
            Assert.That(code, Contains.Substring("return \"positive\";"));
            Assert.That(code, Contains.Substring("return \"negative\";"));
            Assert.That(code, Contains.Substring("return \"zero or negative\";"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateModuleWithMultipleFunctions()
        {
            // Arrange
            var func1 = new FunctionDeclaration(
                "add",
                new List<Parameter> { new("a", "int"), new("b", "int") },
                "int",
                new List<ASTNode> { new ReturnStatement(new BinaryExpression(new Identifier("a"), "+", new Identifier("b"))) }
            );
            var func2 = new FunctionDeclaration(
                "multiply",
                new List<Parameter> { new("a", "int"), new("b", "int") },
                "int",
                new List<ASTNode> { new ReturnStatement(new BinaryExpression(new Identifier("a"), "*", new Identifier("b"))) }
            );
            var export = new ExportStatement(new List<string> { "add", "multiply" });
            var module = new ModuleDeclaration(
                "Math",
                new List<ASTNode> { func1, func2, export }
            );
            var program = new Program(new List<ASTNode> { module });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("namespace Math"));
            Assert.That(code, Contains.Substring("public static class MathModule"));
            Assert.That(code, Contains.Substring("public static int add(int a, int b)"));
            Assert.That(code, Contains.Substring("public static int multiply(int a, int b)"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateLogicalOperators()
        {
            // Arrange
            var expr = new BinaryExpression(
                new BinaryExpression(
                    new Identifier("a"),
                    "&&",
                    new Identifier("b")
                ),
                "||",
                new UnaryExpression("!", new Identifier("c"))
            );
            var func = new FunctionDeclaration(
                "testLogical",
                new List<Parameter> { new("a", "bool"), new("b", "bool"), new("c", "bool") },
                "bool",
                new List<ASTNode> { new ReturnStatement(expr) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return a && b || !c;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateComplexStringInterpolation()
        {
            // Arrange
            var interp = new StringInterpolation(new List<ASTNode>
            {
                new StringLiteral("User "),
                new Identifier("name"),
                new StringLiteral(" has "),
                new FunctionCall("getCount", new List<ASTNode>()),
                new StringLiteral(" messages")
            });
            var func = new FunctionDeclaration(
                "getMessage",
                new List<Parameter> { new("name", "string") },
                "string",
                new List<ASTNode> { new ReturnStatement(interp) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("string.Format"));
            Assert.That(code, Contains.Substring("\"User {0} has {1} messages\""));
            Assert.That(code, Contains.Substring("name, getCount()"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateMultipleLetStatements()
        {
            // Arrange
            var let1 = new LetStatement("x", new NumberLiteral(10));
            var let2 = new LetStatement("y", new NumberLiteral(20));
            var let3 = new LetStatement("sum", new BinaryExpression(new Identifier("x"), "+", new Identifier("y")));
            var returnStmt = new ReturnStatement(new Identifier("sum"));
            
            var func = new FunctionDeclaration(
                "calculate",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { let1, let2, let3, returnStmt }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("var x = 10;"));
            Assert.That(code, Contains.Substring("var y = 20;"));
            Assert.That(code, Contains.Substring("var sum = x + y;"));
            Assert.That(code, Contains.Substring("return sum;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateNestedFunctionCalls()
        {
            // Arrange
            var innerCall = new FunctionCall("getValue", new List<ASTNode> { new NumberLiteral(42) });
            var outerCall = new FunctionCall("processValue", new List<ASTNode> { innerCall });
            var func = new FunctionDeclaration(
                "nested",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { new ReturnStatement(outerCall) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return processValue(getValue(42));"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateComparisonOperators()
        {
            // Arrange
            var expr = new BinaryExpression(
                new BinaryExpression(new Identifier("a"), ">", new Identifier("b")),
                "&&",
                new BinaryExpression(new Identifier("c"), "<=", new Identifier("d"))
            );
            var func = new FunctionDeclaration(
                "compare",
                new List<Parameter> { new("a", "int"), new("b", "int"), new("c", "int"), new("d", "int") },
                "bool",
                new List<ASTNode> { new ReturnStatement(expr) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("return a > b && c <= d;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateImportStatements()
        {
            // Arrange
            var import1 = new ImportStatement("Math", null, true); // wildcard
            var import2 = new ImportStatement("Utils", new List<string> { "helper1", "helper2" }, false); // specific
            var func = new FunctionDeclaration(
                "test",
                new List<Parameter>(),
                "int",
                new List<ASTNode> { new ReturnStatement(new NumberLiteral(42)) }
            );
            var program = new Program(new List<ASTNode> { import1, import2, func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("using Math;"));
            Assert.That(code, Contains.Substring("using Utils;"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateComplexResultTypes()
        {
            // Arrange
            var nestedOk = new OkExpression(new OkExpression(new NumberLiteral(42)));
            var func = new FunctionDeclaration(
                "complexResult",
                new List<Parameter>(),
                "Result<Result<int, string>, bool>",
                new List<ASTNode> { new ReturnStatement(nestedOk) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("Result<Result<int, string>, bool>"));
            Assert.That(code, Contains.Substring("Result.Ok(Result.Ok(42))"));
        }

        [Test]
        public void CodeGenerator_ShouldGenerateAllMathOperators()
        {
            // Arrange
            var addExpr = new BinaryExpression(new Identifier("a"), "+", new Identifier("b"));
            var subExpr = new BinaryExpression(new Identifier("c"), "-", new Identifier("d"));
            var mulExpr = new BinaryExpression(new Identifier("e"), "*", new Identifier("f"));
            var divExpr = new BinaryExpression(new Identifier("g"), "/", new Identifier("h"));
            
            var complexExpr = new BinaryExpression(
                new BinaryExpression(addExpr, "+", subExpr),
                "+",
                new BinaryExpression(mulExpr, "+", divExpr)
            );
            
            var func = new FunctionDeclaration(
                "mathOps",
                new List<Parameter> 
                { 
                    new("a", "int"), new("b", "int"), new("c", "int"), new("d", "int"),
                    new("e", "int"), new("f", "int"), new("g", "int"), new("h", "int")
                },
                "int",
                new List<ASTNode> { new ReturnStatement(complexExpr) }
            );
            var program = new Program(new List<ASTNode> { func });

            // Act
            var syntaxTree = _generator.GenerateFromAST(program);
            var code = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();

            // Assert
            Assert.That(code, Contains.Substring("a + b"));
            Assert.That(code, Contains.Substring("c - d"));
            Assert.That(code, Contains.Substring("e * f"));
            Assert.That(code, Contains.Substring("g / h"));
        }
    }
}