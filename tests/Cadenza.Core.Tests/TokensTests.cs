using NUnit.Framework;
using Cadenza.Core;

namespace Cadenza.Core.Tests;

[TestFixture]
public class TokensTests
{
    [Test]
    public void Token_Creation_ShouldSetAllProperties()
    {
        // Arrange
        var type = TokenType.Identifier;
        var lexeme = "testVariable";
        var literal = "literal_value";
        var line = 5;
        var column = 10;

        // Act
        var token = new Token(type, lexeme, literal, line, column);

        // Assert
        Assert.AreEqual(type, token.Type);
        Assert.AreEqual(lexeme, token.Lexeme);
        Assert.AreEqual(literal, token.Literal);
        Assert.AreEqual(line, token.Line);
        Assert.AreEqual(column, token.Column);
    }

    [Test]
    public void Token_WithNullLiteral_ShouldAcceptNull()
    {
        // Arrange & Act
        var token = new Token(TokenType.Identifier, "test", null, 1, 1);

        // Assert
        Assert.AreEqual(TokenType.Identifier, token.Type);
        Assert.AreEqual("test", token.Lexeme);
        Assert.IsNull(token.Literal);
        Assert.AreEqual(1, token.Line);
        Assert.AreEqual(1, token.Column);
    }

    [Test]
    public void TokenType_Enum_ShouldContainExpectedValues()
    {
        // Test that key token types exist
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Function));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Return));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.If));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Match));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Ok));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Error));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.EOF));
    }

    [Test]
    public void TokenType_ShouldHaveOperatorTokens()
    {
        // Test that operator tokens exist
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Plus));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Minus));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Equal));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.NotEqual));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.And));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Or));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Question));
    }

    [Test]
    public void TokenType_ShouldHaveEffectTokens()
    {
        // Test that effect tokens exist
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Database));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Network));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Logging));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.FileSystem));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Memory));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.IO));
    }

    [Test]
    public void TokenType_ShouldHaveModuleTokens()
    {
        // Test that module system tokens exist
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Module));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Import));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Export));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.From));
    }

    [Test]
    public void TokenType_ShouldHaveUITokens()
    {
        // Test that UI component tokens exist
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Component));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.State));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.Render));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TokenType), TokenType.EventHandler));
    }
}