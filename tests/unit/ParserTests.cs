using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowLang.Tests.Unit
{
    [TestFixture]
    public class ParserTests
    {
        private FlowLangParser CreateParser(string source)
        {
            var lexer = new FlowLangLexer(source);
            var tokens = lexer.Tokenize();
            return new FlowLangParser(tokens);
        }

        [Test]
        public void Parser_ShouldParseFunctionDeclaration()
        {
            // Arrange
            var source = "function add(a: int, b: int) -> int { return a + b }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<FunctionDeclaration>());
            
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Name, Is.EqualTo("add"));
            Assert.That(func.Parameters.Count, Is.EqualTo(2));
            Assert.That(func.Parameters[0].Name, Is.EqualTo("a"));
            Assert.That(func.Parameters[0].Type, Is.EqualTo("int"));
            Assert.That(func.Parameters[1].Name, Is.EqualTo("b"));
            Assert.That(func.Parameters[1].Type, Is.EqualTo("int"));
            Assert.That(func.ReturnType, Is.EqualTo("int"));
            Assert.That(func.Body.Count, Is.EqualTo(1));
            Assert.That(func.Body[0], Is.InstanceOf<ReturnStatement>());
        }

        [Test]
        public void Parser_ShouldParsePureFunction()
        {
            // Arrange
            var source = "pure function add(a: int, b: int) -> int { return a + b }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.IsPure, Is.True);
            Assert.That(func.Effects, Is.Null);
        }

        [Test]
        public void Parser_ShouldParseFunctionWithEffects()
        {
            // Arrange
            var source = "function saveUser(user: string) uses [Database, Logging] -> int { return 42 }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Effects, Is.Not.Null);
            Assert.That(func.Effects.Effects.Count, Is.EqualTo(2));
            Assert.That(func.Effects.Effects[0], Is.EqualTo("Database"));
            Assert.That(func.Effects.Effects[1], Is.EqualTo("Logging"));
        }

        [Test]
        public void Parser_ShouldParseResultType()
        {
            // Arrange
            var source = "function divide(a: int, b: int) -> Result<int, string> { return Ok(42) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.ReturnType, Is.EqualTo("Result<int, string>"));
        }

        [Test]
        public void Parser_ShouldParseBinaryExpressions()
        {
            // Arrange
            var source = "function test() -> int { return a + b * c }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<BinaryExpression>());
            
            var expr = (BinaryExpression)returnStmt.Expression;
            Assert.That(expr.Operator, Is.EqualTo("+"));
            Assert.That(expr.Left, Is.InstanceOf<Identifier>());
            Assert.That(expr.Right, Is.InstanceOf<BinaryExpression>());
        }

        [Test]
        public void Parser_ShouldParseUnaryExpressions()
        {
            // Arrange
            var source = "function test() -> bool { return !condition }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<UnaryExpression>());
            
            var expr = (UnaryExpression)returnStmt.Expression;
            Assert.That(expr.Operator, Is.EqualTo("!"));
            Assert.That(expr.Operand, Is.InstanceOf<Identifier>());
        }

        [Test]
        public void Parser_ShouldParseIfStatement()
        {
            // Arrange
            var source = @"function test() -> int { 
                if condition { 
                    return 1 
                } else { 
                    return 2 
                }
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(1));
            Assert.That(func.Body[0], Is.InstanceOf<IfStatement>());
            
            var ifStmt = (IfStatement)func.Body[0];
            Assert.That(ifStmt.Condition, Is.InstanceOf<Identifier>());
            Assert.That(ifStmt.ThenBody.Count, Is.EqualTo(1));
            Assert.That(ifStmt.ElseBody, Is.Not.Null);
            Assert.That(ifStmt.ElseBody.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parser_ShouldParseGuardStatement()
        {
            // Arrange
            var source = @"function test() -> int { 
                guard condition else { 
                    return 0 
                }
                return 1
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(2));
            Assert.That(func.Body[0], Is.InstanceOf<GuardStatement>());
            
            var guardStmt = (GuardStatement)func.Body[0];
            Assert.That(guardStmt.Condition, Is.InstanceOf<Identifier>());
            Assert.That(guardStmt.ElseBody.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parser_ShouldParseLetStatement()
        {
            // Arrange
            var source = @"function test() -> int { 
                let x = 42
                return x
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(2));
            Assert.That(func.Body[0], Is.InstanceOf<LetStatement>());
            
            var letStmt = (LetStatement)func.Body[0];
            Assert.That(letStmt.Name, Is.EqualTo("x"));
            Assert.That(letStmt.Expression, Is.InstanceOf<NumberLiteral>());
        }

        [Test]
        public void Parser_ShouldParseFunctionCall()
        {
            // Arrange
            var source = "function test() -> int { return add(1, 2) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<FunctionCall>());
            
            var call = (FunctionCall)returnStmt.Expression;
            Assert.That(call.Name, Is.EqualTo("add"));
            Assert.That(call.Arguments.Count, Is.EqualTo(2));
            Assert.That(call.Arguments[0], Is.InstanceOf<NumberLiteral>());
            Assert.That(call.Arguments[1], Is.InstanceOf<NumberLiteral>());
        }

        [Test]
        public void Parser_ShouldParseOkExpression()
        {
            // Arrange
            var source = "function test() -> Result<int, string> { return Ok(42) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<OkExpression>());
            
            var okExpr = (OkExpression)returnStmt.Expression;
            Assert.That(okExpr.Value, Is.InstanceOf<NumberLiteral>());
        }

        [Test]
        public void Parser_ShouldParseErrorExpression()
        {
            // Arrange
            var source = "function test() -> Result<int, string> { return Error(\"failed\") }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<ErrorExpression>());
            
            var errorExpr = (ErrorExpression)returnStmt.Expression;
            Assert.That(errorExpr.Value, Is.InstanceOf<StringLiteral>());
        }

        [Test]
        public void Parser_ShouldParseErrorPropagation()
        {
            // Arrange
            var source = "function test() -> Result<int, string> { let x = getValue()? return Ok(x) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            Assert.That(func.Body.Count, Is.EqualTo(2));
            var letStmt = (LetStatement)func.Body[0];
            Assert.That(letStmt.Expression, Is.InstanceOf<ErrorPropagationExpression>());
            
            var errorProp = (ErrorPropagationExpression)letStmt.Expression;
            Assert.That(errorProp.Expression, Is.InstanceOf<FunctionCall>());
        }

        [Test]
        public void Parser_ShouldParseStringInterpolation()
        {
            // Arrange
            var source = "function test() -> string { return $\"Hello {name}!\" }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<StringInterpolation>());
            
            var interp = (StringInterpolation)returnStmt.Expression;
            Assert.That(interp.Parts.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Parser_ShouldParseModuleDeclaration()
        {
            // Arrange
            var source = @"module Math {
                function add(a: int, b: int) -> int { return a + b }
            }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<ModuleDeclaration>());
            
            var module = (ModuleDeclaration)program.Statements[0];
            Assert.That(module.Name, Is.EqualTo("Math"));
            Assert.That(module.Body.Count, Is.EqualTo(1));
            Assert.That(module.Body[0], Is.InstanceOf<FunctionDeclaration>());
        }

        [Test]
        public void Parser_ShouldParseImportStatement()
        {
            // Arrange
            var source = "import Math.{add, subtract}";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<ImportStatement>());
            
            var import = (ImportStatement)program.Statements[0];
            Assert.That(import.ModuleName, Is.EqualTo("Math"));
            Assert.That(import.SpecificImports, Is.Not.Null);
            Assert.That(import.SpecificImports.Count, Is.EqualTo(2));
            Assert.That(import.SpecificImports[0], Is.EqualTo("add"));
            Assert.That(import.SpecificImports[1], Is.EqualTo("subtract"));
        }

        [Test]
        public void Parser_ShouldParseWildcardImport()
        {
            // Arrange
            var source = "import Math.*";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var import = (ImportStatement)program.Statements[0];
            Assert.That(import.ModuleName, Is.EqualTo("Math"));
            Assert.That(import.IsWildcard, Is.True);
        }

        [Test]
        public void Parser_ShouldParseExportStatement()
        {
            // Arrange
            var source = "export {add, subtract}";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.InstanceOf<ExportStatement>());
            
            var export = (ExportStatement)program.Statements[0];
            Assert.That(export.ExportedNames.Count, Is.EqualTo(2));
            Assert.That(export.ExportedNames[0], Is.EqualTo("add"));
            Assert.That(export.ExportedNames[1], Is.EqualTo("subtract"));
        }

        [Test]
        public void Parser_ShouldParseQualifiedFunctionCall()
        {
            // Arrange
            var source = "function test() -> int { return Math.add(1, 2) }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<FunctionCall>());
            
            var call = (FunctionCall)returnStmt.Expression;
            Assert.That(call.Name, Is.EqualTo("Math.add"));
        }

        [Test]
        public void Parser_ShouldHandleOperatorPrecedence()
        {
            // Arrange
            var source = "function test() -> bool { return a + b * c > d && e || f }";
            var parser = CreateParser(source);

            // Act
            var program = parser.Parse();

            // Assert
            var func = (FunctionDeclaration)program.Statements[0];
            var returnStmt = (ReturnStatement)func.Body[0];
            Assert.That(returnStmt.Expression, Is.InstanceOf<BinaryExpression>());
            
            // Should parse as: ((a + (b * c)) > d) && e) || f
            var expr = (BinaryExpression)returnStmt.Expression;
            Assert.That(expr.Operator, Is.EqualTo("||"));
        }

        [Test]
        public void Parser_ShouldThrowOnInvalidSyntax()
        {
            // Arrange
            var source = "function test( -> int";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }

        [Test]
        public void Parser_ShouldThrowOnMissingFunctionBody()
        {
            // Arrange
            var source = "function test() -> int";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }

        [Test]
        public void Parser_ShouldThrowOnInvalidEffectName()
        {
            // Arrange
            var source = "function test() uses [InvalidEffect] -> int { return 42 }";
            var parser = CreateParser(source);

            // Act & Assert
            Assert.Throws<Exception>(() => parser.Parse());
        }
    }
}