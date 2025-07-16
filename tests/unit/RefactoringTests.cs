using NUnit.Framework;
using System.Collections.Generic;
using Cadenza.Core;

namespace Cadenza.Tests.Unit;

[TestFixture]
public class RefactoringTests
{
    [Test]
    public void TestTokenCreation()
    {
        // Test that Token record works correctly
        var token = new Token(TokenType.Identifier, "test", null, 1, 1);
        Assert.AreEqual(TokenType.Identifier, token.Type);
        Assert.AreEqual("test", token.Lexeme);
        Assert.AreEqual(1, token.Line);
        Assert.AreEqual(1, token.Column);
    }

    [Test]
    public void TestLexerBasicFunctionality()
    {
        // Test that CadenzaLexer works correctly
        var source = "function test() -> int { return 42; }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        Assert.Greater(tokens.Count, 0);
        Assert.AreEqual(TokenType.Function, tokens[0].Type);
        Assert.AreEqual("function", tokens[0].Lexeme);
        Assert.AreEqual(TokenType.EOF, tokens[tokens.Count - 1].Type);
    }

    [Test]
    public void TestParserBasicFunctionality()
    {
        // Test that CadenzaParser works correctly
        var source = "function test() -> int { return 42; }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var program = parser.Parse();
        
        Assert.IsNotNull(program);
        Assert.Greater(program.Statements.Count, 0);
        Assert.IsInstanceOf<FunctionDeclaration>(program.Statements[0]);
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.AreEqual("test", func.Name);
        Assert.AreEqual("int", func.ReturnType);
    }

    [Test]
    public void TestMatchExpressionParsing()
    {
        // Test that match expressions are parsed correctly
        var source = @"
function test() -> int {
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
        Assert.Greater(program.Statements.Count, 0);
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.AreEqual("test", func.Name);
        
        // Should have parsed successfully without errors
        Assert.AreEqual(2, func.Body.Count); // let and return statements
    }

    [Test]
    public void TestCSharpGeneratorBasicFunctionality()
    {
        // Test that CSharpGenerator works correctly
        var source = "function test() -> int { return 42; }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var program = parser.Parse();
        
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(program);
        
        Assert.IsNotNull(syntaxTree);
        var csharpCode = syntaxTree.ToString();
        
        // Should contain the generated function
        Assert.IsTrue(csharpCode.Contains("int test()"));
        Assert.IsTrue(csharpCode.Contains("return 42;"));
    }
}