using Cadenza.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Cadenza.Tests.Unit;

[TestFixture]
public class RefactoringTests
{
    [Test]
    public void Tokens_Token_ShouldCreateCorrectly()
    {
        // Test that Token record works correctly
        var token = new Token(TokenType.Identifier, "test", null, 1, 1);
        Assert.That(token.Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(token.Lexeme, Is.EqualTo("test"));
        Assert.That(token.Line, Is.EqualTo(1));
        Assert.That(token.Column, Is.EqualTo(1));
    }

    [Test]
    public void Lexer_BasicFunctionality_ShouldWork()
    {
        // Test that CadenzaLexer works correctly
        var source = "function test() -> int { return 42; }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        Assert.That(tokens.Count, Is.GreaterThan(0));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Function));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("function"));
        Assert.That(tokens[tokens.Count - 1].Type, Is.EqualTo(TokenType.EOF));
    }

    [Test]
    public void Parser_BasicFunctionality_ShouldWork()
    {
        // Test that CadenzaParser works correctly
        var source = "function test() -> int { return 42; }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var program = parser.Parse();
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements.Count, Is.GreaterThan(0));
        Assert.That(program.Statements[0], Is.InstanceOf<FunctionDeclaration>());
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.That(func.Name, Is.EqualTo("test"));
        Assert.That(func.ReturnType, Is.EqualTo("int"));
    }

    [Test]
    public void Parser_MatchExpression_ShouldParseCorrectly()
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
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements.Count, Is.GreaterThan(0));
        
        var func = (FunctionDeclaration)program.Statements[0];
        Assert.That(func.Name, Is.EqualTo("test"));
        
        // Should have parsed successfully without errors
        Assert.That(func.Body.Count, Is.EqualTo(2)); // let and return statements
    }

    [Test]
    public void CodeGenerator_BasicFunctionality_ShouldWork()
    {
        // Test that CSharpGenerator works correctly
        var source = "function test() -> int { return 42; }";
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var program = parser.Parse();
        
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(program);
        
        Assert.That(syntaxTree, Is.Not.Null);
        var csharpCode = syntaxTree.ToString();
        
        // Should contain the generated function
        Assert.That(csharpCode.Contains("int test()"), Is.True);
        Assert.That(csharpCode.Contains("return 42;"), Is.True);
    }
}