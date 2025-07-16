using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Core;

namespace Cadenza.Core.Tests;

[TestFixture]
public class ParserTests
{
    private CadenzaParser GetParser(string source)
    {
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        return new CadenzaParser(tokens);
    }

    [Test]
    public void Parser_EmptyProgram_ShouldReturnEmptyProgram()
    {
        // Arrange
        var parser = GetParser("");

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(0, program.Statements.Count);
    }

    [Test]
    public void Parser_SimpleFunctionDeclaration_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function test() -> int { return 42; }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual("test", func.Name);
        Assert.AreEqual("int", func.ReturnType);
        Assert.AreEqual(0, func.Parameters.Count);
        Assert.AreEqual(1, func.Body.Count);
        
        var returnStmt = func.Body[0] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsInstanceOf<NumberLiteral>(returnStmt.Expression);
        Assert.AreEqual(42, ((NumberLiteral)returnStmt.Expression).Value);
    }

    [Test]
    public void Parser_FunctionWithParameters_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function add(x: int, y: int) -> int { return x + y; }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual("add", func.Name);
        Assert.AreEqual("int", func.ReturnType);
        Assert.AreEqual(2, func.Parameters.Count);
        
        Assert.AreEqual("x", func.Parameters[0].Name);
        Assert.AreEqual("int", func.Parameters[0].Type);
        Assert.AreEqual("y", func.Parameters[1].Name);
        Assert.AreEqual("int", func.Parameters[1].Type);
    }

    [Test]
    public void Parser_PureFunction_ShouldParseCorrectly()
    {
        // Arrange
        var source = "pure function add(x: int, y: int) -> int { return x + y; }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual("add", func.Name);
        Assert.IsTrue(func.IsPure);
    }

    [Test]
    public void Parser_FunctionWithEffects_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function getData() uses [Database, Network] -> Result<string, string> { return Ok(\"data\"); }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual("getData", func.Name);
        Assert.IsNotNull(func.Effects);
        Assert.AreEqual(2, func.Effects.Count);
        Assert.Contains("Database", func.Effects);
        Assert.Contains("Network", func.Effects);
    }

    [Test]
    public void Parser_LetStatement_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function test() -> int { let x = 42; return x; }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual(2, func.Body.Count);
        
        var letStmt = func.Body[0] as LetStatement;
        Assert.IsNotNull(letStmt);
        Assert.AreEqual("x", letStmt.Name);
        Assert.IsNull(letStmt.Type); // Type inference
        Assert.IsInstanceOf<NumberLiteral>(letStmt.Expression);
    }

    [Test]
    public void Parser_LetStatementWithType_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function test() -> int { let x: int = 42; return x; }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        
        var letStmt = func.Body[0] as LetStatement;
        Assert.IsNotNull(letStmt);
        Assert.AreEqual("x", letStmt.Name);
        Assert.AreEqual("int", letStmt.Type);
        Assert.IsInstanceOf<NumberLiteral>(letStmt.Expression);
    }

    [Test]
    public void Parser_IfStatement_ShouldParseCorrectly()
    {
        // Arrange
        var source = @"
function test(x: int) -> int {
    if (x > 0) {
        return x;
    } else {
        return -x;
    }
}";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual(1, func.Body.Count);
        
        var ifStmt = func.Body[0] as IfStatement;
        Assert.IsNotNull(ifStmt);
        Assert.IsInstanceOf<ComparisonExpression>(ifStmt.Condition);
        Assert.AreEqual(1, ifStmt.ThenBody.Count);
        Assert.AreEqual(1, ifStmt.ElseBody.Count);
    }

    [Test]
    public void Parser_BinaryExpression_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function test() -> int { return 2 + 3 * 4; }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        
        var returnStmt = func.Body[0] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsInstanceOf<ArithmeticExpression>(returnStmt.Expression);
        
        var addExpr = returnStmt.Expression as ArithmeticExpression;
        Assert.AreEqual("+", addExpr.Operator);
        Assert.IsInstanceOf<NumberLiteral>(addExpr.Left);
        Assert.IsInstanceOf<ArithmeticExpression>(addExpr.Right); // 3 * 4
    }

    [Test]
    public void Parser_CallExpression_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function test() -> int { return add(2, 3); }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        
        var returnStmt = func.Body[0] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsInstanceOf<CallExpression>(returnStmt.Expression);
        
        var callExpr = returnStmt.Expression as CallExpression;
        Assert.AreEqual("add", callExpr.Name);
        Assert.AreEqual(2, callExpr.Arguments.Count);
        Assert.IsInstanceOf<NumberLiteral>(callExpr.Arguments[0]);
        Assert.IsInstanceOf<NumberLiteral>(callExpr.Arguments[1]);
    }

    [Test]
    public void Parser_ResultExpression_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function test() -> Result<int, string> { return Ok(42); }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        
        var returnStmt = func.Body[0] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsInstanceOf<ResultExpression>(returnStmt.Expression);
        
        var resultExpr = returnStmt.Expression as ResultExpression;
        Assert.AreEqual("Ok", resultExpr.Type);
        Assert.IsInstanceOf<NumberLiteral>(resultExpr.Value);
    }

    [Test]
    public void Parser_ErrorPropagation_ShouldParseCorrectly()
    {
        // Arrange
        var source = "function test() -> Result<int, string> { let x = riskyFunction()?; return Ok(x); }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        
        var letStmt = func.Body[0] as LetStatement;
        Assert.IsNotNull(letStmt);
        Assert.IsInstanceOf<ErrorPropagation>(letStmt.Expression);
        
        var errorProp = letStmt.Expression as ErrorPropagation;
        Assert.IsInstanceOf<CallExpression>(errorProp.Expression);
    }

    [Test]
    public void Parser_MatchExpression_ShouldParseCorrectly()
    {
        // Arrange
        var source = @"
function test() -> int {
    let result = Ok(42);
    return match result {
        Ok(value) -> value
        Error(err) -> 0
    };
}";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual(2, func.Body.Count);
        
        var returnStmt = func.Body[1] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsInstanceOf<MatchExpression>(returnStmt.Expression);
        
        var matchExpr = returnStmt.Expression as MatchExpression;
        Assert.AreEqual(2, matchExpr.Cases.Count);
        
        var okCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "Ok");
        Assert.IsNotNull(okCase);
        Assert.AreEqual("value", okCase.Variable);
        
        var errorCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "Error");
        Assert.IsNotNull(errorCase);
        Assert.AreEqual("err", errorCase.Variable);
    }

    [Test]
    public void Parser_GuardStatement_ShouldParseCorrectly()
    {
        // Arrange
        var source = @"
function test(x: int) -> int {
    guard x > 0 else {
        return -1;
    }
    return x;
}";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.AreEqual(2, func.Body.Count);
        
        var guardStmt = func.Body[0] as GuardStatement;
        Assert.IsNotNull(guardStmt);
        Assert.IsInstanceOf<ComparisonExpression>(guardStmt.Condition);
        Assert.AreEqual(1, guardStmt.ElseBody.Count);
    }

    [Test]
    public void Parser_ModuleDeclaration_ShouldParseCorrectly()
    {
        // Arrange
        var source = @"
module Math {
    function add(x: int, y: int) -> int {
        return x + y;
    }
    
    export function multiply(x: int, y: int) -> int {
        return x * y;
    }
}";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var module = program.Statements[0] as ModuleDeclaration;
        Assert.IsNotNull(module);
        Assert.AreEqual("Math", module.Name);
        Assert.AreEqual(2, module.Body.Count);
        
        // Check that the exported function is marked as exported
        var exportedFunc = module.Body[1] as FunctionDeclaration;
        Assert.IsNotNull(exportedFunc);
        Assert.AreEqual("multiply", exportedFunc.Name);
        Assert.IsTrue(exportedFunc.IsExported);
    }

    [Test]
    public void Parser_ImportStatement_ShouldParseCorrectly()
    {
        // Arrange
        var source = "import Math.{ add, multiply }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var import = program.Statements[0] as ImportStatement;
        Assert.IsNotNull(import);
        Assert.AreEqual("Math", import.ModuleName);
        Assert.IsNotNull(import.SpecificImports);
        Assert.AreEqual(2, import.SpecificImports.Count);
        Assert.Contains("add", import.SpecificImports);
        Assert.Contains("multiply", import.SpecificImports);
        Assert.IsFalse(import.IsWildcard);
    }

    [Test]
    public void Parser_StringInterpolation_ShouldParseCorrectly()
    {
        // Arrange
        var source = @"function greet(name: string) -> string { return $""Hello {name}!""; }";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        
        var returnStmt = func.Body[0] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsInstanceOf<StringInterpolation>(returnStmt.Expression);
        
        var stringInterp = returnStmt.Expression as StringInterpolation;
        Assert.Greater(stringInterp.Parts.Count, 0);
    }

    [Test]
    public void Parser_SpecificationBlock_ShouldParseCorrectly()
    {
        // Arrange
        var source = @"
/*spec
intent: This function adds two numbers
rules:
- Both parameters must be integers
- Result is the sum of the inputs
spec*/
function add(x: int, y: int) -> int {
    return x + y;
}";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        var func = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func);
        Assert.IsNotNull(func.Specification);
        Assert.IsTrue(func.Specification.Intent.Contains("adds two numbers"));
    }

    [Test]
    public void Parser_InvalidSyntax_ShouldThrowException()
    {
        // Arrange
        var source = "function test( { return 42; }"; // Missing closing parenthesis
        var parser = GetParser(source);

        // Act & Assert
        Assert.Throws<System.Exception>(() => parser.Parse());
    }

    [Test]
    public void Parser_MultipleStatements_ShouldParseCorrectly()
    {
        // Arrange
        var source = @"
function add(x: int, y: int) -> int {
    return x + y;
}

function multiply(x: int, y: int) -> int {
    return x * y;
}";
        var parser = GetParser(source);

        // Act
        var program = parser.Parse();

        // Assert
        Assert.IsNotNull(program);
        Assert.AreEqual(2, program.Statements.Count);
        
        var func1 = program.Statements[0] as FunctionDeclaration;
        Assert.IsNotNull(func1);
        Assert.AreEqual("add", func1.Name);
        
        var func2 = program.Statements[1] as FunctionDeclaration;
        Assert.IsNotNull(func2);
        Assert.AreEqual("multiply", func2.Name);
    }
}