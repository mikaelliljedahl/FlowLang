using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Core;

namespace Cadenza.Tests.Unit;

[TestFixture]
public class MatchExpressionTests
{
    [Test]
    public void TestMatchExpressionParsing()
    {
        // Test that match expressions are parsed correctly
        var source = @"
function test_match() -> int {
    let result = Ok(42);
    return match result {
        Ok(value) -> value
        Error(err) -> 0
    };
}";
        
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var program = parser.Parse();
        
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.AreEqual("test_match", func.Name);
        Assert.AreEqual("int", func.ReturnType);
        Assert.AreEqual(2, func.Body.Count); // let and return statements
        
        // Check that the return statement contains a match expression
        var returnStmt = func.Body[1] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsNotNull(returnStmt.Expression);
        Assert.IsInstanceOf<MatchExpression>(returnStmt.Expression);
        
        var matchExpr = (MatchExpression)returnStmt.Expression;
        Assert.AreEqual(2, matchExpr.Cases.Count);
        
        // Check the Ok case
        var okCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "Ok");
        Assert.IsNotNull(okCase);
        Assert.AreEqual("value", okCase.Variable);
        Assert.AreEqual(1, okCase.Body.Count);
        
        // Check the Error case
        var errorCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "Error");
        Assert.IsNotNull(errorCase);
        Assert.AreEqual("err", errorCase.Variable);
        Assert.AreEqual(1, errorCase.Body.Count);
    }

    [Test]
    public void TestMatchExpressionCodeGeneration()
    {
        // Test that match expressions generate correct C# code
        var source = @"
function test_match() -> int {
    let result = Ok(42);
    return match result {
        Ok(value) -> value
        Error(err) -> 0
    };
}";
        
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var program = parser.Parse();
        
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(program);
        var csharpCode = syntaxTree.ToString();
        
        Assert.IsNotNull(csharpCode);
        Assert.IsTrue(csharpCode.Contains("int test_match()"));
        
        // Should contain some form of conditional logic for the match
        Assert.IsTrue(csharpCode.Contains("IsSuccess") || csharpCode.Contains("?"));
    }

    [Test]
    public void TestMatchWithSimplePatterns()
    {
        // Test match with simple patterns (not just Result types)
        var source = @"
function test_simple_match(value: int) -> string {
    return match value {
        1 -> ""one""
        2 -> ""two""
        _ -> ""other""
    };
}";
        
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var program = parser.Parse();
        
        Assert.IsNotNull(program);
        Assert.AreEqual(1, program.Statements.Count);
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.AreEqual("test_simple_match", func.Name);
        
        var returnStmt = func.Body[0] as ReturnStatement;
        Assert.IsNotNull(returnStmt);
        Assert.IsInstanceOf<MatchExpression>(returnStmt.Expression);
        
        var matchExpr = (MatchExpression)returnStmt.Expression;
        Assert.AreEqual(3, matchExpr.Cases.Count);
        
        // Check cases
        var case1 = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "1");
        Assert.IsNotNull(case1);
        
        var case2 = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "2");
        Assert.IsNotNull(case2);
        
        var wildcardCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "_");
        Assert.IsNotNull(wildcardCase);
    }
}