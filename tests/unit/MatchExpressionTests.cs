using Cadenza.Core;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cadenza.Tests.Unit;

[TestFixture]
public class MatchExpressionTests
{
    [Test]
    public void Parser_MatchExpression_ShouldParseCorrectly()
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
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements.Count, Is.EqualTo(1));
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.That(func.Name, Is.EqualTo("test_match"));
        Assert.That(func.ReturnType, Is.EqualTo("int"));
        Assert.That(func.Body.Count, Is.EqualTo(2)); // let and return statements
        
        // Check that the return statement contains a match expression
        var returnStmt = func.Body[1] as ReturnStatement;
        Assert.That(returnStmt, Is.Not.Null);
        Assert.That(returnStmt.Expression, Is.Not.Null);
        Assert.That(returnStmt.Expression, Is.InstanceOf<MatchExpression>());
        
        var matchExpr = (MatchExpression)returnStmt.Expression;
        Assert.That(matchExpr.Cases.Count, Is.EqualTo(2));
        
        // Check the Ok case
        var okCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "Ok");
        Assert.That(okCase, Is.Not.Null);
        Assert.That(okCase.Variable, Is.EqualTo("value"));
        Assert.That(okCase.Body.Count, Is.EqualTo(1));
        
        // Check the Error case
        var errorCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "Error");
        Assert.That(errorCase, Is.Not.Null);
        Assert.That(errorCase.Variable, Is.EqualTo("err"));
        Assert.That(errorCase.Body.Count, Is.EqualTo(1));
    }

    [Test]
    public void CodeGenerator_MatchExpression_ShouldGenerateCorrectCode()
    {
        // Test that match expressions generate correct C# code
        var source = @"
function test_match() -> int {
    let result: Result<int, string> = Ok(42);
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
        var csharpCode = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
        
        Assert.That(csharpCode, Is.Not.Null);
        Assert.That(csharpCode.Contains("int test_match()"), Is.True);
        
        // Should contain some form of conditional logic for the match
        Assert.That(csharpCode.Contains("IsSuccess") || csharpCode.Contains("?"), Is.True);
    }

    [Test]
    public void Parser_MatchExpression_ShouldParseSimplePatterns()
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
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements.Count, Is.EqualTo(1));
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.That(func.Name, Is.EqualTo("test_simple_match"));
        
        var returnStmt = func.Body[0] as ReturnStatement;
        Assert.That(returnStmt, Is.Not.Null);
        Assert.That(returnStmt.Expression, Is.InstanceOf<MatchExpression>());
        
        var matchExpr = (MatchExpression)returnStmt.Expression;
        Assert.That(matchExpr.Cases.Count, Is.EqualTo(3));
        
        // Check cases
        var case1 = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "1");
        Assert.That(case1, Is.Not.Null);
        
        var case2 = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "2");
        Assert.That(case2, Is.Not.Null);
        
        var wildcardCase = matchExpr.Cases.FirstOrDefault(c => c.Pattern == "_");
        Assert.That(wildcardCase, Is.Not.Null);
    }
}