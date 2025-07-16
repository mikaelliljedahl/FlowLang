using NUnit.Framework;
using System.Collections.Generic;
using Cadenza.Core;

namespace Cadenza.Core.Tests;

[TestFixture]
public class AstTests
{
    [Test]
    public void Program_Creation_ShouldStoreStatements()
    {
        // Arrange
        var statements = new List<ASTNode>
        {
            new FunctionDeclaration("test", new List<Parameter>(), "int", new List<ASTNode>()),
            new ReturnStatement(new NumberLiteral(42))
        };

        // Act
        var program = new ProgramNode(statements);

        // Assert
        Assert.AreEqual(2, program.Statements.Count);
        Assert.IsInstanceOf<FunctionDeclaration>(program.Statements[0]);
        Assert.IsInstanceOf<ReturnStatement>(program.Statements[1]);
    }

    [Test]
    public void FunctionDeclaration_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "testFunction";
        var parameters = new List<Parameter> { new Parameter("x", "int"), new Parameter("y", "string") };
        var returnType = "int";
        var body = new List<ASTNode> { new ReturnStatement(new NumberLiteral(42)) };
        var isPure = true;
        var effects = new List<string> { "Database", "Network" };
        var isExported = true;

        // Act
        var func = new FunctionDeclaration(name, parameters, returnType, body, isPure, effects, isExported);

        // Assert
        Assert.AreEqual(name, func.Name);
        Assert.AreEqual(parameters, func.Parameters);
        Assert.AreEqual(returnType, func.ReturnType);
        Assert.AreEqual(body, func.Body);
        Assert.AreEqual(isPure, func.IsPure);
        Assert.AreEqual(effects, func.Effects);
        Assert.AreEqual(isExported, func.IsExported);
    }

    [Test]
    public void Parameter_Creation_ShouldStoreNameAndType()
    {
        // Arrange
        var name = "testParam";
        var type = "string";

        // Act
        var parameter = new Parameter(name, type);

        // Assert
        Assert.AreEqual(name, parameter.Name);
        Assert.AreEqual(type, parameter.Type);
    }

    [Test]
    public void LetStatement_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "testVar";
        var type = "int";
        var expression = new NumberLiteral(42);

        // Act
        var letStmt = new LetStatement(name, type, expression);

        // Assert
        Assert.AreEqual(name, letStmt.Name);
        Assert.AreEqual(type, letStmt.Type);
        Assert.AreEqual(expression, letStmt.Expression);
    }

    [Test]
    public void IfStatement_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var condition = new BooleanLiteral(true);
        var thenBody = new List<ASTNode> { new ReturnStatement(new NumberLiteral(1)) };
        var elseBody = new List<ASTNode> { new ReturnStatement(new NumberLiteral(0)) };

        // Act
        var ifStmt = new IfStatement(condition, thenBody, elseBody);

        // Assert
        Assert.AreEqual(condition, ifStmt.Condition);
        Assert.AreEqual(thenBody, ifStmt.ThenBody);
        Assert.AreEqual(elseBody, ifStmt.ElseBody);
    }

    [Test]
    public void BinaryExpression_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var left = new NumberLiteral(5);
        var operator_ = "+";
        var right = new NumberLiteral(3);

        // Act
        var binaryExpr = new BinaryExpression(left, operator_, right);

        // Assert
        Assert.AreEqual(left, binaryExpr.Left);
        Assert.AreEqual(operator_, binaryExpr.Operator);
        Assert.AreEqual(right, binaryExpr.Right);
    }

    [Test]
    public void CallExpression_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "testFunction";
        var arguments = new List<ASTNode> { new NumberLiteral(42), new StringLiteral("test") };

        // Act
        var callExpr = new CallExpression(name, arguments);

        // Assert
        Assert.AreEqual(name, callExpr.Name);
        Assert.AreEqual(arguments, callExpr.Arguments);
    }

    [Test]
    public void NumberLiteral_Creation_ShouldStoreValue()
    {
        // Arrange
        var value = 42;

        // Act
        var numberLiteral = new NumberLiteral(value);

        // Assert
        Assert.AreEqual(value, numberLiteral.Value);
    }

    [Test]
    public void StringLiteral_Creation_ShouldStoreValue()
    {
        // Arrange
        var value = "test string";

        // Act
        var stringLiteral = new StringLiteral(value);

        // Assert
        Assert.AreEqual(value, stringLiteral.Value);
    }

    [Test]
    public void BooleanLiteral_Creation_ShouldStoreValue()
    {
        // Arrange
        var value = true;

        // Act
        var booleanLiteral = new BooleanLiteral(value);

        // Assert
        Assert.AreEqual(value, booleanLiteral.Value);
    }

    [Test]
    public void ResultExpression_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var type = "Ok";
        var value = new NumberLiteral(42);

        // Act
        var resultExpr = new ResultExpression(type, value);

        // Assert
        Assert.AreEqual(type, resultExpr.Type);
        Assert.AreEqual(value, resultExpr.Value);
    }

    [Test]
    public void ErrorPropagation_Creation_ShouldStoreExpression()
    {
        // Arrange
        var expression = new CallExpression("riskyFunction", new List<ASTNode>());

        // Act
        var errorProp = new ErrorPropagation(expression);

        // Assert
        Assert.AreEqual(expression, errorProp.Expression);
    }

    [Test]
    public void MatchExpression_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var value = new Identifier("result");
        var cases = new List<MatchCase>
        {
            new MatchCase("Ok", "val", new List<ASTNode> { new Identifier("val") }),
            new MatchCase("Error", "err", new List<ASTNode> { new NumberLiteral(0) })
        };

        // Act
        var matchExpr = new MatchExpression(value, cases);

        // Assert
        Assert.AreEqual(value, matchExpr.Value);
        Assert.AreEqual(cases, matchExpr.Cases);
    }

    [Test]
    public void MatchCase_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var pattern = "Ok";
        var variable = "value";
        var body = new List<ASTNode> { new Identifier("value") };

        // Act
        var matchCase = new MatchCase(pattern, variable, body);

        // Assert
        Assert.AreEqual(pattern, matchCase.Pattern);
        Assert.AreEqual(variable, matchCase.Variable);
        Assert.AreEqual(body, matchCase.Body);
    }

    [Test]
    public void ModuleDeclaration_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "TestModule";
        var body = new List<ASTNode> { new FunctionDeclaration("test", new List<Parameter>(), "int", new List<ASTNode>()) };
        var exports = new List<string> { "test" };

        // Act
        var module = new ModuleDeclaration(name, body, exports);

        // Assert
        Assert.AreEqual(name, module.Name);
        Assert.AreEqual(body, module.Body);
        Assert.AreEqual(exports, module.Exports);
    }

    [Test]
    public void ImportStatement_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var moduleName = "TestModule";
        var specificImports = new List<string> { "function1", "function2" };
        var isWildcard = false;

        // Act
        var import = new ImportStatement(moduleName, specificImports, isWildcard);

        // Assert
        Assert.AreEqual(moduleName, import.ModuleName);
        Assert.AreEqual(specificImports, import.SpecificImports);
        Assert.AreEqual(isWildcard, import.IsWildcard);
    }

    [Test]
    public void ExportStatement_Creation_ShouldStoreExportedNames()
    {
        // Arrange
        var exportedNames = new List<string> { "function1", "function2" };

        // Act
        var export = new ExportStatement(exportedNames);

        // Assert
        Assert.AreEqual(exportedNames, export.ExportedNames);
    }

    [Test]
    public void SpecificationBlock_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var intent = "This function calculates the sum of two numbers";
        var rules = new List<string> { "Both inputs must be positive", "Result must be greater than either input" };
        var postconditions = new List<string> { "Returns sum of inputs", "Result is integer" };
        var sourceDoc = "API Documentation link";

        // Act
        var spec = new SpecificationBlock(intent, rules, postconditions, sourceDoc);

        // Assert
        Assert.AreEqual(intent, spec.Intent);
        Assert.AreEqual(rules, spec.Rules);
        Assert.AreEqual(postconditions, spec.Postconditions);
        Assert.AreEqual(sourceDoc, spec.SourceDoc);
    }

    [Test]
    public void GuardStatement_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var condition = new BooleanLiteral(true);
        var elseBody = new List<ASTNode> { new ReturnStatement(new NumberLiteral(0)) };

        // Act
        var guard = new GuardStatement(condition, elseBody);

        // Assert
        Assert.AreEqual(condition, guard.Condition);
        Assert.AreEqual(elseBody, guard.ElseBody);
    }

    [Test]
    public void TernaryExpression_Creation_ShouldStoreAllProperties()
    {
        // Arrange
        var condition = new BooleanLiteral(true);
        var thenExpr = new NumberLiteral(1);
        var elseExpr = new NumberLiteral(0);

        // Act
        var ternary = new TernaryExpression(condition, thenExpr, elseExpr);

        // Assert
        Assert.AreEqual(condition, ternary.Condition);
        Assert.AreEqual(thenExpr, ternary.ThenExpr);
        Assert.AreEqual(elseExpr, ternary.ElseExpr);
    }

    [Test]
    public void AllASTNodes_ShouldInheritFromASTNode()
    {
        // Test that all AST node types properly inherit from ASTNode
        Assert.IsInstanceOf<ASTNode>(new ProgramNode(new List<ASTNode>()));
        Assert.IsInstanceOf<ASTNode>(new FunctionDeclaration("test", new List<Parameter>(), "int", new List<ASTNode>()));
        Assert.IsInstanceOf<ASTNode>(new ReturnStatement(null));
        Assert.IsInstanceOf<ASTNode>(new IfStatement(new BooleanLiteral(true), new List<ASTNode>()));
        Assert.IsInstanceOf<ASTNode>(new LetStatement("x", "int", new NumberLiteral(42)));
        Assert.IsInstanceOf<ASTNode>(new BinaryExpression(new NumberLiteral(1), "+", new NumberLiteral(2)));
        Assert.IsInstanceOf<ASTNode>(new CallExpression("test", new List<ASTNode>()));
        Assert.IsInstanceOf<ASTNode>(new NumberLiteral(42));
        Assert.IsInstanceOf<ASTNode>(new StringLiteral("test"));
        Assert.IsInstanceOf<ASTNode>(new BooleanLiteral(true));
        Assert.IsInstanceOf<ASTNode>(new ResultExpression("Ok", new NumberLiteral(42)));
        Assert.IsInstanceOf<ASTNode>(new MatchExpression(new Identifier("x"), new List<MatchCase>()));
        Assert.IsInstanceOf<ASTNode>(new ModuleDeclaration("test", new List<ASTNode>()));
        Assert.IsInstanceOf<ASTNode>(new ImportStatement("test", null, false));
        Assert.IsInstanceOf<ASTNode>(new ExportStatement(new List<string>()));
    }
}